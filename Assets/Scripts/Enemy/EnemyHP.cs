using DG.Tweening;
using UnityEngine;
using TMPro;

public class EnemyHP : MonoBehaviour
{
    [Header("체력 관련")]
    public GameObject hpBarPrefab;
    private EnemyHPBar hpBar;
    private float currentHP;
    private float maxHP;

    [Header("데미지 텍스트")]
    public GameObject damageTextPrefab; // 풀에 등록 필요

    private SpriteRenderer spriteRenderer;

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
    }

    public void TakeDamage()
    {
        int damage = Mathf.RoundToInt(GameManager.Instance.playerStats.attack);
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
