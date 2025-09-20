using UnityEngine;

public class FallingLightningEffect : MonoBehaviour
{
    public Vector3 targetPosition;
    public float fallSpeed = 30f;

    public GameObject impactEffectPrefab;
    public float impactEffectDuration = 1f;

    private bool isFalling = true;
    private Vector3 fallDirection;

    void Start()
    {
        // 도착 지점 Y축 위로 보정
        targetPosition += new Vector3(0f, 1.5f, 0f);
        // 시작 위치
        transform.position = targetPosition + Vector3.up * 5f;
        // 낙하 방향
        fallDirection = (targetPosition - transform.position).normalized;
        // Z축 회전
        float angle = Mathf.Atan2(fallDirection.y, fallDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void Update()
    {
        if (!isFalling) return;
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, fallSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) <= 0.05f)
        {
            isFalling = false;
            OnImpact();
        }
    }

    void OnImpact()
    {
        // 임팩트 이펙트
        if (impactEffectPrefab != null)
        {
            GameObject effect = Instantiate(impactEffectPrefab, targetPosition, Quaternion.identity);
            Destroy(effect, impactEffectDuration);
        }
        Destroy(gameObject);
    }
}
