Shader "Game/Interaction/OutlineHighlight"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 1, 1, 1)
        _OutlineThickness ("Outline Thickness", Range(0, 0.2)) = 0.02
        _OutlineOpacity ("Outline Opacity", Range(0, 1)) = 1
        _OutlineGlow ("Outline Glow", Range(0, 10)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Name "Outline"
            Cull Front
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineThickness;
                float _OutlineOpacity;
                float _OutlineGlow;
            CBUFFER_END

            /// <summary>
            /// Расширяет вершины модели по нормали и подготавливает их к отрисовке оболочки.
            /// </summary>
            Varyings Vert(
                Attributes input)
            {
                Varyings output;

                float3 expandedPosition =
                    input.positionOS.xyz +
                    input.normalOS * _OutlineThickness;

                output.positionCS =
                    TransformObjectToHClip(expandedPosition);

                return output;
            }

            /// <summary>
            /// Возвращает цвет контура с настроенной прозрачностью и интенсивностью свечения.
            /// </summary>
            half4 Frag()
                : SV_Target
            {
                float glowMultiplier =
                    1.0 + max(0.0, _OutlineGlow);

                float alpha =
                    saturate(
                        _OutlineColor.a *
                        _OutlineOpacity
                    );

                return half4(
                    _OutlineColor.rgb * glowMultiplier,
                    alpha
                );
            }

            ENDHLSL
        }
    }
}
