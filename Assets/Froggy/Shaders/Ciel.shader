// Ciel du jardin : dégradé horizon / zénith, soleil (disque HDR pour le bloom, halo chaud) placé sur la lumière
// principale, nuages peints à bords nets, éclairés du côté du soleil et poussés par le vent (_FroggyVent).
Shader "Froggy/Ciel"
{
    Properties
    {
        _Zenith ("Zénith", Color) = (0.36, 0.60, 0.88, 1)
        _Horizon ("Horizon", Color) = (0.80, 0.89, 0.95, 1)
        _SousHorizon ("Sous l'horizon", Color) = (0.62, 0.70, 0.70, 1)
        [HDR] _SoleilCouleur ("Soleil", Color) = (1.0, 0.88, 0.70, 1)
        _SoleilTaille ("Taille du soleil (rad)", Range(0.005, 0.2)) = 0.045
        _Halo ("Halo", Range(0, 2)) = 0.8
        _Couverture ("Couverture nuageuse", Range(0, 1)) = 0.42
        _NuagesClairs ("Nuages au soleil", Color) = (1, 0.98, 0.95, 1)
        _NuagesOmbres ("Nuages à l'ombre", Color) = (0.70, 0.76, 0.88, 1)
        _NuagesEchelle ("Échelle des nuages", Float) = 1.1
        _NuagesVitesse ("Vitesse des nuages", Float) = 0.02
    }
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Bruit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Zenith, _Horizon, _SousHorizon, _SoleilCouleur, _NuagesClairs, _NuagesOmbres;
                half _SoleilTaille, _Halo, _Couverture, _NuagesEchelle, _NuagesVitesse;
            CBUFFER_END

            struct Entree { float4 positionOS : POSITION; };
            struct Sortie { float4 positionCS : SV_POSITION; float3 direction : TEXCOORD0; };

            Sortie vert(Entree e)
            {
                Sortie s;
                s.positionCS = TransformObjectToHClip(e.positionOS.xyz);
                s.direction = e.positionOS.xyz;
                return s;
            }

            half4 frag(Sortie s) : SV_Target
            {
                float3 d = normalize(s.direction);
                float3 soleil = normalize(_MainLightPosition.xyz);
                float h = d.y;
                float3 c = h >= 0 ? lerp(_Horizon.rgb, _Zenith.rgb, pow(saturate(h), 0.5))
                                  : lerp(_Horizon.rgb, _SousHorizon.rgb, saturate(-h * 4));
                float cs = dot(d, soleil);
                c += _SoleilCouleur.rgb * (pow(saturate(cs), 10) * 0.22 + pow(saturate(cs), 160) * 0.5) * _Halo;
                // le bas du ciel se réchauffe du côté du soleil
                c = lerp(c, c * float3(1.08, 1.0, 0.9), saturate(1 - abs(h) * 5) * saturate(cs * 0.5 + 0.5));

                float disque = smoothstep(cos(_SoleilTaille * 1.12), cos(_SoleilTaille), cs);
                if (h > 0)
                {
                    float2 deriv = FroggyDirectionVent() * FroggyHorloge() * _NuagesVitesse;
                    float2 uv = d.xz / (h + 0.12) * _NuagesEchelle * 0.35 + deriv;
                    float n = Fbm(uv) * 0.75 + Fbm(uv * 2.3 + 4.1) * 0.25;
                    float seuil = 1 - _Couverture;
                    float couvert = smoothstep(seuil * 0.62, seuil * 0.62 + 0.012, n);  // bord net, peint
                    // éclairage du nuage : plus épais vers le soleil = plus sombre de ce côté-ci
                    float nVersSoleil = Fbm(uv + normalize(soleil.xz + 1e-4) * 0.12) * 0.75 + Fbm((uv + normalize(soleil.xz + 1e-4) * 0.12) * 2.3 + 4.1) * 0.25;
                    float eclaire = saturate(0.55 + (n - nVersSoleil) * 6);
                    eclaire = eclaire > 0.5 ? 1 : 0.35 + eclaire * 0.6;                // deux tons
                    float3 nuage = lerp(_NuagesOmbres.rgb, _NuagesClairs.rgb, eclaire);
                    nuage += _SoleilCouleur.rgb * pow(saturate(cs), 6) * 0.35;       // liseré du côté du soleil
                    float fondu = smoothstep(0.0, 0.22, h);
                    c = lerp(c, nuage, couvert * fondu);
                    disque *= 1 - couvert * fondu * 0.7;   // le soleil perce un peu les nuages
                }
                c += _SoleilCouleur.rgb * disque * 12;    // au-delà de 1 : le bloom le fait rayonner
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
