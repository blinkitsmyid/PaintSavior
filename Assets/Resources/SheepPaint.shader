Shader "Game/Paint/SheepPaint"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (0.8, 0.8, 0.8, 1)
        _PaintBlobMask ("Paint Blob Mask", 2D) = "white" {}
        _PaintCoverageTexture ("Paint Coverage Texture", 2D) = "black" {}

        _LeftColor ("Left Color", Color) = (1, 1, 1, 1)
        _RightColor ("Right Color", Color) = (1, 1, 1, 1)
        _BackColor ("Back Color", Color) = (1, 1, 1, 1)
        _TailColor ("Tail Color", Color) = (1, 1, 1, 1)

        _LeftProgress ("Left Progress", Range(0,1)) = 0
        _RightProgress ("Right Progress", Range(0,1)) = 0
        _BackProgress ("Back Progress", Range(0,1)) = 0
        _TailProgress ("Tail Progress", Range(0,1)) = 0
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
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define MAX_BLOBS 8

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 normalOS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)

                float4 _BaseMap_ST;
                float4 _BaseColor;

                float4 _LeftColor;
                float4 _RightColor;
                float4 _BackColor;
                float4 _TailColor;

                float _LeftProgress;
                float _RightProgress;
                float _BackProgress;
                float _TailProgress;

            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            TEXTURE2D(_PaintBlobMask);
            SAMPLER(sampler_PaintBlobMask);

            TEXTURE2D(_PaintCoverageTexture);
            SAMPLER(sampler_PaintCoverageTexture);

            float4 _LeftBlobs[MAX_BLOBS];
            float4 _RightBlobs[MAX_BLOBS];
            float4 _BackBlobs[MAX_BLOBS];
            float4 _TailBlobs[MAX_BLOBS];

            float4 _LeftBlobColors[MAX_BLOBS];
            float4 _RightBlobColors[MAX_BLOBS];
            float4 _BackBlobColors[MAX_BLOBS];
            float4 _TailBlobColors[MAX_BLOBS];

            Varyings vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(input.positionOS.xyz);

                output.positionCS =
                    positionInputs.positionCS;

                output.positionOS =
                    input.positionOS.xyz;

                output.normalOS =
                    input.normalOS;

                output.uv =
                    input.uv;

                return output;
            }

            float BlobMask(
                float2 uv,
                float4 blob)
            {
                float2 position =
                    blob.xy;

                float size =
                    blob.z;

                if (size <= 0.0001)
                {
                    return 0;
                }

                float rotation =
                    blob.w;

                float2 delta =
                    uv - position;

                float sinRotation =
                    sin(rotation);

                float cosRotation =
                    cos(rotation);

                float2 rotated;

                rotated.x =
                    delta.x * cosRotation -
                    delta.y * sinRotation;

                rotated.y =
                    delta.x * sinRotation +
                    delta.y * cosRotation;

                rotated /= size;

                if (
                    rotated.x < -0.5 ||
                    rotated.x > 0.5 ||
                    rotated.y < -0.5 ||
                    rotated.y > 0.5
                )
                {
                    return 0;
                }

                float2 blobUV =
                    rotated + 0.5;

                float mask =
                    SAMPLE_TEXTURE2D(
                        _PaintBlobMask,
                        sampler_PaintBlobMask,
                        blobUV
                    ).r;

                return mask;
            }

            float3 ApplyBlobs(
                float2 uv,
                float4 blobs[MAX_BLOBS],
                float4 colors[MAX_BLOBS],
                float3 baseColor)
            {
                float3 result =
                    baseColor;

                for (int i = 0; i < MAX_BLOBS; i++)
                {
                    float mask =
                        BlobMask(
                            uv,
                            blobs[i]
                        );

                    result =
                        lerp(
                            result,
                            colors[i].rgb,
                            mask * blobs[i].a
                        );
                }

                return result;
            }

            half4 frag(Varyings input)
                : SV_Target
            {
                float2 baseUV =
                    input.uv * _BaseMap_ST.xy +
                    _BaseMap_ST.zw;

                float3 color =
                    SAMPLE_TEXTURE2D(
                        _BaseMap,
                        sampler_BaseMap,
                        baseUV
                    ).rgb * _BaseColor.rgb;

                float4 paintCoverage =
                    SAMPLE_TEXTURE2D(
                        _PaintCoverageTexture,
                        sampler_PaintCoverageTexture,
                        input.uv
                    );

                color =
                    lerp(
                        color,
                        paintCoverage.rgb,
                        saturate(paintCoverage.a)
                    );

                return half4(
                    color,
                    1
                );
            }

            ENDHLSL
        }
    }
}
