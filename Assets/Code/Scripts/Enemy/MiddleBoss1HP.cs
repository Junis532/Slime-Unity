using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MiddleBoss1HP : MonoBehaviour
{
    [Header("체력 관련")]
    public GameObject hpBarPrefab; // 인스펙터 오류 방지를 위해 남겨둠
    private Image hpBarFill;
    public float currentHP;
    private float maxHP;

    private BulletSpawner bulletSpawner;

    [Header("데미지 텍스트")]
    public GameObject damageTextPrefab;
    public GameObject cDamageTextPrefab;

    [Header("이펙트 프리팹")]
    public GameObject hitEffectPrefab;

    [Header("넉백 설정")]
    public bool enableKnockback = true; // 넉백 ON/OFF

    private Transform playerTransform;
    private SpriteRenderer spriteRenderer;
    private float criticalChance;
    private bool isDead = false;

    void Start()
    {
        maxHP = GameManager.Instance.middleBoss1Stats.maxHP;
        currentHP = maxHP;
        criticalChance = GameManager.Instance.playerStats.criticalChance;

        GameObject bossHpBarUI = GameObject.Find("BossHP");
        if (bossHpBarUI == null)
        {
            Debug.LogError("Hierarchy에서 'BossHP' 오브젝트를 찾을 수 없습니다. 해당 오브젝트를 미리 배치해주세요!");
            return;
        }

        hpBarFill = bossHpBarUI.transform.Find("HPBar/HPFilled")?.GetComponent<Image>();
        if (hpBarFill == null)
        {
            Debug.LogError("'BossHP/HPFilled' Image 컴포넌트를 찾을 수 없습니다. UI 계층 구조를 확인하세요.");
            return;
        }

        bossHpBarUI.SetActive(true);
        UpdateHPBar();

        spriteRenderer = GetComponent<SpriteRenderer>();
        bulletSpawner = FindFirstObjectByType<BulletSpawner>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
    }

    private void UpdateHPBar()
    {
        if (hpBarFill != null)
        {
            hpBarFill.fillAmount = currentHP / maxHP;
        }
    }

    public void TakeDamage()
    {
        bool isCritical = Random.Range(0f, 100f) < criticalChance;
        int damage = isCritical
            ? Mathf.RoundToInt(GameManager.Instance.playerStats.attack * 2f)
            : Mathf.RoundToInt(GameManager.Instance.playerStats.attack);

        ApplyDamage(damage, isCritical);

        if (enableKnockback && playerTransform != null)
        {
            Vector3 knockbackDir = (transform.position - playerTransform.position).normalized;
            float knockbackDistance = 0.3f;
            float knockbackDuration = 0.1f;

            transform.DOMove(transform.position + knockbackDir * knockbackDistance, knockbackDuration)
                     .SetEase(Ease.OutQuad);
        }
    }

    public void FireballTakeDamage(int damage) => ApplyDamage(damage, false);
    public void SkillTakeDamage(int damage) => ApplyDamage(damage, false);

    private void ApplyDamage(int damage, bool isCritical)
    {
        if (isDead) return;

        currentHP -= damage;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);

        UpdateHPBar();

        if (!bulletSpawner.slowSkillActive)
        {
            PlayDamageEffect();
            PlayHitEffect();
        }

        if (isCritical)
        {
            AudioManager.Instance.PlaySFX(AudioManager.Instance.arrowHit);
            ShowCDamageText(damage);
            GameManager.Instance.cameraShake.GenerateImpulse();
        }
        else
        {
            AudioManager.Instance.PlaySFX(AudioManager.Instance.arrowHit);
            ShowDamageText(damage);
        }

        if (currentHP <= 0)
            Die();
    }

    private void PlayHitEffect()
    {
        if (hitEffectPrefab == null) return;
        GameObject effectObj = PoolManager.Instance.SpawnFromPool(hitEffectPrefab.name, transform.position, Quaternion.identity);
        if (effectObj == null) return;
        DOVirtual.DelayedCall(0.3f, () => PoolManager.Instance.ReturnToPool(effectObj));
    }

    private void ShowDamageText(int damage)
    {
        if (damageTextPrefab == null || damage <= 0) return;
        GameObject textObj = PoolManager.Instance.SpawnFromPool(damageTextPrefab.name, transform.position, Quaternion.identity);
        if (textObj == null) return;

        TMP_Text text = textObj.GetComponent<TMP_Text>();
        if (text != null) text.text = damage.ToString();

        Transform t = textObj.transform;
        t.DOMoveY(t.position.y + 0.5f, 0.5f).SetEase(Ease.OutCubic);
        t.DOScale(1.2f, 0.2f).OnComplete(() => t.DOScale(1f, 0.3f));
        DOVirtual.DelayedCall(0.6f, () => PoolManager.Instance.ReturnToPool(textObj));
    }

    private void ShowCDamageText(int damage)
    {
        if (cDamageTextPrefab == null) return;
        GameObject textObj = PoolManager.Instance.SpawnFromPool(cDamageTextPrefab.name, transform.position, Quaternion.identity);
        if (textObj == null) return;

        TMP_Text text = textObj.GetComponent<TMP_Text>();
        if (text != null) text.text = damage.ToString();

        Transform t = textObj.transform;
        t.DOMoveY(t.position.y + 0.5f, 0.5f).SetEase(Ease.OutCubic);
        t.DOScale(1.2f, 0.2f).OnComplete(() => t.DOScale(1f, 0.3f));
        DOVirtual.DelayedCall(0.6f, () => PoolManager.Instance.ReturnToPool(textObj));
    }

    private void PlayDamageEffect()
    {
        if (spriteRenderer == null) return;
        spriteRenderer.DOColor(Color.red, 0.1f).OnComplete(() => spriteRenderer.DOColor(Color.white, 0.1f));
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        GameObject bossHpBarUI = GameObject.Find("BossHPBarUI");
        if (bossHpBarUI != null)
        {
            bossHpBarUI.SetActive(false);
        }

        GameManager.Instance.cameraShake.GenerateImpulse();

        PlayerHeal playerHeal = FindFirstObjectByType<PlayerHeal>();
        if (playerHeal != null && playerHeal.hpHeal)
        {
            GameManager.Instance.playerStats.currentHP += playerHeal.hpHealAmount;
            GameManager.Instance.playerStats.currentHP =
                Mathf.Clamp(GameManager.Instance.playerStats.currentHP, 0, GameManager.Instance.playerStats.maxHP);
        }

        EnemiesDie enemiesDie = GetComponent<EnemiesDie>();
        if (enemiesDie != null) enemiesDie.Die();
    }
}
