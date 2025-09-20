using UnityEngine;

public class FireballVFX : MonoBehaviour
{
    public Transform parentProjectile;   // 부모 투사체 Transform
    public ParticleSystem ps;
    public float speedMagnitude = 5f;

    void Update()
    {
        if (parentProjectile == null || ps == null)
            return;

        Vector3 parentDir = (parentProjectile.position - transform.position).normalized;
        // 또는 parentProjectile.forward, 프로젝트 방향에 따라 다름

        Vector3 reverseDir = -parentDir;

        var velOverLifetime = ps.velocityOverLifetime;
        velOverLifetime.enabled = true;
        velOverLifetime.space = ParticleSystemSimulationSpace.Local;

        velOverLifetime.x = new ParticleSystem.MinMaxCurve(reverseDir.x * speedMagnitude);
        velOverLifetime.y = new ParticleSystem.MinMaxCurve(reverseDir.y * speedMagnitude);
    }
}
