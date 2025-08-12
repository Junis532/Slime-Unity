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
    public GameObject damageTextPrefab; // 풀에 등록 필요
    public GameObject cDamageTextPrefab; // 풀에서 가져온 인스턴스

    [Header("이펙트 프리팹")]
    public GameObject hitEffectPrefab; // 이펙트 프리팹 (풀 등록 필요)

    private Transform playerTransform;


    private SpriteRenderer spriteRenderer;

    private float criticalChance = 10f;

    void Start()
    {
        maxHP = GameManager.Instance.enemyStats.maxHP;
        currentHP = maxHP;

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

        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
    }

    public void TakeDamage()
    {
        Vector3 knockbackDir = (transform.position - playerTransform.position).normalized;
        float knockbackDistance = 0.5f;
        float knockbackDuration = 0.1f;



        if (Random.Range(0f, 100f) < criticalChance)
        {
            Debug.Log("Critical Hit!");
            int damage = Mathf.RoundToInt(GameManager.Instance.playerStats.attack * 1.5f);
            currentHP -= damage;
            currentHP = Mathf.Clamp(currentHP, 0, maxHP);

            if (hpBar != null)
            {
                hpBar.SetHP(currentHP);
                hpBar.gameObject.SetActive(true);
            }

            // 슬로우 중이 아닐 때만 이펙트 재생
            if (!bulletSpawner.slowSkillActive)
            {
                PlayDamageEffect();
                PlayHitEffect();
            }

            ShowCDamageText(damage);
            GameManager.Instance.cameraShake.GenerateImpulse();

            // 나머지 기존 TakeDamage 코드 내에서 넉백 적용
            transform.DOMove(transform.position + knockbackDir * knockbackDistance, knockbackDuration)
                     .SetEase(Ease.OutQuad);

            if (currentHP <= 0)
                Die();
        }
        else
        {
            int damage = Mathf.RoundToInt(GameManager.Instance.playerStats.attack);
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

            ShowDamageText(damage);

            // DOTween 넉백
            transform.DOMove(transform.position + knockbackDir * knockbackDistance, knockbackDuration)
                     .SetEase(Ease.OutQuad);

            if (currentHP <= 0)
                Die();
        }
    }


    public void FireballTakeDamage(int damage)
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
        if (hitEffectPrefab == null)
        {
            Debug.LogWarning("hitEffectPrefab이 설정되지 않았습니다.");
            return;
        }

        Vector3 spawnPosition = transform.position;

        GameObject effectObj = PoolManager.Instance.SpawnFromPool(
            hitEffectPrefab.name,
            spawnPosition,
            Quaternion.identity
        );

        if (effectObj == null)
        {
            Debug.LogWarning("hitEffectPrefab이 풀에서 생성되지 않았습니다.");
            return;
        }

        // 이펙트 0.3초 후 풀로 반환
        DOVirtual.DelayedCall(0.3f, () =>
        {
            PoolManager.Instance.ReturnToPool(effectObj);
        });
    }


    private void ShowDamageText(int damage)
    {
        if (damageTextPrefab == null)
        {
            Debug.LogWarning("damageTextPrefab이 설정되지 않았습니다.");
            return;
        }

        Vector3 spawnPosition = transform.position;
        GameObject textObj = PoolManager.Instance.SpawnFromPool(
            damageTextPrefab.name,
            spawnPosition,
            Quaternion.identity
        );

        if (textObj == null)
        {
            Debug.LogWarning("damageTextObj가 null입니다. 풀에 damageTextPrefab이 등록되었는지 확인하세요.");
            return;
        }

        TMP_Text text = textObj.GetComponent<TMP_Text>();
        if (text != null)
        {
            text.text = damage.ToString();
        }
        else
        {
            Debug.LogWarning("damageTextObj에 TMP_Text 컴포넌트가 없습니다.");
        }

        Transform t = textObj.transform;
        if (t != null)
        {
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
    }

    private void ShowCDamageText(int damage)
    {
        if (cDamageTextPrefab == null)
        {
            Debug.LogWarning("cDamageTextPrefab이 설정되지 않았습니다.");
            return;
        }

        Vector3 spawnPosition = transform.position;
        GameObject textObj = PoolManager.Instance.SpawnFromPool(
            cDamageTextPrefab.name,
            spawnPosition,
            Quaternion.identity
        );

        if (textObj == null)
        {
            Debug.LogWarning("cDamageTextObj가 null입니다. 풀에 damageTextPrefab이 등록되었는지 확인하세요.");
            return;
        }

        TMP_Text text = textObj.GetComponent<TMP_Text>();
        if (text != null)
        {
            text.text = damage.ToString();
        }
        else
        {
            Debug.LogWarning("cDamageTextObj에 TMP_Text 컴포넌트가 없습니다.");
        }

        Transform t = textObj.transform;
        if (t != null)
        {
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
    }


    private void PlayDamageEffect()
    {
        if (spriteRenderer == null) return;

        spriteRenderer.DOColor(Color.red, 0.1f).OnComplete(() =>
        {
            spriteRenderer.DOColor(Color.white, 0.1f);
        });
    }

    private void Die()
    {
        if (hpBar != null)
            PoolManager.Instance.ReturnToPool(hpBar.gameObject);

        EnemiesDie enemiesDie = GetComponent<EnemiesDie>();
        if (enemiesDie != null)
        {
            enemiesDie.Die();
        }
    }
}
