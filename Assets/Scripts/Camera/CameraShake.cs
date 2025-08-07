using Unity.Cinemachine;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    [SerializeField]
    private CinemachineImpulseSource impulseSource;

    [SerializeField]
    private float magnitude = 1f;

    [SerializeField]
    private float roughness = 1f;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            GenerateImpulse();
        }
    }

    public void GenerateImpulse()
    {
        // 방향 없이 그냥 흔들림
        impulseSource.GenerateImpulse();

        // 만약 방향 및 세기를 넣고 싶다면:
        // impulseSource.GenerateImpulse(Vector3.up * magnitude);
    }
}
