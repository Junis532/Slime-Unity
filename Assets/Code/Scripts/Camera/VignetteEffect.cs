using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using DG.Tweening;

[RequireComponent(typeof(Volume))]
public class VignetEffect : MonoBehaviour
{
    private Volume volume;
    private Vignette vignette;
    private ColorAdjustments colorAdjustments;
    private Tween flashTween; // 🔴 피격 효과 트윈

    [Header("Vignette Settings")]
    public float maxIntensity = 0.45f; // 최대 비네팅 강도
    public float maxSmoothness = 0.9f;

    [Header("Color Adjustments Settings")]
    public float maxContrast = 40f; // Contrast 최댓값

    [Header("차징 연동용 변수 (자동 업데이트)")]
    [Range(0f, 1f)] public float chargeAmount = 0f;

    private Color defaultColor = Color.black; // 원래 색상 저장용

    void Start()
    {
        volume = GetComponent<Volume>();
        if (volume.profile == null)
            volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();

        // 1️⃣ 비네트 설정
        if (!volume.profile.TryGet(out vignette))
            vignette = volume.profile.Add<Vignette>(true);

        vignette.color.value = defaultColor;
        vignette.center.value = new Vector2(0.5f, 0.5f);
        vignette.intensity.value = 0f;

        // 2️⃣ 색상 대비 설정
        if (!volume.profile.TryGet(out colorAdjustments))
            colorAdjustments = volume.profile.Add<ColorAdjustments>(true);

        volume.isGlobal = true;
        volume.priority = 10;
    }

    void Update()
    {
        if (vignette != null)
        {
            vignette.intensity.value = Mathf.Lerp(0f, maxIntensity, chargeAmount);
            vignette.smoothness.value = Mathf.Lerp(0.5f, maxSmoothness, chargeAmount);
        }

        if (colorAdjustments != null)
        {
            colorAdjustments.contrast.value = Mathf.Lerp(0f, maxContrast, chargeAmount);
        }
    }

    /// <summary>
    /// 🔴 피격 시 붉은 비네트 효과 (현재 어두운 정도 유지)
    /// </summary>
    public void PlayDamageFlash(float redBoost = 0.25f, float duration = 0.5f)
    {
        if (vignette == null) return;

        // 기존 트윈 중단
        flashTween?.Kill();

        // 현재 상태 저장
        Color originalColor = vignette.color.value;
        float originalIntensity = vignette.intensity.value; // 현재 어두운 정도 유지용

        vignette.color.value = Color.red;

        // 🔥 트윈 시퀀스: 현재 강도에서 살짝 더 진해졌다가 원래 강도로 복귀
        flashTween = DOTween.Sequence()
            .Append(DOTween.To(
                () => vignette.intensity.value,
                x => vignette.intensity.value = x,
                Mathf.Min(originalIntensity + redBoost, 1f), // 너무 밝아지지 않게 제한
                0.1f
            ))
            .Append(DOTween.To(
                () => vignette.intensity.value,
                x => vignette.intensity.value = x,
                originalIntensity,
                duration
            ))
            .OnComplete(() =>
            {
                vignette.color.value = originalColor; // 색상 복귀
            });
    }

}
