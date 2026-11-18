// Eau provisoire de la vitrine du jardin : transparente, rides animées, reflet clair en rasant, éclat du soleil.
// À remplacer par la vraie eau (skill tech-art : couleur par profondeur, lignes de rive).
Shader "Froggy/EauSimple"
{
    Properties
    {
        _Couleur ("Couleur (alpha = opacité)", Color) = (0.30, 0.58, 0.62, 0.72)
        _Reflet ("Reflet en rasant", Color) = (0.80, 0.91, 0.96, 1)
        _Rides ("Force des rides", Range(0, 1)) = 0.35
        _Vitesse ("Vitesse des rides", Float) = 0.5
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

            CBUFFER_START(UnityPerMaterial)
                half4 _Couleur;
                half4 _Reflet;
                half _Rides;
                half _Vitesse;
            CBUFFER_END

            struct Entree { float4 positionOS : POSITION; };
            struct Sortie { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; half brouillard : TEXCOORD1; };

            Sortie vert(Entree e)
            {
                Sortie s;
                VertexPositionInputs p = GetVertexPositionInputs(e.positionOS.xyz);
                s.positionCS = p.positionCS;
                s.positionWS = p.positionWS;
                s.brouillard = ComputeFogFactor(p.positionCS.z);
                return s;
            }

            half4 frag(Sortie s) : SV_Target
            {
                float2 p = s.positionWS.xz;
                float t = _Time.y * _Vitesse;
                float3 n = normalize(float3(
                    _Rides * 0.12 * (sin(p.x * 1.9 + t) + 0.6 * sin((p.x + p.y) * 3.1 - 1.7 * t)),
                    1,
                    _Rides * 0.12 * (cos(p.y * 2.2 - 1.2 * t) + 0.6 * cos((p.x - p.y) * 2.7 + t))));
                float3 v = GetWorldSpaceNormalizeViewDir(s.positionWS);
                Light soleil = GetMainLight();
                half fresnel = pow(1 - saturate(dot(n, v)), 3);
                half eclat = pow(saturate(dot(n, normalize(soleil.direction + v))), 160) * 1.5;
                half3 c = lerp(_Couleur.rgb * (0.55 + 0.45 * soleil.color), _Reflet.rgb, fresnel * 0.75) + soleil.color * eclat;
                half a = saturate(lerp(_Couleur.a, 1, fresnel * 0.6) + eclat);
                return half4(MixFog(c, s.brouillard), a);
            }
            ENDHLSL
        }
    }
}
