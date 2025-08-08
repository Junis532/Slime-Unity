using Unity.Cinemachine;
using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    [SerializeField]
    private CinemachineImpulseSource impulseSource;

    [SerializeField]
    private float magnitude = 1f;

    [SerializeField]
    private float roughness = 1f;

    private CinemachineCamera virtualCamera;
    private CinemachineBasicMultiChannelPerlin noise;

    private void Awake()
    {
        virtualCamera = GetComponent<CinemachineCamera>();
        if (virtualCamera != null)
            noise = virtualCamera.GetCinemachineComponent(CinemachineCore.Stage.Noise) as CinemachineBasicMultiChannelPerlin;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            GenerateImpulse();
        }
    }

    public void GenerateImpulse()
    {
        impulseSource.GenerateImpulse();
    }

    public void ShakeForSeconds(float duration = 2f)
    {
        if (noise != null)
        {
            StopAllCoroutines();
            StartCoroutine(ShakeCoroutine(duration));
        }
        else
        {
            GenerateImpulse();
        }
    }

    private IEnumerator ShakeCoroutine(float duration)
    {
        // »ÁµÈ±‚ Ω√¿€
        noise.AmplitudeGain = magnitude;
        noise.FrequencyGain = roughness;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // »ÁµÈ±‚ ¡æ∑·
        noise.AmplitudeGain = 0f;
        noise.FrequencyGain = 0f;
    }
}
