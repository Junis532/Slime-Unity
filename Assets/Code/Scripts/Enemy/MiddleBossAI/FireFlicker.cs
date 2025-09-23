using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Light2D))]
public class FireFlicker : MonoBehaviour
{
    private Light2D light2D;

    [Header("Intensity")]
    [Tooltip("평균 밝기(기본값).")]
    public float baseIntensity = 1.2f;
    [Tooltip("노이즈에 의해 흔들리는 폭(±).")]
    public float intensityAmplitude = 0.8f;
    [Tooltip("노이즈 속도(커질수록 빠르게 흔들림).")]
    public float noiseSpeed = 2.0f;
    [Tooltip("가끔 튀는 스파크 강도(0이면 없음).")]
    public float sparkJitter = 0.4f;
    [Tooltip("스파크 발생 확률(초당). 예: 2 = 평균 1초에 2회")]
    public float sparksPerSecond = 1.5f;

    [Header("Smoothing")]
    [Tooltip("값 변화 부드러움(높을수록 잔진동 적음).")]
    public float smoothLerp = 8f;

    [Header("Optional: Color wobble")]
    public bool colorWobble = false;
    [Tooltip("기본 색상(알파는 1 권장).")]
    public Color baseColor = new Color(1f, 0.75f, 0.35f, 1f);
    [Tooltip("색상 온도 변화 폭(HSV V/S 흔들림).")]
    public float colorVariance = 0.08f;

    [Header("Optional: Radius wobble (Spot/Point)")]
    public bool radiusWobble = false;
    public float baseOuterRadius = 3f;
    public float radiusAmplitude = 0.25f;

    float tOffset;
    float currentIntensity;

    void Awake()
    {
        light2D = GetComponent<Light2D>();
        tOffset = Random.value * 10f;          // 개체마다 다른 패턴
        currentIntensity = light2D.intensity;  // 시작값 동기화
        if (radiusWobble) light2D.pointLightOuterRadius = baseOuterRadius;
        if (colorWobble) light2D.color = baseColor;
    }

    void Update()
    {
        float t = Time.time * noiseSpeed + tOffset;

        // 0~1 Perlin → -1~1 로 변환
        float noise = Mathf.PerlinNoise(t, t * 0.73f) * 2f - 1f;

        // 기본 노이즈 기반 목표 밝기
        float target = baseIntensity + noise * intensityAmplitude;

        // 가끔 번쩍(스파크)
        if (sparksPerSecond > 0f && Random.value < sparksPerSecond * Time.deltaTime)
        {
            target += sparkJitter;
        }

        // 음수 방지
        target = Mathf.Max(0f, target);

        // 부드럽게 보간
        currentIntensity = Mathf.Lerp(currentIntensity, target, Time.deltaTime * smoothLerp);
        light2D.intensity = currentIntensity;

        // 선택: 반경 살짝 흔들림
        if (radiusWobble)
        {
            float rNoise = Mathf.PerlinNoise(t * 0.8f, 7.1f) * 2f - 1f;
            light2D.pointLightOuterRadius = baseOuterRadius + rNoise * radiusAmplitude;
        }

        // 선택: 색상 살짝 흔들림(밝기/채도 미세 변화)
        if (colorWobble)
        {
            Color.RGBToHSV(baseColor, out float h, out float s, out float v);
            float wob = (Mathf.PerlinNoise(t * 0.6f, 3.3f) * 2f - 1f) * colorVariance;
            float wob2 = (Mathf.PerlinNoise(4.7f, t * 0.9f) * 2f - 1f) * (colorVariance * 0.6f);
            s = Mathf.Clamp01(s + wob2);
            v = Mathf.Clamp01(v + wob);
            light2D.color = Color.HSVToRGB(h, s, v);
        }
    }
}
