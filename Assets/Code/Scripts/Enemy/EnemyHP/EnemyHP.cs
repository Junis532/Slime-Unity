using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class EnemyHP : MonoBehaviour
{
    [Header("체력 관련")]
    public GameObject hpBarPrefab;
    private EnemyHPBar hpBar;

    [Tooltip("Inspector에서 직접 설정(예: 15000). useGameManagerHP=false일 때 이 값이 사용됩니다.")]
    public float maxHP = 15000f;
    public float currentHP;

    [Header("ID / 공유 HP 옵션")]
    [Tooltip("같은 ID를 가진 적끼리 HP를 공유하고 싶다면 값 지정")]
    public string ID = "";
    [Tooltip("같은 ID를 가진 적끼리 HP를 공유")]
    public bool shareHPByID = false;
    [Tooltip("씬 시작 시 이 개체의 maxHP로 SharedHP[ID]를 초기화")]
    public bool initializeSharedToMaxOnStart = true;

    [Header("체력 설정 소스")]
    [Tooltip("true면 GameManager.Instance.enemyStats.maxHP로 maxHP를 덮어씁니다")]
    public bool useGameManagerHP = false;

    private static readonly Dictionary<string, float> SharedHP = new();

    private BulletSpawner bulletSpawner;
    private Transform playerTransform;
    private SpriteRenderer spriteRenderer;
    private float criticalChance;

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

    [Header("상태")]
    public bool isDead = false;

    // ========== 라이프사이클 ==========

    void Start()
    {
        // 1) 최대 체력 결정 순서: GameManager → Inspector
        if (useGameManagerHP)
        {
            maxHP = GameManager.Instance.enemyStats.maxHP; // 예: 1000
        }
        // useGameManagerHP=false면 Inspector의 값(예: 15000)을 그대로 사용

        // 2) 공유 HP 초기화/동기화
        if (shareHPByID && !string.IsNullOrEmpty(ID))
        {
            if (initializeSharedToMaxOnStart || !SharedHP.ContainsKey(ID))
            {
                SharedHP[ID] = maxHP; // 이 개체의 maxHP로 시작값 고정 (예: 15000)
            }
            currentHP = Mathf.Clamp(SharedHP[ID], 0f, maxHP);
        }
        else
        {
            currentHP = maxHP;
        }

        // 3) 기타 레퍼런스
        criticalChance = GameManager.Instance.playerStats.criticalChance;

        spriteRenderer = GetComponent<SpriteRenderer>();
        bulletSpawner = Object.FindFirstObjectByType<BulletSpawner>();

        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;

        // 4) HP 바 생성(월드 캔버스에 붙임)
        Canvas worldCanvas = Object.FindAnyObjectByType<Canvas>();
        if (worldCanvas != null && hpBarPrefab != null)
        {
            GameObject hpBarObj = PoolManager.Instance.SpawnFromPool(hpBarPrefab.name, Vector3.zero, Quaternion.identity);
            if (hpBarObj != null)
            {
                hpBarObj.transform.SetParent(worldCanvas.transform, false);
                hpBar = hpBarObj.GetComponent<EnemyHPBar>();
                if (hpBar != null)
                {
                    hpBar.Init(transform, maxHP);
                    hpBar.gameObject.SetActive(false);
                }
            }
        }
    }

    void Update()
    {
        // 공유 HP 모드라면, 다른 동일 ID 개체가 먼저 맞아 SharedHP가 내려갔을 때 동기화
        if (shareHPByID && !string.IsNullOrEmpty(ID))
        {
            if (SharedHP.TryGetValue(ID, out float shared) && shared < currentHP)
            {
                currentHP = shared;
                if (hpBar != null) hpBar.SetHP(currentHP);
                if (currentHP <= 0f && !isDead) Die();
            }
        }
    }

    // ========== 외부 데미지 인터페이스 ==========

    // 일반 공격
    public void TakeDamage()
    {
        if (isDead) return;

        Vector3 knockDir = playerTransform != null
            ? (transform.position - playerTransform.position).normalized
            : Vector3.zero;

        bool isCritical = Random.Range(0f, 100f) < criticalChance;
        int baseAtk = Mathf.RoundToInt(GameManager.Instance.playerStats.attack);
        int damage = isCritical ? baseAtk * 2 : baseAtk;

        ApplyDamage(damage, isCritical, knockDir);
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

    // ========== 내부 공통 처리 ==========

    private void ApplyDamage(int damage, bool isCritical, Vector3 knockbackDir)
    {
        currentHP -= damage;
        currentHP = Mathf.Clamp(currentHP, 0f, maxHP);

        // 공유 HP 반영
        if (shareHPByID && !string.IsNullOrEmpty(ID))
            SharedHP[ID] = currentHP;

        // HP바 갱신/표시
        if (hpBar != null)
        {
            hpBar.SetHP(currentHP);
            hpBar.gameObject.SetActive(true);
        }

        // 이펙트 (슬로우 스킬 중엔 생략)
        if (bulletSpawner == null || !bulletSpawner.slowSkillActive)
        {
            PlayDamageEffect();
            PlayHitEffect();
        }

        // 사운드 & 텍스트 & 카메라 셰이크
        AudioManager.Instance.PlaySFX(AudioManager.Instance.arrowHit);
        if (isCritical)
        {
            ShowCriticalDamageText(damage);
            GameManager.Instance.cameraShake.GenerateImpulse();
        }
        else
        {
            ShowDamageText(damage);
        }

        // 넉백
        if (useKnockback && knockbackDir != Vector3.zero)
        {
            transform.DOMove(transform.position + knockbackDir * knockbackDistance, knockbackDuration)
                     .SetEase(Ease.OutQuad);
        }

        if (currentHP <= 0f) Die();
    }

    private void PlayHitEffect()
    {
        if (hitEffectPrefab == null) return;

        GameObject effectObj = PoolManager.Instance.SpawnFromPool(
            hitEffectPrefab.name, transform.position, Quaternion.identity);

        if (effectObj == null) return;

        DOVirtual.DelayedCall(0.15f, () =>
        {
            PoolManager.Instance.ReturnToPool(effectObj);
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

    private void ShowCriticalDamageText(int damage)
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

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        if (hpBar != null)
            PoolManager.Instance.ReturnToPool(hpBar.gameObject);

        GameManager.Instance.cameraShake.GenerateImpulse();

        // 플레이어 HP 회복(옵션)
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
        if (enemiesDie != null) enemiesDie.Die();

        // Crystal 레이어: Turret 레이어 적 모두 제거
        if (gameObject.layer == LayerMask.NameToLayer("Crystal"))
        {
            EnemyHP[] allEnemies = Object.FindObjectsByType<EnemyHP>(FindObjectsSortMode.None);
            foreach (EnemyHP turretHP in allEnemies)
            {
                if (turretHP.gameObject.layer == LayerMask.NameToLayer("Turret") && !turretHP.isDead)
                {
                    turretHP.currentHP = 0f;
                    if (turretHP.shareHPByID && !string.IsNullOrEmpty(turretHP.ID))
                        SharedHP[turretHP.ID] = 0f;

                    turretHP.Die();
                    Debug.Log($"Turret killed: {turretHP.name}");
                }
            }
        }
    }

    // ========== 유틸(테스트/초기화) ==========

    /// <summary>현재 실행 중 SharedHP 테이블 전체 초기화(테스트용)</summary>
    public static void ClearAllSharedHP() => SharedHP.Clear();

    /// <summary>특정 ID의 공유 HP를 지정 값으로 설정</summary>
    public static void SetSharedHP(string id, float value)
    {
        if (string.IsNullOrEmpty(id)) return;
        SharedHP[id] = value;
    }

    /// <summary>특정 ID의 공유 HP를 제거</summary>
    public static void RemoveSharedHP(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        if (SharedHP.ContainsKey(id)) SharedHP.Remove(id);
    }
}
