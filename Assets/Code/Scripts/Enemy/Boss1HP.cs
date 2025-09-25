using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Boss1HP : MonoBehaviour
{
    [Header("체력 관련")]
    public GameObject hpBarPrefab; // 사용하지 않지만 인스펙터 오류 방지용
    private Image hpBarFill;
    public float currentHP;
    private float maxHP;

    private BulletSpawner bulletSpawner;

    [Header("데미지 텍스트")]
    public GameObject damageTextPrefab;
    public GameObject cDamageTextPrefab;

    [Header("이펙트 프리팹")]
    public GameObject hitEffectPrefab;

    [Header("넉백 옵션")]
    public bool useKnockback = true; // Inspector에서 켜고 끌 수 있음
    public float knockbackDistance = 0.3f;
    public float knockbackDuration = 0.1f;

    private Transform playerTransform;
    private SpriteRenderer spriteRenderer;
    private float criticalChance;
    private bool isDead = false;

    void Start()
    {
        maxHP = GameManager.Instance.boss1Stats.maxHP;
        currentHP = maxHP;
        criticalChance = GameManager.Instance.playerStats.criticalChance;

        // HP 바 세팅
        GameObject bossHpBarUI = GameObject.Find("BossHP");
        if (bossHpBarUI == null)
        {
            Debug.LogError("Hierarchy에서 'BossHP' 오브젝트를 찾을 수 없습니다!");
            return;
        }

        hpBarFill = bossHpBarUI.transform.Find("HPBar/HPFilled")?.GetComponent<Image>();
        if (hpBarFill == null)
        {
            Debug.LogError("'BossHP/HPFilled' Image 컴포넌트를 찾을 수 없습니다.");
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
        Vector3 knockbackDir = Vector3.zero;
        if (playerTransform != null)
            knockbackDir = (transform.position - playerTransform.position).normalized;

        bool isCritical = Random.Range(0f, 100f) < criticalChance;
        int damage = isCritical
            ? Mathf.RoundToInt(GameManager.Instance.playerStats.attack * 2f)
            : Mathf.RoundToInt(GameManager.Instance.playerStats.attack);

        ApplyDamage(damage, isCritical);

        // 넉백 적용 (옵션)
        if (useKnockback && playerTransform != null)
        {
            transform.DOMove(transform.position + knockbackDir * knockbackDistance, knockbackDuration)
                     .SetEase(Ease.OutQuad);
        }
    }

    public void FireballTakeDamage(int damage)
    {
        ApplyDamage(damage, false);
    }

    public void SkillTakeDamage(int damage)
    {
        ApplyDamage(damage, false);
    }

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

        // 플레이어 HP 회복
        PlayerHeal playerHeal = FindFirstObjectByType<PlayerHeal>();
        if (playerHeal != null && playerHeal.hpHeal)
        {
            GameManager.Instance.playerStats.currentHP += playerHeal.hpHealAmount;
            GameManager.Instance.playerStats.currentHP = Mathf.Clamp(
                GameManager.Instance.playerStats.currentHP,
                0,
                GameManager.Instance.playerStats.maxHP
            );
        }

        EnemiesDie enemiesDie = GetComponent<EnemiesDie>();
        if (enemiesDie != null)
            enemiesDie.Die();
    }
}
