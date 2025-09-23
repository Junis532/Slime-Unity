using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BiggerBullet : MonoBehaviour
{
    [Header("성장 설정")]
    public float scaleIncrease = 0.1f;   // 0.2초마다 얼마나 커질지
    public float colliderIncrease = 0.1f;// 0.2초마다 콜라이더 얼마나 키울지
    public float interval = 0.2f;        // 성장 주기 (0.2초)
    public int growSteps = 10;           // 몇 번 커질지 (-1이면 무한)

    private Collider2D col;
    private Vector3 originalScale;
    private int steps = 0;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        originalScale = transform.localScale;
    }

    void OnEnable()
    {
        steps = 0;
        transform.localScale = originalScale;
        StartCoroutine(GrowRoutine());
    }

    IEnumerator GrowRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(interval);

        while (growSteps < 0 || steps < growSteps)
        {
            // 스케일 키우기
            transform.localScale += Vector3.one * scaleIncrease;

            // 콜라이더 크기 조정 (CircleCollider2D, BoxCollider2D 모두 대응)
            if (col is CircleCollider2D circle)
            {
                circle.radius += colliderIncrease;
            }
            else if (col is BoxCollider2D box)
            {
                box.size += Vector2.one * colliderIncrease;
            }
            else if (col is CapsuleCollider2D capsule)
            {
                capsule.size += Vector2.one * colliderIncrease;
            }

            steps++;
            yield return wait;
        }
    }
}
