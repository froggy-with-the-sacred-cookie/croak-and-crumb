// Gouttes d'eau en particules (face à la caméra) : un disque doux, plus clair au centre, couleur de la particule.
Shader "Froggy/Goutte"
{
    Properties { }
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

            struct Entree { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 couleur : COLOR; };
            struct Sortie { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half brouillard : TEXCOORD1; half4 couleur : COLOR; };

            Sortie vert(Entree e)
            {
                Sortie s;
                s.positionCS = TransformObjectToHClip(e.positionOS.xyz);
                s.uv = e.uv;
                s.brouillard = ComputeFogFactor(s.positionCS.z);
                s.couleur = e.couleur;
                return s;
            }

            half4 frag(Sortie s) : SV_Target
            {
                float r = length(s.uv - 0.5) * 2;
                half disque = 1 - smoothstep(0.55, 1.0, r);
                half3 c = s.couleur.rgb;
                #if !defined(UNITY_COLORSPACE_GAMMA)
                    c = SRGBToLinear(c);
                #endif
                c = c * (0.6 + 0.4 * GetMainLight().color) + (1 - r) * 0.35;   // reflet clair au centre
                return half4(MixFog(c, s.brouillard), s.couleur.a * disque);
            }
            ENDHLSL
        }
    }
}
