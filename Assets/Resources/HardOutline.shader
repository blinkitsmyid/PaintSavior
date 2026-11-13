Shader "Game/Interaction/HardOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 1, 1, 1)
        _OutlineThickness ("Outline Thickness", Range(1, 12)) = 2
        _OutlineOpacity ("Outline Opacity", Range(0, 1)) = 1
        _OutlineGlow ("Outline Glow", Range(0, 10)) = 1
        _ReferenceHeight ("Reference Resolution Height", Float) = 1080
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "HardOutline"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D_X(_OutlineRenderTexture);
            SAMPLER(sampler_OutlineRenderTexture);

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineThickness;
                float _OutlineOpacity;
                float _OutlineGlow;
                float _ReferenceHeight;
            CBUFFER_END

            /// <summary>
            /// Возвращает альфа-значение силуэта в заданной экранной координате.
            /// </summary>
            half SampleSilhouette(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X(
                    _OutlineRenderTexture,
                    sampler_OutlineRenderTexture,
                    uv
                ).a;
            }

            /// <summary>
            /// Анализирует восемь соседних пикселей и строит внешний контур силуэта.
            /// </summary>
            half4 Frag(
                Varyings input)
                : SV_Target
            {
                float2 texelSize =
                    1.0 / _ScreenParams.xy;

                float referenceHeight =
                    max(1.0, _ReferenceHeight);

                float resolutionScale =
                    _ScreenParams.y / referenceHeight;

                float2 offset =
                    texelSize *
                    (_OutlineThickness * resolutionScale);

                float2 uv = input.texcoord;
                half center = SampleSilhouette(uv);

                half up =
                    SampleSilhouette(
                        uv + float2(0, offset.y)
                    );
                half down =
                    SampleSilhouette(
                        uv - float2(0, offset.y)
                    );
                half left =
                    SampleSilhouette(
                        uv - float2(offset.x, 0)
                    );
                half right =
                    SampleSilhouette(
                        uv + float2(offset.x, 0)
                    );

                half topLeft =
                    SampleSilhouette(
                        uv + float2(-offset.x, offset.y)
                    );
                half topRight =
                    SampleSilhouette(
                        uv + float2(offset.x, offset.y)
                    );
                half bottomLeft =
                    SampleSilhouette(
                        uv + float2(-offset.x, -offset.y)
                    );
                half bottomRight =
                    SampleSilhouette(
                        uv + float2(offset.x, -offset.y)
                    );

                half cardinalMaximum =
                    max(
                        max(up, down),
                        max(left, right)
                    );

                half diagonalMaximum =
                    max(
                        max(topLeft, topRight),
                        max(bottomLeft, bottomRight)
                    );

                half dilated =
                    max(
                        center,
                        max(cardinalMaximum, diagonalMaximum)
                    );

                half edge =
                    saturate(dilated - center);

                float glowMultiplier =
                    1.0 + max(0.0, _OutlineGlow);

                float alpha =
                    edge *
                    saturate(_OutlineColor.a) *
                    saturate(_OutlineOpacity);

                return half4(
                    _OutlineColor.rgb * glowMultiplier,
                    alpha
                );
            }

            ENDHLSL
        }
    }
}
