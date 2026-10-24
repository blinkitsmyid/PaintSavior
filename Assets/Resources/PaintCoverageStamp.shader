Shader "Hidden/Game/PaintCoverageStamp"
{
    Properties
    {
        _MainTex ("Previous Paint", 2D) = "black" {}
        _PaintBlobMask ("Paint Blob Mask", 2D) = "white" {}
        _StampData ("Stamp Data", Vector) = (0.5, 0.5, 0.2, 0)
        _StampColor ("Stamp Color", Color) = (1, 1, 1, 1)
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

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_PaintBlobMask);
            SAMPLER(sampler_PaintBlobMask);

            float4 _StampData;
            float4 _StampColor;

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

            float StampMask(
                float2 uv)
            {
                float2 delta =
                    uv - _StampData.xy;

                float sinRotation =
                    sin(_StampData.w);

                float cosRotation =
                    cos(_StampData.w);

                float2 rotated = float2(
                    delta.x * cosRotation - delta.y * sinRotation,
                    delta.x * sinRotation + delta.y * cosRotation
                );

                rotated /= max(_StampData.z, 0.0001);

                if (
                    rotated.x < -0.5 ||
                    rotated.x > 0.5 ||
                    rotated.y < -0.5 ||
                    rotated.y > 0.5
                )
                {
                    return 0;
                }

                float2 maskUV =
                    rotated + 0.5;

                return SAMPLE_TEXTURE2D(
                    _PaintBlobMask,
                    sampler_PaintBlobMask,
                    maskUV
                ).r;
            }

            half4 frag(
                Varyings input)
                : SV_Target
            {
                float4 previous =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        input.uv
                    );

                float mask =
                    StampMask(input.uv);

                float3 color =
                    lerp(
                        previous.rgb,
                        _StampColor.rgb,
                        mask
                    );

                float coverage =
                    max(previous.a, mask);

                return half4(
                    color,
                    coverage
                );
            }

            ENDHLSL
        }
    }
}
