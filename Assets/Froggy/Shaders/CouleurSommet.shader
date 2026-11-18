// Les modèles taillés et peints de Froggy (Tools/blender) : la couleur est dans les sommets, en sRGB, avec une lumière
// douce déjà peinte. Ce shader éclaire par-dessus, doucement (pas d'ombre dure sur les facettes), reçoit les ombres et le
// brouillard. L'ambiante est un dégradé ciel / sol réglé sur le matériau, en attendant la DA du jardin.
// _Flotter > 0 : ce qui flotte (nénuphars, fleurs) houle sur place et penche vers le vent, retenu par sa tige ; chaque
// instance a sa phase, tirée de sa position. Seul le dessin bouge : la collision reste où elle est.
Shader "Froggy/CouleurSommet"
{
    Properties
    {
        _Teinte ("Teinte", Color) = (1, 1, 1, 1)
        _Ciel ("Ambiante venue du ciel", Color) = (0.62, 0.70, 0.80, 1)
        _Sol ("Ambiante venue du sol", Color) = (0.40, 0.38, 0.32, 1)
        _Douceur ("Douceur de la lumière", Range(0, 1)) = 0.45
        _Flotter ("Flotter (0 : immobile)", Range(0, 2)) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Bruit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Teinte;
                half4 _Ciel;
                half4 _Sol;
                half _Douceur;
                half _Flotter;
            CBUFFER_END

            struct Entree { float4 positionOS : POSITION; float3 normalOS : NORMAL; half4 couleur : COLOR; };
            struct Sortie
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                half brouillard : TEXCOORD2;
                half4 couleur : COLOR;
            };

            float3 Flotter(float3 pw)
            {
                float3 pivot = TransformObjectToWorld(float3(0, 0, 0));
                float h = Hachage1(floor(pivot.xz * 7.13));           // phase propre à chaque instance
                float t = _Time.y;
                float2 d = pw.xz - pivot.xz;
                float a = sin(t * 0.23 + h * 40.0) * 0.05 * _Flotter;  // lacet : va-et-vient de ±3° autour de la tige
                float ca = cos(a), sa = sin(a);
                float2 dr = float2(d.x * ca - d.y * sa, d.x * sa + d.y * ca);
                float2 vent = FroggyDirectionVent() * (0.3 + _FroggyVent.z);
                float houle = (sin(t * 1.1 + h * 6.283) * 0.6 + sin(t * 1.9 + h * 17.0) * 0.4) * 0.012 * _Flotter;
                float pente = sin(t * 0.8 + h * 9.0 + dot(pivot.xz, vent) * 0.4) * 0.02 * _Flotter;
                return float3(pivot.x + dr.x, pw.y + houle + dot(dr, vent) * pente, pivot.z + dr.y);
            }

            Sortie vert(Entree e)
            {
                Sortie s;
                float3 pw = TransformObjectToWorld(e.positionOS.xyz);
                if (_Flotter > 0) pw = Flotter(pw);
                s.positionWS = pw;
                s.positionCS = TransformWorldToHClip(pw);
                s.normalWS = TransformObjectToWorldNormal(e.normalOS);
                s.brouillard = ComputeFogFactor(s.positionCS.z);
                s.couleur = e.couleur;
                return s;
            }

            half4 frag(Sortie s) : SV_Target
            {
                half3 n = normalize(s.normalWS);
                Light soleil = GetMainLight(TransformWorldToShadowCoord(s.positionWS));
                half l = saturate((dot(n, soleil.direction) + _Douceur) / (1 + _Douceur));
                half3 albedo = s.couleur.rgb * _Teinte.rgb;
                #if !defined(UNITY_COLORSPACE_GAMMA)
                    albedo = SRGBToLinear(albedo);   // les couleurs de sommets arrivent en sRGB, telles que peintes
                #endif
                half3 ambiante = lerp(_Sol.rgb, _Ciel.rgb, n.y * 0.5 + 0.5);
                half3 c = albedo * (soleil.color * l * soleil.shadowAttenuation + ambiante);
                return half4(MixFog(c, s.brouillard), 1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
}
