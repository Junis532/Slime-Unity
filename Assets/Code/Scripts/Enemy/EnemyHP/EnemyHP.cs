using DG.Tweening;
using UnityEngine;
using TMPro;

public class EnemyHP : MonoBehaviour
{
    [Header("체력 관련")]
    public GameObject hpBarPrefab;
    private EnemyHPBar hpBar;
    public float currentHP;
    private float maxHP;

    private BulletSpawner bulletSpawner;

    [Header("데미지 텍스트")]
    public GameObject damageTextPrefab;
    public GameObject cDamageTextPrefab;

    [Header("이펙트 프리팹")]
    public GameObject hitEffectPrefab;

    [Header("도트 데미지 이펙트 프리팹")]
    public GameObject dotEffectPrefab;
    private GameObject activeDotEffect;

    [Header("넉백 옵션")]
    public bool useKnockback = true;
    public float knockbackDistance = 0.1f;
    public float knockbackDuration = 0.1f;

    private Transform playerTransform;
    private SpriteRenderer spriteRenderer;
    private float criticalChance;

    public bool isDead = false; // 적이 죽었는지 상태

    void Start()
    {
        maxHP = GameManager.Instance.enemyStats.maxHP;
        currentHP = maxHP;
        criticalChance = GameManager.Instance.playerStats.criticalChance;

        // 월드 캔버스에 HP 바 붙이기
        Canvas worldCanvas = Object.FindAnyObjectByType<Canvas>();
        GameObject hpBarObj = PoolManager.Instance.SpawnFromPool(
            hpBarPrefab.name, Vector3.zero, Quaternion.identity);
        hpBarObj.transform.SetParent(worldCanvas.transform, false);

        hpBar = hpBarObj.GetComponent<EnemyHPBar>();
        hpBar.Init(transform, maxHP);
        hpBarObj.SetActive(false);

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            Debug.LogWarning("SpriteRenderer를 찾지 못했습니다.");

        bulletSpawner = Object.FindFirstObjectByType<BulletSpawner>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            playerTransform = playerObj.transform;
        else
            Debug.LogWarning("플레이어를 찾지 못했습니다. playerTransform이 null 상태입니다.");
    }

    // 일반 공격
    public void TakeDamage()
    {
        if (isDead) return;

        Vector3 knockbackDir = playerTransform != null
            ? (transform.position - playerTransform.position).normalized
            : Vector3.zero;

        bool isCritical = Random.Range(0f, 100f) < criticalChance;
        int damage = isCritical
            ? Mathf.RoundToInt(GameManager.Instance.playerStats.attack * 2f)
            : Mathf.RoundToInt(GameManager.Instance.playerStats.attack);

        ApplyDamage(damage, isCritical, knockbackDir);
    }

    // 파이어볼 데미지
    public void FireballTakeDamage(int damage)
    {
        if (isDead) return;
        ApplyDamage(damage, false, Vector3.zero);
    }

    // 스킬 데미지
    public void SkillTakeDamage(int damage)
    {
        if (isDead) return;
        ApplyDamage(damage, false, Vector3.zero);
    }

    // 데미지 적용 공통 함수
    private void ApplyDamage(int damage, bool isCritical, Vector3 knockbackDir)
    {
        currentHP -= damage;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);

        if (hpBar != null)
        {
            hpBar.SetHP(currentHP);
            hpBar.gameObject.SetActive(true);
        }

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

        if (useKnockback && knockbackDir != Vector3.zero)
        {
            transform.DOMove(transform.position + knockbackDir * knockbackDistance, knockbackDuration)
                     .SetEase(Ease.OutQuad);
        }

        if (currentHP <= 0)
            Die();
    }

    private void PlayHitEffect()
    {
        if (hitEffectPrefab == null) return;

        GameObject effectObj = PoolManager.Instance.SpawnFromPool(
            hitEffectPrefab.name, transform.position, Quaternion.identity);

        if (effectObj == null) return;

        DOVirtual.DelayedCall(0.3f, () =>
        {
            PoolManager.Instance.ReturnToPool(effectObj);
        });
    }

    private void ShowDamageText(int damage)
    {
        if (damageTextPrefab == null || damage <= 0) return;

        GameObject textObj = PoolManager.Instance.SpawnFromPool(
            damageTextPrefab.name, transform.position, Quaternion.identity);

        if (textObj == null) return;

        TMP_Text text = textObj.GetComponent<TMP_Text>();
        if (text != null) text.text = damage.ToString();

        Transform t = textObj.transform;
        t.position = transform.position;
        t.localScale = Vector3.one;
        t.DOMoveY(t.position.y + 0.5f, 0.5f).SetEase(Ease.OutCubic);
        t.DOScale(1.2f, 0.2f).OnComplete(() => t.DOScale(1f, 0.3f));

        DOVirtual.DelayedCall(0.6f, () =>
        {
            PoolManager.Instance.ReturnToPool(textObj);
        });
    }

    private void ShowCDamageText(int damage)
    {
        if (cDamageTextPrefab == null) return;

        GameObject textObj = PoolManager.Instance.SpawnFromPool(
            cDamageTextPrefab.name, transform.position, Quaternion.identity);

        if (textObj == null) return;

        TMP_Text text = textObj.GetComponent<TMP_Text>();
        if (text != null) text.text = damage.ToString();

        Transform t = textObj.transform;
        t.position = transform.position;
        t.localScale = Vector3.one;
        t.DOMoveY(t.position.y + 0.5f, 0.5f).SetEase(Ease.OutCubic);
        t.DOScale(1.2f, 0.2f).OnComplete(() => t.DOScale(1f, 0.3f));

        DOVirtual.DelayedCall(0.6f, () =>
        {
            PoolManager.Instance.ReturnToPool(textObj);
        });
    }

    private void PlayDamageEffect()
    {
        if (spriteRenderer == null) return;

        spriteRenderer.DOColor(Color.red, 0.1f).OnComplete(() =>
        {
            spriteRenderer.DOColor(Color.white, 0.1f);
        });
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        if (hpBar != null)
            PoolManager.Instance.ReturnToPool(hpBar.gameObject);
        GameManager.Instance.cameraShake.GenerateImpulse();

        // 플레이어 HP 회복
        PlayerHeal playerHeal = Object.FindFirstObjectByType<PlayerHeal>();
        if (playerHeal != null && playerHeal.hpHeal)
        {
            GameManager.Instance.playerStats.currentHP += playerHeal.hpHealAmount;
            GameManager.Instance.playerStats.currentHP = Mathf.Clamp(
                GameManager.Instance.playerStats.currentHP,
                0,
                GameManager.Instance.playerStats.maxHP
            );
        }

        // 본인 사망 처리
        EnemiesDie enemiesDie = GetComponent<EnemiesDie>();
        if (enemiesDie != null)
            enemiesDie.Die();

        // Crystal 레이어 처리: Turret 레이어 적 모두 죽이기
        if (gameObject.layer == LayerMask.NameToLayer("Crystal"))
        {
            // FindObjectsByType 사용, 정렬 필요 없으므로 FindObjectsSortMode.None
            EnemyHP[] allEnemies = Object.FindObjectsByType<EnemyHP>(FindObjectsSortMode.None);
            foreach (EnemyHP turretHP in allEnemies)
            {
                if (turretHP.gameObject.layer == LayerMask.NameToLayer("Turret") && !turretHP.isDead)
                {
                    turretHP.currentHP = 0;
                    turretHP.Die();
                    Debug.Log($"Turret killed: {turretHP.name}");
                }
            }
        }

    }
}
