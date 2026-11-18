// Eau du jardin (recette « Ghibli / animation » de la Tech Art Bible, volume 1, et leçons de Lovecraft, skill tech-art) :
// - couleur par profondeur (Beer-Lambert) entre eau peu profonde et profonde, fond vu à travers (réfraction) ;
// - profondeur lissée sur cinq points d'écran, sinon l'écume suit les marches du terrain ;
// - écume de rive graphique, en bandes qui avancent ; motif de Voronoï déformé, d'épaisseur variable ;
// - rides et motif poussés par le vent (_FroggyVent), reflet du ciel en rasant (Fresnel), éclats du soleil (HDR) ;
// - la luminosité suit la couleur du soleil.
// Il faut les textures de profondeur et de couleur de la caméra (cochées dans PC_RPAsset).
Shader "Froggy/Eau"
{
    Properties
    {
        _Peu ("Eau peu profonde", Color) = (0.45, 0.80, 0.74, 1)
        _Profond ("Eau profonde", Color) = (0.08, 0.34, 0.42, 1)
        _Absorption ("Profondeur de la couleur (m)", Float) = 0.35
        _Clarte ("Transparence", Range(0, 1)) = 0.55
        _Reflet ("Reflet du ciel", Color) = (0.78, 0.89, 0.96, 1)
        _Fresnel ("Force du reflet", Range(0, 1)) = 0.45
        _Rive ("Largeur de l'écume (m)", Float) = 0.16
        _Ecume ("Écume", Color) = (0.94, 0.98, 1.0, 1)
        _Motif ("Motif de Voronoï", Range(0, 1)) = 0.4
        _MotifEchelle ("Échelle du motif", Float) = 1.3
        _Refraction ("Réfraction", Range(0, 0.08)) = 0.025
        _Rides ("Rides", Range(0, 1)) = 0.45
        _Eclat ("Éclats du soleil", Range(0, 6)) = 2.5
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "Bruit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Peu, _Profond, _Reflet, _Ecume;
                half _Absorption, _Clarte, _Fresnel, _Rive, _Motif, _MotifEchelle, _Refraction, _Rides, _Eclat;
            CBUFFER_END

            struct Entree { float4 positionOS : POSITION; };
            struct Sortie
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float4 ecran : TEXCOORD1;
                half brouillard : TEXCOORD2;
            };

            Sortie vert(Entree e)
            {
                Sortie s;
                VertexPositionInputs p = GetVertexPositionInputs(e.positionOS.xyz);
                s.positionCS = p.positionCS;
                s.positionWS = p.positionWS;
                s.ecran = ComputeScreenPos(p.positionCS);
                s.brouillard = ComputeFogFactor(p.positionCS.z);
                return s;
            }

            float ProfondeurDerriere(float2 uv) { return LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams); }

            half4 frag(Sortie s) : SV_Target
            {
                float2 uv = s.ecran.xy / s.ecran.w;
                float zEau = s.ecran.w;
                float3 v = GetWorldSpaceNormalizeViewDir(s.positionWS);

                // épaisseur d'eau, lissée sur cinq points d'écran, ramenée à la verticale
                float2 px = 2.0 / _ScreenParams.xy;
                float zFond = (ProfondeurDerriere(uv) + ProfondeurDerriere(uv + float2(px.x, 0)) + ProfondeurDerriere(uv - float2(px.x, 0))
                             + ProfondeurDerriere(uv + float2(0, px.y)) + ProfondeurDerriere(uv - float2(0, px.y))) / 5;
                float prof = max(0, zFond - zEau) * saturate(abs(v.y) + 0.15);

                // rides : deux trains de vagues dans le sens du vent, un en travers
                float t = FroggyHorloge();
                float2 dir = FroggyDirectionVent();
                float2 travers = float2(-dir.y, dir.x);
                float2 p = s.positionWS.xz;
                float force = _Rides * (0.35 + _FroggyVent.z);
                float a1 = dot(p, dir) * 2.1 - t * 1.6, a2 = dot(p, dir * 0.8 + travers * 0.6) * 3.7 - t * 2.3, a3 = dot(p, travers) * 1.3 + t * 0.7;
                float2 pente = (dir * cos(a1) * 0.5 + (dir * 0.8 + travers * 0.6) * cos(a2) * 0.3 + travers * cos(a3) * 0.2) * 0.12 * force;
                float3 n = normalize(float3(-pente.x, 1, -pente.y));

                // fond vu à travers l'eau (sans réfracter ce qui est devant la surface)
                float2 uvR = uv + n.xz * _Refraction * saturate(prof * 4);
                if (ProfondeurDerriere(uvR) < zEau) uvR = uv;
                float3 fond = SampleSceneColor(uvR);

                Light soleil = GetMainLight();
                float3 lumiere = 0.55 + 0.45 * soleil.color;
                float k = 1 - exp(-prof / max(_Absorption, 0.01));                // Beer-Lambert
                float3 teinte = lerp(_Peu.rgb, _Profond.rgb, k) * lumiere;
                float3 c = lerp(fond * lerp(1, _Peu.rgb, 0.5), teinte, saturate(lerp(1 - _Clarte, 1, k)));

                // motif de Voronoï, déformé, épaisseur variable, plus net en eau peu profonde
                float2 q = p * _MotifEchelle + dir * t * 0.12;
                q += 0.4 * float2(BruitValeur(q * 0.6 + t * 0.07), BruitValeur(q * 0.6 + 5.2 - t * 0.07));
                float2 f = Voronoi(q, t * 0.5);
                float largeur = 0.05 + 0.07 * BruitValeur(p * 0.45 + 3.1);
                float lignes = 1 - smoothstep(0, largeur, f.y - f.x);
                c += lignes * _Motif * (1 - k * 0.7) * soleil.color * 0.3;

                // reflet du ciel en rasant
                float fres = pow(1 - saturate(dot(n, v)), 4) * _Fresnel;
                float3 ciel = GlossyEnvironmentReflection(reflect(-v, n), 0.15, 1);
                c = lerp(c, lerp(_Reflet.rgb * lumiere, ciel, 0.5), saturate(fres));

                // éclats du soleil, quantifiés (bord net)
                float spec = pow(saturate(dot(n, normalize(soleil.direction + v))), 280);
                c += soleil.color * smoothstep(0.35, 0.55, spec) * _Eclat;

                // écume de rive : bandes qui avancent vers le bord
                float bord = 1 - smoothstep(0, _Rive, prof);
                float bandes = step(0.5, frac(prof / _Rive * 1.4 - t * 0.5 + BruitValeur(p * 2.3) * 0.6));
                float ecume = step(0.38, bord * (0.55 + 0.45 * bandes));
                c = lerp(c, _Ecume.rgb * lumiere, ecume * 0.9);

                half alpha = saturate(prof / 0.02);   // fondu doux contre la berge
                return half4(MixFog(c, s.brouillard), alpha);
            }
            ENDHLSL
        }
    }
}
