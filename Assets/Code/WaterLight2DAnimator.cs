using UnityEngine;
#if UNITY_2021_2_OR_NEWER
using UnityEngine.Rendering.Universal;
#else
using UnityEngine.Experimental.Rendering.Universal;
#endif

/// Light2D Intensity만 물결처럼 변조 (모든 시드/위상 자동 난수)
[RequireComponent(typeof(Light2D))]
public class WaterLight2DAnimator : MonoBehaviour
{
    [Header("시간")]
    public bool useUnscaledTime = false;
    [Range(0.1f, 5f)] public float timeScale = 1f;

    [Header("강도 범위")]
    [Range(0f, 5f)] public float minIntensity = 0.2f;
    [Range(0f, 5f)] public float maxIntensity = 0.7f;

    [Header("메인 호흡(느린 파동)")]
    [Tooltip("Hz (0.5 = 2초 주기)")]
    public float mainHz = 0.5f;
    [Range(0f, 1f)] public float mainDepth = 0.8f;

    [Header("잔물결(빠른 파동)")]
    public float wobbleHz = 1.8f;
    [Range(0f, 1f)] public float wobbleDepth = 0.25f;

    [Header("가끔 번쩍(스파이크)")]
    public Vector2 spikeIntervalRange = new Vector2(1.8f, 3.6f);
    public float spikeDuration = 0.45f;
    [Range(0f, 1f)] public float spikeStrength = 0.6f;
    [Range(0f, 1f)] public float spikeEase = 0.35f;

    // 내부 상태(전부 난수)
    Light2D _light;
    float _phaseMain, _phaseWobble, _phaseSpike;
    float _nextSpikeTime, _spikeStartTime;
    bool _spikeActive;

    void Awake()
    {
        _light = GetComponent<Light2D>();
        RandomizeAll();
        ScheduleNextSpike(true);
    }

    void OnEnable()
    {
        RandomizeAll();
        ScheduleNextSpike(true);
    }

    void Update()
    {
        float now = useUnscaledTime ? Time.unscaledTime : Time.time;
        float t = now * timeScale;

        float mid = (minIntensity + maxIntensity) * 0.5f;
        float range = Mathf.Max(0.0001f, (maxIntensity - minIntensity) * 0.5f);

        float main = Mathf.Sin((t + _phaseMain) * Mathf.PI * 2f * Mathf.Max(0.01f, mainHz)) * range * mainDepth;
        float wobble = Mathf.Sin((t + _phaseWobble) * Mathf.PI * 2f * wobbleHz) * range * wobbleDepth;

        if (!_spikeActive && now >= _nextSpikeTime) { _spikeActive = true; _spikeStartTime = now; }

        float spike = 0f;
        if (_spikeActive)
        {
            float u = Mathf.InverseLerp(0f, spikeDuration, now - _spikeStartTime);
            if (u >= 1f) { _spikeActive = false; ScheduleNextSpike(false); }
            else
            {
                float e = EaseInOut(u, spikeEase);
                spike = Mathf.Sin(e * Mathf.PI) * range * spikeStrength; // 0~1~0
            }
        }

        _light.intensity = Mathf.Clamp(mid + main + wobble + spike, minIntensity, maxIntensity);
    }

    // ---- 유틸 ----
    void RandomizeAll()
    {
        _phaseMain = Random.value * 1000f;
        _phaseWobble = Random.value * 1000f;
        _phaseSpike = Random.value;
    }

    void ScheduleNextSpike(bool first)
    {
        float now = useUnscaledTime ? Time.unscaledTime : Time.time;
        float gap = Random.Range(spikeIntervalRange.x, spikeIntervalRange.y);
        if (first) gap *= Mathf.Lerp(0.6f, 1.4f, _phaseSpike);
        _nextSpikeTime = now + Mathf.Max(0.05f, gap);
    }

    // 0=선형, 1=부드럽게
    float EaseInOut(float x, float softness)
    {
        softness = Mathf.Clamp01(softness);
        if (softness <= 0.0001f) return x;
        float a = Mathf.SmoothStep(0f, 1f, x);
        return Mathf.Lerp(x, a, softness);
    }

#if UNITY_EDITOR
    [ContextMenu("Randomize Now")]
    void EditorRandomizeNow()
    {
        RandomizeAll();
        ScheduleNextSpike(true);
    }
#endif
}
