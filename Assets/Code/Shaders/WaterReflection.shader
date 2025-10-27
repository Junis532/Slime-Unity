Shader "Custom/WaterReflection"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _ReflectionTex ("Reflection Texture", 2D) = "white" {}
        _ReflectionStrength ("Reflection Strength", Range(0, 1)) = 0.5
        _ReflectionSpeed ("Reflection Speed", Range(0, 10)) = 1
        _ReflectionSize ("Reflection Size", Range(0, 1)) = 0.5
        _ReflectionOffset ("Reflection Offset", Range(-1, 1)) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Overlay" }
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
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            uniform float _ReflectionStrength;
            uniform float _ReflectionSpeed;
            uniform float _ReflectionSize;
            uniform float _ReflectionOffset;

            sampler2D _MainTex;
            sampler2D _ReflectionTex;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                float wave = sin(i.worldPos.x * 0.1 + _Time.y * _ReflectionSpeed) * _ReflectionSize;
                float2 reflectionUV = i.uv + float2(0, wave + _ReflectionOffset);
                half4 baseColor = tex2D(_MainTex, i.uv);
                half4 reflectionColor = tex2D(_ReflectionTex, reflectionUV);
                return lerp(baseColor, reflectionColor, _ReflectionStrength);
            }
            ENDCG
        }
    }
}
