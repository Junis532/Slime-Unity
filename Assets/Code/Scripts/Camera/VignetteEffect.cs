using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Volume))]
public class VignetEffect : MonoBehaviour
{
    private Volume volume;
    private Vignette vignette;
    private ColorAdjustments colorAdjustments; // ColorAdjustments 변수 추가

    [Header("Vignette Settings")]
    [Range(0f, 1f)]
    public float intensity = 0.4f;

    [Range(0f, 1f)]
    public float smoothness = 0.8f;

    [Header("Color Adjustments Settings")]
    [Range(-100f, 100f)] // Contrast 값 범위
    public float contrast = 0f; // Contrast 값

    void Start()
    {
        volume = GetComponent<Volume>();
        if (volume.profile == null)
        {
            volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
        }

        // 1. Vignette 효과 추가 및 초기 설정
        if (!volume.profile.TryGet(out vignette))
        {
            vignette = volume.profile.Add<Vignette>(true);
        }
        vignette.intensity.value = intensity;
        vignette.smoothness.value = smoothness;
        vignette.color.value = Color.black;
        vignette.center.value = new Vector2(0.5f, 0.5f);

        // 2. Color Adjustments 효과 추가 및 초기 설정
        if (!volume.profile.TryGet(out colorAdjustments))
        {
            colorAdjustments = volume.profile.Add<ColorAdjustments>(true);
        }

        // Contrast 활성화 및 초기값 설정
        colorAdjustments.contrast.overrideState = true;
        colorAdjustments.contrast.value = contrast;

        volume.isGlobal = true;
        volume.priority = 10;
    }

    void Update()
    {
        // 1. Vignette 값 업데이트
        if (vignette != null)
        {
            vignette.intensity.value = intensity;
            vignette.smoothness.value = smoothness;
        }

        // 2. Contrast 값 업데이트
        if (colorAdjustments != null)
        {
            colorAdjustments.contrast.value = contrast;
        }
    }
}