Shader "Custom/Water_Gravity_Unity6"
{
    Properties
    {
        _ShallowColor ("Shallow Tint", Color) = (0.7, 0.9, 1.0, 1)
        _DeepColor ("Deep Tint", Color) = (0.1, 0.4, 0.8, 1)
        _SurfaceRimColor ("Top Edge Color", Color) = (1, 1, 1, 1)

        _Transparency ("Transparency", Range(0.1, 1)) = 0.75

        [Header(Gravity Level)]
        _FillHeight ("Water Level (Offset from Object Y)", Float) = 0.05

        _FresnelPower ("Edge/Rim Power", Range(0.5, 10)) = 3.0
        _DepthStrength ("Depth Darkening", Range(0,10)) = 1.5

        _WaveStrength ("Tiny Ripples", Range(0,0.02)) = 0.002
        _WaveSpeed ("Ripple Speed", Range(0,5)) = 0.6
        _WaveScale ("Ripple Scale", Range(0,10)) = 3
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent+10"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };

            struct v2f
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float3 viewDirWS    : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor;
                half4 _DeepColor;
                half4 _SurfaceRimColor;
                float _FillHeight;
                float _FresnelPower;
                float _DepthStrength;
                float _WaveStrength;
                float _WaveSpeed;
                float _WaveScale;
                half _Transparency;
            CBUFFER_END

            v2f vert (appdata input)
            {
                v2f output;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;

                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                output.normalWS = normalInputs.normalWS;

                output.viewDirWS = GetWorldSpaceViewDir(positionInputs.positionWS);

                return output;
            }

            half4 frag (v2f input, FRONT_FACE_TYPE facing : SV_IsFrontFace) : SV_Target
            {
                // World space gravity vector
                float3 gravityUp = float3(0, 1, 0);

                // Animated ripple in world space
                float ripple = sin((input.positionWS.x + input.positionWS.z) * _WaveScale + _Time.y * _WaveSpeed) * _WaveStrength;

                // --- GRAVITY WORLD-SPACE CLIP MATH ---
                // Get the container's world position origin (Pivot)
                float3 objectWorldPos = GetAbsolutePositionWS(UNITY_MATRIX_M[3].xyz);

                // Calculate vertical world height relative to object's pivot point along true Gravity (0,1,0)
                float heightRelativeToPivot = dot(input.positionWS - objectWorldPos, gravityUp);

                // Surface cut level aligned with gravity
                float surfaceLevel = _FillHeight + ripple;
                float distToSurface = surfaceLevel - heightRelativeToPivot;

                // Clip pixels above gravity level
                clip(distToSurface);

                // Water depth coloring along gravity
                float depth = saturate(distToSurface * _DepthStrength);
                half3 waterColor = lerp(_ShallowColor.rgb, _DeepColor.rgb, depth);

                // Normal inversion for inside container visibility
                float3 N = normalize(input.normalWS);
                N = facing ? N : -N;

                float3 V = normalize(input.viewDirWS);
                float NdotV = saturate(dot(N, V));

                // Rim light / fresnel outline
                half fresnel = pow(1.0 - NdotV, _FresnelPower);

                // Surface line rim
                float surfaceEdge = smoothstep(0.015, 0.00, distToSurface);
                half3 finalCol = lerp(waterColor + (fresnel * 0.3), _SurfaceRimColor.rgb, surfaceEdge);

                half alpha = max(_Transparency, fresnel * 0.5);
                alpha = max(alpha, surfaceEdge);

                return half4(finalCol, alpha);
            }

            ENDHLSL
        }
    }
}