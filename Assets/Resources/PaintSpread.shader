Shader "Hidden/Game/PaintSpread"
{
    Properties
    {
        _MainTex ("Source", 2D) = "black" {}
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            ZTest Always
            Cull Off
            ZWrite Off

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define MAX_SPREAD_BLOBS 64

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            int _SpreadBlobCount;
            float4 _SpreadBlobData[MAX_SPREAD_BLOBS];
            float4 _SpreadBlobColors[MAX_SPREAD_BLOBS];

            /// <summary>
            /// Преобразует вершину полноэкранного прохода в clip-space и передаёт UV-координаты.
            /// </summary>
            Varyings vert(
                Attributes input)
            {
                Varyings output;

                output.positionCS =
                    TransformObjectToHClip(
                        input.positionOS.xyz
                    );

                output.uv =
                    input.uv;

                return output;
            }

            /// <summary>
            /// Назначает каждому UV-пикселю цвет ближайшей сохранённой кляксы.
            /// </summary>
            half4 frag(
                Varyings input)
                : SV_Target
            {
                if (_SpreadBlobCount <= 0)
                {
                    return half4(0, 0, 0, 0);
                }

                float nearestDistance =
                    1000000.0;

                float3 nearestColor =
                    _SpreadBlobColors[0].rgb;

                for (int i = 0; i < MAX_SPREAD_BLOBS; i++)
                {
                    if (i >= _SpreadBlobCount)
                    {
                        break;
                    }

                    float distanceToBlob =
                        distance(
                            input.uv,
                            _SpreadBlobData[i].xy
                        );

                    if (distanceToBlob < nearestDistance)
                    {
                        nearestDistance = distanceToBlob;
                        nearestColor =
                            _SpreadBlobColors[i].rgb;
                    }
                }

                return half4(
                    nearestColor,
                    1
                );
            }

            ENDHLSL
        }
    }
}
