using UnityEngine;
using DG.Tweening;
using System.Collections;

public class PlayerDamaged : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool isInvincible = false;  // ✅ 무적 상태

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    // ✅ 외부에서 호출하는 메서드
    public void TakeDamage(int damage)
    {
        if (isInvincible)
        {
            Debug.Log("무적 상태라 데미지 무시");
            return;
        }

        // ✅ 데미지 들어온 순간 바로 무적 ON
        isInvincible = true;
        StartCoroutine(InvincibleRoutine());

        // HP 감소
        GameManager.Instance.playerStats.currentHP -= damage;

        // 피격 효과 실행
        PlayDamageEffect();

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
        yield return new WaitForSeconds(1f); // ✅ 무적 유지
        isInvincible = false;
    }


    private void PlayDamageEffect()
    {
        if (spriteRenderer == null) return;

        StartCoroutine(InvincibleRoutine()); // ✅ 무적 시작

        spriteRenderer.DOKill();

        AudioManager.Instance.PlaySFX(AudioManager.Instance.hitSound);

        spriteRenderer.color = Color.red;
        spriteRenderer.DOColor(originalColor, 0.5f);

        Handheld.Vibrate();

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            var zacSkill = playerObj.GetComponent<ZacSkill>();
            if (zacSkill != null && zacSkill.enabled)
            {
                zacSkill.SpawnPieces();
            }
        }
    }
    public bool IsInvincible()
    {
        return isInvincible;
    }
}
