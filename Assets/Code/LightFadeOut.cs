using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class LightFadeOut : MonoBehaviour
{
    public Light2D light2D;      // Inspector에서 할당 (혹은 GetComponent)
    public float fadeDuration = 1f; // 사라지는 시간 (1초)

    void Start()
    {
        if (light2D == null)
            light2D = GetComponent<Light2D>();

        StartCoroutine(FadeOutLight());
    }

    private IEnumerator FadeOutLight()
    {
        float startIntensity = light2D.intensity;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            light2D.intensity = Mathf.Lerp(startIntensity, 0f, elapsed / fadeDuration);
            yield return null;
        }

        light2D.intensity = 0f; // 마지막으로 완전히 끔
    }
} 