using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Boss1HP : MonoBehaviour
{
    [Header("체력 관련")]
    public GameObject hpBarPrefab; // 하이어라키에 소환할 프리팹
    private GameObject hpBarUI;    // 런타임에 생성될 오브젝트
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
    public bool useKnockback = true;
    public float knockbackDistance = 0.3f;
    public float knockbackDuration = 0.1f;

    private Transform playerTransform;
    private SpriteRenderer spriteRenderer;
    private float criticalChance;
    private bool isDead = false;

    private int playerHitCount = 0; // 플레이어 맞은 횟수


    // 💡 HP바가 한 번만 생성되었는지 체크
    private static bool hpBarCreated = false;

    void Start()
    {
        maxHP = GameManager.Instance.boss1Stats.maxHP;
        currentHP = maxHP;
        criticalChance = GameManager.Instance.playerStats.criticalChance;

        // 💡 HP바 한 번만 생성
        if (!hpBarCreated && hpBarPrefab != null)
        {
            hpBarUI = Instantiate(hpBarPrefab);
            hpBarUI.SetActive(true);

            hpBarFill = hpBarUI.transform.Find("HPBar/HPFilled")?.GetComponent<Image>();
            if (hpBarFill == null)
                Debug.LogError("'HPBar/HPFilled' Image 컴포넌트를 찾을 수 없습니다.");

            hpBarCreated = true;
        }
        else if (hpBarCreated)
        {
            // 이미 생성된 경우, 기존 HP바 찾아 연결
            hpBarUI = GameObject.FindWithTag("HP"); // prefab에 태그 BossHPBar 추가 필요
            if (hpBarUI != null)
                hpBarFill = hpBarUI.transform.Find("HPBar/HPFilled")?.GetComponent<Image>();
        }

        UpdateHPBar();

        spriteRenderer = GetComponent<SpriteRenderer>();
        bulletSpawner = FindFirstObjectByType<BulletSpawner>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            playerTransform = playerObj.transform;
    }

    private void Update()
    {
        if (hpBarUI != null && !isDead)
        {
            hpBarUI.transform.position = transform.position + Vector3.up * 2f;
        }
    }

    private void UpdateHPBar()
    {
        if (hpBarFill != null)
            hpBarFill.fillAmount = currentHP / maxHP;
    }

    public void TakeDamage()
    {
        Vector3 knockbackDir = playerTransform != null ? (transform.position - playerTransform.position).normalized : Vector3.zero;

        bool isCritical = Random.Range(0f, 100f) < criticalChance;
        int damage = isCritical
            ? Mathf.RoundToInt(GameManager.Instance.playerStats.attack * 2f)
            : Mathf.RoundToInt(GameManager.Instance.playerStats.attack);

        ApplyDamage(damage, isCritical);

        if (useKnockback && playerTransform != null)
        {
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

        // FireBoss 스킬 강제 종료
        FireBoss fireBoss = GetComponent<FireBoss>();
        if (fireBoss != null)
        {
            fireBoss.OnBossTakeDamage(); // 이미 FireBoss 스크립트에 구현되어 있는 강제 종료 로직 호출
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

        if (hpBarUI != null)
            hpBarUI.SetActive(false);

        GameManager.Instance.cameraShake.GenerateImpulse();

        // 플레이어 HP 회복
        PlayerHeal playerHeal = FindFirstObjectByType<PlayerHeal>();
        if (playerHeal != null && playerHeal.hpHeal)
        {
            GameManager.Instance.playerStats.currentHP += playerHeal.hpHealAmount;
            GameManager.Instance.playerStats.currentHP =
                Mathf.Clamp(GameManager.Instance.playerStats.currentHP, 0, GameManager.Instance.playerStats.maxHP);
        }

        EnemiesDie enemiesDie = GetComponent<EnemiesDie>();
        if (enemiesDie != null)
            enemiesDie.Die();
    }
}
