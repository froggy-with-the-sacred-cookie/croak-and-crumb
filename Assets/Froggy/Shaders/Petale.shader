// Pétale de cerisier en particule (rendu en maillage) : couleur de la particule, éclairé des deux côtés comme une
// feuille fine (la lumière passe au travers), fondu en fin de vie par l'alpha.
Shader "Froggy/Petale"
{
    Properties
    {
        _Transmission ("Lumière qui traverse", Range(0, 1)) = 0.6
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
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half _Transmission;
            CBUFFER_END

            struct Entree { float4 positionOS : POSITION; float3 normalOS : NORMAL; half4 couleur : COLOR; };
            struct Sortie { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; half brouillard : TEXCOORD1; half4 couleur : COLOR; };

            Sortie vert(Entree e)
            {
                Sortie s;
                VertexPositionInputs p = GetVertexPositionInputs(e.positionOS.xyz);
                s.positionCS = p.positionCS;
                s.normalWS = TransformObjectToWorldNormal(e.normalOS);
                s.brouillard = ComputeFogFactor(p.positionCS.z);
                s.couleur = e.couleur;
                return s;
            }

            half4 frag(Sortie s) : SV_Target
            {
                Light soleil = GetMainLight();
                half ndl = dot(normalize(s.normalWS), soleil.direction);
                half l = saturate(abs(ndl)) * (ndl > 0 ? 1 : _Transmission) * 0.6 + 0.55;
                half3 albedo = s.couleur.rgb;
                #if !defined(UNITY_COLORSPACE_GAMMA)
                    albedo = SRGBToLinear(albedo);
                #endif
                return half4(MixFog(albedo * soleil.color * l, s.brouillard), s.couleur.a);
            }
            ENDHLSL
        }
    }
}
