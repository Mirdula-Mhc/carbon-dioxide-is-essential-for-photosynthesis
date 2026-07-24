Shader "Custom/EnergyHighlight_FullObject"
{
    Properties
    {
        _HighlightColor ("Highlight Color", Color) = (0.2, 0.9, 1.0, 1)
        _Opacity ("Opacity", Range(0,1)) = 0.55

        _Intensity ("Glow Intensity", Range(0,5)) = 2.0

        _PulseSpeed ("Pulse Speed", Range(0,5)) = 1.5
        _PulseStrength ("Pulse Strength", Range(0,2)) = 0.6

        _WaveSpeed ("Wave Speed", Range(0,5)) = 1.0
        _WaveScale ("Wave Scale", Range(0.5,10)) = 3.0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        ZWrite Off
        Blend One One        // Additive transparency
        Cull Back
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 viewDir : TEXCOORD2;
            };

            fixed4 _HighlightColor;
            float _Opacity;
            float _Intensity;
            float _PulseSpeed;
            float _PulseStrength;
            float _WaveSpeed;
            float _WaveScale;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = normalize(_WorldSpaceCameraPos - o.worldPos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Smooth pulse (whole object)
                float pulse = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;
                pulse = lerp(1.0, pulse, _PulseStrength);

                // World-space wave shimmer
                float wave =
                    sin((i.worldPos.y + i.worldPos.x) * _WaveScale +
                        _Time.y * _WaveSpeed);

                wave = wave * 0.5 + 0.5;

                // Gentle view-based softening (not rim-only)
                float facing = saturate(dot(normalize(i.worldNormal), i.viewDir));
                float viewBoost = lerp(1.0, 0.8, facing);

                float intensity = wave * pulse * viewBoost * _Intensity;

                fixed4 col;
                col.rgb = _HighlightColor.rgb * intensity;
                col.a = intensity * _Opacity;

                return col;
            }
            ENDCG
        }
    }
}
