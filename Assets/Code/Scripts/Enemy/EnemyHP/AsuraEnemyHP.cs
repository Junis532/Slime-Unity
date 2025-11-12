using DG.Tweening;
using UnityEngine;
using TMPro;

public class AsuraEnemyHP : MonoBehaviour
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
    public bool useKnockback = true;   // ✅ Inspector에서 켜고 끌 수 있음
    public float knockbackDistance = 0.1f;
    public float knockbackDuration = 0.1f;


    private Transform playerTransform;
    private SpriteRenderer spriteRenderer;
    private float criticalChance;

    void Start()
    {
        maxHP = GameManager.Instance.asuraEnemyStats.maxHP;
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

        // 플레이어 찾기 (없을 경우 null)
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
        else
        {
            Debug.LogWarning("플레이어를 찾지 못했습니다. playerTransform이 null 상태입니다.");
            playerTransform = null;
        }
    }

    public void TakeDamage()
    {
        Vector3 knockbackDir = Vector3.zero;
        if (playerTransform != null)
        {
            knockbackDir = (transform.position - playerTransform.position).normalized;
        }

        bool isCritical = Random.Range(0f, 100f) < criticalChance;
        int damage = isCritical
            ? Mathf.RoundToInt(GameManager.Instance.playerStats.attack * 2f)
            : Mathf.RoundToInt(GameManager.Instance.playerStats.attack);

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
            GameManager.Instance.audioManager.PlayArrowHitSound(1.5f);
            ShowCDamageText(damage);
            GameManager.Instance.cameraShake.GenerateImpulse();
        }
        else
        {
            GameManager.Instance.audioManager.PlayArrowHitSound(1.5f);
            ShowDamageText(damage);
        }

        // ✅ 넉백 옵션 적용
        if (useKnockback && playerTransform != null)
        {
            transform.DOMove(transform.position + knockbackDir * knockbackDistance, knockbackDuration)
                     .SetEase(Ease.OutQuad);
        }

        if (currentHP <= 0)
            Die();
    }

    public void FireballTakeDamage(int damage)
    {
        GameManager.Instance.audioManager.PlayArrowHitSound(1.5f);
        currentHP -= damage;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);

        if (hpBar != null)
        {
            hpBar.SetHP(currentHP);
            hpBar.gameObject.SetActive(true);
        }

        PlayDamageEffect();
        ShowDamageText(damage);

        if (currentHP <= 0)
            Die();
    }

    public void SkillTakeDamage(int damage)
    {
        currentHP -= damage;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);

        if (hpBar != null)
        {
            hpBar.SetHP(currentHP);
            hpBar.gameObject.SetActive(true);
        }

        PlayDamageEffect();
        ShowDamageText(damage);

        if (currentHP <= 0)
            Die();
    }

    private void PlayHitEffect()
    {
        if (hitEffectPrefab == null) return;

        Vector3 spawnPosition = transform.position;
        GameObject effectObj = PoolManager.Instance.SpawnFromPool(
            hitEffectPrefab.name,
            spawnPosition,
            Quaternion.identity
        );

        if (effectObj == null) return;

        DOVirtual.DelayedCall(0.3f, () =>
        {
            PoolManager.Instance.ReturnToPool(effectObj);
        });
    }

    private void ShowDamageText(int damage)
    {
        if (damageTextPrefab == null) return;

        // ✅ 데미지가 0 이하일 경우 텍스트 띄우지 않음
        if (damage <= 0) return;

        Vector3 spawnPosition = transform.position;
        GameObject textObj = PoolManager.Instance.SpawnFromPool(
            damageTextPrefab.name,
            spawnPosition,
            Quaternion.identity
        );
        if (textObj == null) return;

        TMP_Text text = textObj.GetComponent<TMP_Text>();
        if (text != null)
            text.text = damage.ToString();

        Transform t = textObj.transform;
        t.position = spawnPosition;
        t.localScale = Vector3.one;
        t.DOMoveY(spawnPosition.y + 0.5f, 0.5f).SetEase(Ease.OutCubic);
        t.DOScale(1.2f, 0.2f).OnComplete(() =>
        {
            t.DOScale(1f, 0.3f);
        });

        DOVirtual.DelayedCall(0.6f, () =>
        {
            PoolManager.Instance.ReturnToPool(textObj);
        });
    }

    private void ShowCDamageText(int damage)
    {
        if (cDamageTextPrefab == null) return;

        Vector3 spawnPosition = transform.position;
        GameObject textObj = PoolManager.Instance.SpawnFromPool(
            cDamageTextPrefab.name,
            spawnPosition,
            Quaternion.identity
        );

        if (textObj == null) return;

        TMP_Text text = textObj.GetComponent<TMP_Text>();
        if (text != null)
            text.text = damage.ToString();

        Transform t = textObj.transform;
        t.position = spawnPosition;
        t.localScale = Vector3.one;
        t.DOMoveY(spawnPosition.y + 0.5f, 0.5f).SetEase(Ease.OutCubic);
        t.DOScale(1.2f, 0.2f).OnComplete(() =>
        {
            t.DOScale(1f, 0.3f);
        });

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

    private bool isDead = false; // 적이 죽었는지 상태

    private void Die()
    {
        if (isDead) return; // 이미 죽었으면 아무것도 하지 않음
        isDead = true;       // 죽음 상태로 변경

        if (hpBar != null)
            PoolManager.Instance.ReturnToPool(hpBar.gameObject);
        GameManager.Instance.cameraShake.GenerateImpulse();

        // ✅ 플레이어 HP 회복
        PlayerHeal playerHeal = Object.FindFirstObjectByType<PlayerHeal>();
        int healAmount = playerHeal.hpHealAmount; // PlayerHeal 스크립트에서 설정
        if (playerHeal != null && playerHeal.hpHeal)
        {
            GameManager.Instance.playerStats.currentHP += healAmount;
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
