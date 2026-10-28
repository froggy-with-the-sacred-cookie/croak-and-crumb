// Les modèles taillés et peints de Froggy (Tools/blender) : la couleur est dans les sommets, en sRGB, avec une lumière
// douce déjà peinte. Ce shader éclaire par-dessus, doucement (pas d'ombre dure sur les facettes), reçoit les ombres et le
// brouillard. L'ambiante est un dégradé ciel / sol réglé sur le matériau, en attendant la DA du jardin.
Shader "Froggy/CouleurSommet"
{
    Properties
    {
        _Teinte ("Teinte", Color) = (1, 1, 1, 1)
        _Ciel ("Ambiante venue du ciel", Color) = (0.62, 0.70, 0.80, 1)
        _Sol ("Ambiante venue du sol", Color) = (0.40, 0.38, 0.32, 1)
        _Douceur ("Douceur de la lumière", Range(0, 1)) = 0.45
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

            CBUFFER_START(UnityPerMaterial)
                half4 _Teinte;
                half4 _Ciel;
                half4 _Sol;
                half _Douceur;
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

            Sortie vert(Entree e)
            {
                Sortie s;
                VertexPositionInputs p = GetVertexPositionInputs(e.positionOS.xyz);
                s.positionCS = p.positionCS;
                s.positionWS = p.positionWS;
                s.normalWS = TransformObjectToWorldNormal(e.normalOS);
                s.brouillard = ComputeFogFactor(p.positionCS.z);
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
