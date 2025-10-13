using UnityEngine;

public class triggerWall : MonoBehaviour
{
    [Tooltip("막을 대상 태그 (Player 등)")]
    public string targetTag = "Player";

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag(targetTag)) return;

        Rigidbody2D rb = other.attachedRigidbody;
        if (rb == null) return;

        // 플레이어 → 벽 방향 벡터 구함 (벽 중심에서 플레이어를 향한 방향)
        Vector2 pushDir = (other.transform.position - transform.position).normalized;

        // 밀어내는 거리
        float pushStrength = 3f;

        // 물리 이동(velocity 기반 이동이 아니라 position 보정)
        rb.MovePosition(rb.position + pushDir * Time.deltaTime * pushStrength);

        // 속도 초기화 (즉시 멈춤)
        rb.linearVelocity = Vector2.zero;
    }
}
