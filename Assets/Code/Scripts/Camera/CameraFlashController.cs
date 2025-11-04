using UnityEngine;
using DG.Tweening;

public class CameraFlashController : MonoBehaviour
{
    [Header("타겟 카메라")]
    public Camera targetCamera;

    [Header("기본 설정")]
    public float flashDuration = 0.5f;
    public float flashIntensity = 0.4f;
    public Color defaultFlashColor = Color.red;

    private static CameraFlashController _instance;
    public static CameraFlashController Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindAnyObjectByType<CameraFlashController>();
            return _instance;
        }
    }

    private Color originalColor;

    void Awake()
    {
        if (_instance == null)
            _instance = this;
        else if (_instance != this)
            Destroy(gameObject);

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera != null)
            originalColor = targetCamera.backgroundColor;
    }

    /// <summary>
    /// 카메라 백그라운드 플래시
    /// </summary>
    public void Flash(Color flashColor, float intensity = -1f, float duration = -1f)
    {
        if (targetCamera == null) return;

        if (intensity < 0) intensity = flashIntensity;
        if (duration < 0) duration = flashDuration;

        Color startColor = originalColor;
        Color peakColor = Color.Lerp(startColor, flashColor, intensity);

        // 기존 트윈 종료 후 새 트윈 실행
        targetCamera.DOKill();

        Sequence seq = DOTween.Sequence();
        seq.Append(DOVirtual.Color(startColor, peakColor, duration * 0.4f,
            c => targetCamera.backgroundColor = c));
        seq.Append(DOVirtual.Color(peakColor, originalColor, duration * 0.6f,
            c => targetCamera.backgroundColor = c));
    }

    public void FlashRed() => Flash(Color.red, flashIntensity, flashDuration);
}
