using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Volume))]
public class VignetEffect : MonoBehaviour
{
    private Volume volume;
    private Vignette vignette;
    private ColorAdjustments colorAdjustments;

    [Header("Vignette Settings")]
    public float maxIntensity = 0.45f; // 최대 비네팅 강도
    public float maxSmoothness = 0.9f;

    [Header("Color Adjustments Settings")]
    public float maxContrast = 40f; // Contrast 최댓값

    [Header("차징 연동용 변수 (자동 업데이트)")]
    [Range(0f, 1f)] public float chargeAmount = 0f;

    void Start()
    {
        volume = GetComponent<Volume>();
        if (volume.profile == null)
            volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();

        // 1. 비네팅 설정
        if (!volume.profile.TryGet(out vignette))
            vignette = volume.profile.Add<Vignette>(true);
        vignette.color.value = Color.black;
        vignette.center.value = new Vector2(0.5f, 0.5f);

        // 2. 색상 대비 설정
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
}
