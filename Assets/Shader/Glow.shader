Shader "Custom/GlowFullScreen"
{
    Properties
    {
        _MainTex("Base (RGB)", 2D) = "white" {}
        _BlurSize("Blur Size", Float) = 1
        _GlowIntensity("Glow Intensity", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            ZWrite Off
            Cull Off
            Blend One One // Additive

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            sampler2D _MainTex;
            float _BlurSize;
            float _GlowIntensity;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                half4 col = tex2D(_MainTex, uv) * 0.36;
                col += tex2D(_MainTex, uv + float2(_BlurSize, 0)) * 0.16;
                col += tex2D(_MainTex, uv - float2(_BlurSize, 0)) * 0.16;
                col += tex2D(_MainTex, uv + float2(0, _BlurSize)) * 0.16;
                col += tex2D(_MainTex, uv - float2(0, _BlurSize)) * 0.16;
                return col * _GlowIntensity;
            }
            ENDHLSL
        }
    }
}
