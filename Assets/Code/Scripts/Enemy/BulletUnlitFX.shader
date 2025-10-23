Shader "URP/2D/BulletUnlitFX"
{
    Properties
    {
        // 스프라이트 텍스처/색
        [MainTexture] _MainTex("Sprite", 2D) = "white" {}
        [MainColor]   _BaseColor("Base Color (HDR)", Color) = (1,1,1,1)

        // 색상들
        _TipColor("Tip Flash Color (HDR)", Color)     = (1,0.8,0.3,1)
        _TrailColor("Trail Color (HDR)", Color)       = (1,0.2,0.2,1)
        _DissolveColor("Dissolve Edge Color (HDR)", Color) = (1,0.1,0.1,1)

        // 강도/길이 파라미터
        _Glow("Emission Multiplier", Range(0,5)) = 1.5
        _TrailLen("Trail Length", Range(0,1)) = 0.55
        _TrailSharp("Trail Sharpness", Range(0.1,12)) = 6
        _Scroll("Trail Scroll Speed", Range(-10,10)) = 3

        _HeadLen("Head Flash Length", Range(0.0,0.5)) = 0.17
        _FlashAmp("Head Flash Amp", Range(0,6)) = 3.0
        _FlashDecay("Head Flash Decay (1/s)", Range(0.1,10)) = 2.5

        _EdgePower("Edge Glow Power", Range(0.5,8)) = 2.5
        _EdgeAmp("Edge Glow Amp", Range(0,6)) = 1.2

        // 디졸브
        _Dissolve("Dissolve (0=off,1=gone)", Range(0,1)) = 0
        _DissolveEdge("Dissolve Edge Width", Range(0.001,0.3)) = 0.06

        // 시간/방향
        _Age("Age (seconds)", Float) = 0
        _TimeScale("Local Time Scale", Float) = 1
        _UseXAxis("Use X as Length (0=Y,1=X)", Float) = 0

        // (선택) 스프라이트 마스크 연동용
        // SpriteMask 사용 시 SpriteRenderer가 _StencilComp를 세팅해 줌
        [HideInInspector]_StencilComp("Stencil Comparison", Float) = 8
    }

    SubShader
    {
        Tags{
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "IgnoreProjector"="True"
            "CanUseSpriteAtlas"="True"
        }

        // Sprite Mask와 상호작용 (필요 없으면 블록 삭제해도 됨)
        Stencil
        {
            Ref 1
            Comp [_StencilComp]
            Pass Keep
            Fail Keep
            ZFail Keep
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "ForwardUnlit"
            Tags{ "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _BaseColor;
                float4 _TipColor;
                float4 _TrailColor;
                float4 _DissolveColor;

                float _Glow;
                float _TrailLen;
                float _TrailSharp;
                float _Scroll;

                float _HeadLen;
                float _FlashAmp;
                float _FlashDecay;

                float _EdgePower;
                float _EdgeAmp;

                float _Dissolve;
                float _DissolveEdge;

                float _Age;
                float _TimeScale;
                float _UseXAxis;
            CBUFFER_END

            struct appdata
            {
                float3 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            struct v2f
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.positionHCS = TransformObjectToHClip(v.positionOS);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            // 가벼운 해시 노이즈 (디졸브용)
            float hash21(float2 p)
            {
                p = frac(p*float2(123.34, 345.45));
                p += dot(p, p+34.345);
                return frac(p.x*p.y);
            }

            float safeDiv(float a, float b)
            {
                return a / max(b, 1e-4);
            }

            float4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // 길이 방향 축 선택 (0=Y, 1=X)
                float u = lerp(uv.y, uv.x, step(0.5, _UseXAxis)); // _UseXAxis>=0.5면 X 사용
                float v = uv.x; // 에지 계산용으로 반대축도 보유

                // 스프라이트 샘플 & 기본 색
                float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                float4 baseCol = tex * _BaseColor * i.color;

                // 시간
                float t = _Age * _TimeScale;

                // ── Trail (뒤쪽 꼬리)
                float tailStart = 1.0 - _TrailLen;
                float trailCore = saturate( safeDiv(u - tailStart, _TrailLen) ); // 0..1
                trailCore = 1.0 - trailCore;                       // head(1) → tail(0)
                float trail = pow(saturate(trailCore), _TrailSharp);

                // 흐르는 줄무늬
                float band = 0.5 + 0.5 * cos( (u*12.0 + t*8.0*_Scroll) );
                float trailBand = lerp(1.0, band, 0.35);
                trail *= trailBand;

                // ── Head flash (발사 직후 앞부분 번쩍)
                float headMask = smoothstep(1.0 - _HeadLen, 1.0, u);
                float flash = _FlashAmp * headMask * exp(-_FlashDecay * t);

                // ── Edge glow (가짜 프레넬)
                float edge = abs(v - 0.5) * 2.0;            // 0(center)~1(edge)
                edge = pow(saturate(edge), _EdgePower);
                float edgeGlow = edge * _EdgeAmp;

                // ── Dissolve (붉은 테두리 남기고 사라짐)
                float2 cell = floor(uv * float2(32, 8));
                float n = hash21(cell);

                float dissMask = saturate( safeDiv( (n - _Dissolve), _DissolveEdge ) ); // 1=살아있음
                float edgeA = saturate( (n - _Dissolve + _DissolveEdge*0.5) / max(_DissolveEdge,1e-3) )
                            - saturate( (n - _Dissolve - _DissolveEdge*0.5) / max(_DissolveEdge,1e-3) );
                edgeA = saturate(edgeA * 10.0);

                // ── 색 합성
                float3 col = baseCol.rgb;

                // 꼬리 색
                col = lerp(col, _TrailColor.rgb, trail * baseCol.a);
                // 헤드 플래시
                col += _TipColor.rgb * flash;
                // 에지 글로우
                col += _BaseColor.rgb * edgeGlow;
                // 디졸브 엣지 컬러
                col = lerp(col, _DissolveColor.rgb, edgeA);

                // 알파
                float alpha = baseCol.a * dissMask;
                alpha = saturate(alpha + flash * 0.1);

                // 에미션 강화
                col *= (1.0 + _Glow);

                return float4(col, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
