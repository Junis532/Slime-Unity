using UnityEngine;
using DG.Tweening;
using System.Collections;

public class PlayerDamaged : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private PlayerController playerController;
    private bool isInvincible = false;

    [Header("플레이어 트랜스폼")]
    public Transform playerTransform; // ✅ 플레이어 트랜스폼 (직접 할당 가능)

    [Header("피격 관련")]
    public float knockbackDistance = 0.5f;
    public float knockbackDuration = 0.2f;
    public float invincibleDuration = 1f;

    [Header("이펙트 관련")]
    public GameObject hitEffectPrefab; // ✅ 피격 이펙트 프리팹

   void Start()
{
    // playerTransform이 안 들어왔으면 자기 자신 사용
    if (playerTransform == null)
        playerTransform = transform;

    // playerTransform 기준으로 SpriteRenderer 가져오기
    spriteRenderer = playerTransform.GetComponent<SpriteRenderer>();
    if (spriteRenderer != null)
        originalColor = spriteRenderer.color;

    playerController = playerTransform.GetComponent<PlayerController>();
}


    public void TakeDamage(int damage, Vector3 enemyPosition)
    {
        if (isInvincible)
        {
            Debug.Log("무적 상태라 데미지 무시");
            return;
        }

        isInvincible = true;
        StartCoroutine(InvincibleRoutine());
        
        //  피격 이펙트 재생
        PlayHitEffect();
        playDamageColor();

        GameManager.Instance.vignetEffect.PlayDamageFlash(0.6f, 0.5f);

        // HP 감소
        GameManager.Instance.playerStats.currentHP -= damage;

        // 사망 체크
        if (GameManager.Instance.playerStats.currentHP <= 0)
        {
            GameManager.Instance.playerStats.currentHP = 0;
            Debug.Log("플레이어 사망!");
        }
    }

    private IEnumerator InvincibleRoutine()
    {
        yield return new WaitForSeconds(invincibleDuration);
        isInvincible = false;
    }

    private void playDamageColor()
    {
        if (spriteRenderer == null) return;

        // DOColor 충돌 방지
        spriteRenderer.DOKill();

        // Material 인스턴스화 (공유 마테리얼 문제 해결)
        spriteRenderer.material = new Material(spriteRenderer.material);

        spriteRenderer.color = Color.red;
        spriteRenderer.DOColor(originalColor, 0.5f).SetEase(Ease.OutQuad);
    }


    //private void PlayDamageEffect(Vector3 enemyPosition)
    //{
    //    if (spriteRenderer == null) return;

    //    spriteRenderer.DOKill();
    //    spriteRenderer.color = Color.red;
    //    spriteRenderer.DOColor(originalColor, 0.5f);

    //    // ✅ playerTransform 기준으로 넉백 계산
    //    Vector3 knockbackDir = (playerTransform.position - enemyPosition).normalized;

    //    playerTransform.DOMove(playerTransform.position + knockbackDir * knockbackDistance, knockbackDuration)
    //                   .SetEase(Ease.OutQuad);
    //}

    /// <summary>
    /// 플레이어 기준으로 피격 이펙트 재생
    /// </summary>
    private void PlayHitEffect()
    {
        if (hitEffectPrefab == null || playerTransform == null) return;

        // ✅ 처음 위치 (플레이어 머리 근처)
        Vector3 effectPos = playerTransform.position + new Vector3(0, 0.3f, 0);

        // ✅ 풀에서 스폰
        GameObject effectObj = PoolManager.Instance.SpawnFromPool(
            hitEffectPrefab.name, effectPos, Quaternion.identity);

        if (effectObj == null) return;

        // 🔥 코루틴으로 일정 시간 동안 플레이어 위치 따라가기
        StartCoroutine(FollowPlayerEffect(effectObj));

        // ⏳ 0.5초 뒤 자동 반환
        DOVirtual.DelayedCall(0.15f, () =>
        {
            if (effectObj != null)
                PoolManager.Instance.ReturnToPool(effectObj);
        });
    }

    private IEnumerator FollowPlayerEffect(GameObject effectObj)
    {
        float duration = 0.5f; // 따라다니는 시간 (이펙트 지속시간과 동일)
        float elapsed = 0f;

        // 플레이어보다 약간 위쪽 오프셋
        Vector3 offset = new Vector3(0, 0.3f, 0);

        while (elapsed < duration && effectObj != null && playerTransform != null)
        {
            effectObj.transform.position = playerTransform.position + offset;
            elapsed += Time.deltaTime;
            yield return null;
        }
    }


    public bool IsInvincible() => isInvincible;
}
