Shader "PoseGame/VideoFX"
{
    // Stylizes the raw webcam feed shown in each player's panel: darkens/desaturates the
    // image and tints toward the player's accent color at the edges (a cheap vignette),
    // so the background reads as "produced" rather than a flat, unprocessed camera feed.
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1, 1, 1, 1)
        _VignetteColor ("Vignette Color", Color) = (0, 0, 0, 1)
        _VignetteIntensity ("Vignette Intensity", Range(0, 3)) = 1.2
        _Desaturate ("Desaturate", Range(0, 1)) = 0.25
        _Darken ("Darken", Range(0, 1)) = 0.12
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _VignetteColor;
            float _VignetteIntensity;
            float _Desaturate;
            float _Darken;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * i.color;

                float luma = dot(c.rgb, float3(0.299, 0.587, 0.114));
                c.rgb = lerp(c.rgb, luma.xxx, _Desaturate);

                float2 centered = i.uv - 0.5;
                float dist = length(centered) * 1.4142136; // 0 at center, ~1 at corners
                float vig = saturate(dist * _VignetteIntensity);
                c.rgb = lerp(c.rgb, _VignetteColor.rgb, vig * _VignetteColor.a);

                c.rgb *= (1 - _Darken);
                return c;
            }
            ENDCG
        }
    }
}
