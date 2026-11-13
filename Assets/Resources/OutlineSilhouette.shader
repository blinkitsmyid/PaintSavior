Shader "Hidden/Game/OutlineSilhouette"
{
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "OutlineSilhouette"
            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            /// <summary>
            /// Переводит вершину объекта в пространство камеры для записи силуэта.
            /// </summary>
            Varyings Vert(
                Attributes input)
            {
                Varyings output;

                output.positionCS =
                    TransformObjectToHClip(
                        input.positionOS.xyz
                    );

                return output;
            }

            /// <summary>
            /// Записывает непрозрачный пиксель в маску силуэта объекта.
            /// </summary>
            half4 Frag()
                : SV_Target
            {
                return half4(1, 1, 1, 1);
            }

            ENDHLSL
        }
    }
}
