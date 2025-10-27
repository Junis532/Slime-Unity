Shader "Custom/WaterReflection"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _ReflectionTex ("Reflection Texture", 2D) = "white" {}
        _ReflectionStrength ("Reflection Strength", Range(0, 1)) = 0.5
        _ReflectionSpeed ("Reflection Speed", Range(0, 10)) = 1
        _ReflectionSize ("Reflection Size", Range(0, 1)) = 0.5
        _ReflectionOffset ("Reflection Offset", Range(-1, 1)) = 0
    }
    SubShader
    {
        Tags { 
            "RenderType"="Transparent" 
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            sampler2D _ReflectionTex;
            float4 _MainTex_ST;

            float _ReflectionStrength;
            float _ReflectionSpeed;
            float _ReflectionSize;
            float _ReflectionOffset;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float wave = sin(i.uv.x * 10 + _Time.y * _ReflectionSpeed) * _ReflectionSize;
                float2 reflectionUV = i.uv + float2(0, wave + _ReflectionOffset);
                fixed4 baseColor = tex2D(_MainTex, i.uv);
                fixed4 reflectionColor = tex2D(_ReflectionTex, reflectionUV);
                return lerp(baseColor, reflectionColor * 0.8, _ReflectionStrength);
            }
            ENDCG
        }
    }
}
