using UnityEngine;

[DisallowMultipleComponent]
public class TinyVerticalBob : MonoBehaviour
{
    public enum Mode { UI_RectTransform, World_Local }

    [Header("Target")]
    public Mode mode = Mode.UI_RectTransform;     // UI면 anchoredPosition, 아니면 localPosition
    public Transform target;                      // 비우면 자동

    [Header("Motion")]
    [Tooltip("상하 진폭 (UI=px, World=local units)")]
    public float amplitude = 4f;                  // 너무 과하면 어색, 기본 4
    [Tooltip("초당 왕복 횟수(Hz). 0.3~0.7 추천")]
    public float frequency = 0.5f;

    [Header("Time")]
    public bool useUnscaledTime = true;
    [Tooltip("여러 개가 같은 타이밍으로 흔들리는 게 싫다면 위상(도) 살짝 다르게")]
    [Range(0f, 360f)] public float phaseDeg = 0f;

    // cache
    RectTransform _rt;
    Vector3 _baseLocalPos;
    Vector2 _baseAnchoredPos;
    float _phaseRad;

    void Awake()
    {
        if (!target) target = transform;
        _rt = target as RectTransform;

        // 모드 자동 보정
        if (mode == Mode.UI_RectTransform && _rt == null) mode = Mode.World_Local;

        // 기준 위치 기록
        if (mode == Mode.UI_RectTransform) _baseAnchoredPos = _rt.anchoredPosition;
        else _baseLocalPos = target.localPosition;

        _phaseRad = phaseDeg * Mathf.Deg2Rad;
    }

    void OnEnable()
    {
        // 활성화 시 한 번 바로 반영
        Apply(0f);
    }

    void Update()
    {
        float t = useUnscaledTime ? Time.unscaledTime : Time.time;
        Apply(t);
    }

    void Apply(float t)
    {
        // y = A * sin(2π f t + φ)
        float y = amplitude * Mathf.Sin((Mathf.PI * 2f * frequency * t) + _phaseRad);

        if (mode == Mode.UI_RectTransform)
        {
            // anchoredPosition만 y만 보정
            Vector2 p = _baseAnchoredPos;
            p.y += y;
            _rt.anchoredPosition = p;
        }
        else
        {
            Vector3 p = _baseLocalPos;
            p.y += y;
            target.localPosition = p;
        }
    }

    [ContextMenu("Recapture Base From Current")]
    void RecaptureBase()
    {
        if (mode == Mode.UI_RectTransform) _baseAnchoredPos = _rt.anchoredPosition;
        else _baseLocalPos = target.localPosition;
    }
}
