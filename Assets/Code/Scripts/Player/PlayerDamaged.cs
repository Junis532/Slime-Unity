using UnityEngine;
using DG.Tweening;
using System.Collections;

public class PlayerDamaged : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private PlayerController playerController; // PlayerController 참조
    private bool isInvincible = false;

    [Header("피격 관련")]
    public float knockbackDistance = 0.5f;
    public float knockbackDuration = 0.2f;
    public float invincibleDuration = 1f; // 무적 시간 (InvincibleRoutine과 일치)

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        // PlayerController 참조를 가져옵니다.
        playerController = GetComponent<PlayerController>();
    }

    /// <summary>
    /// 플레이어가 데미지를 입고 넉백됩니다.
    /// </summary>
    /// <param name="damage">입은 데미지량</param>
    /// <param name="enemyPosition">공격한 적의 위치 (넉백 방향 계산에 사용)</param>
    public void TakeDamage(int damage, Vector3 enemyPosition) // <--- Vector3 인자 추가!
    {
        if (isInvincible)
        {
            Debug.Log("무적 상태라 데미지 무시");
            return;
        }

        isInvincible = true;

        StartCoroutine(InvincibleRoutine());

        // HP 감소
        GameManager.Instance.playerStats.currentHP -= damage;

        // 피격 효과 및 넉백 실행
        PlayDamageEffect(enemyPosition); // <--- 적 위치를 전달합니다.

        // 체력이 0 이하일 때 처리
        if (GameManager.Instance.playerStats.currentHP <= 0)
        {
            GameManager.Instance.playerStats.currentHP = 0;
            Debug.Log("플레이어 사망!");
            // TODO: GameManager.Instance.GameOver(); 등으로 연결
        }
    }

    private IEnumerator InvincibleRoutine()
    {
        yield return new WaitForSeconds(invincibleDuration); // 무적 유지
        isInvincible = false;

        // 무적 시간이 넉백 시간보다 길 때, 넉백 완료 후 이동이 켜졌으므로 여기서 다시 켤 필요는 없습니다.
        // Die()에서 멈췄다면 Die()에서만 처리하면 됩니다.
    }

    private void PlayDamageEffect(Vector3 enemyPosition)
    {
        if (spriteRenderer == null) return;

        spriteRenderer.DOKill();

        // 색 변경
        spriteRenderer.color = Color.red;
        spriteRenderer.DOColor(originalColor, 0.5f);

        // -----------------------------------------------------
        // Bridge 위면 넉백 무시
        // -----------------------------------------------------
        if (playerController != null && playerController.bridge != null && playerController.bridge.PlayerOnBridge())
        {
            // 다리 위면 넉백을 하지 않고 종료
            return;
        }

        // 1. 넉백 방향 계산
        Vector3 knockbackDir = (transform.position - enemyPosition).normalized;

        // 2. DOTween으로 넉백 이동 실행
        transform.DOMove(transform.position + knockbackDir * knockbackDistance, knockbackDuration)
                 .SetEase(Ease.OutQuad)
                 .OnComplete(() => {
                     // 넉백 완료 후 처리
                 });
    }

    public bool IsInvincible()
    {
        return isInvincible;
    }
}