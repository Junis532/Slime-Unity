using DG.Tweening;
using UnityEngine;
using System.Collections;

public interface IBossHPView
{
    void Init(float maxHP, float currentHP);
    void SetHP(float currentHP, float maxHP);
    void Show();
    void Hide();
}

[DisallowMultipleComponent]
public class MiddleBoss1HP : MonoBehaviour
{
    // ───────── 자동 탐색 설정(필요시 바꿔도 무방) ─────────
    private const string BossHpViewTag = "BossHPView"; // 있으면 최우선
    private const string BossHpViewName = "BossHP_UI"; // 태그 없을 때 2순위
    private const float ResolveTimeout = 2f;           // 최대 대기 시간(초)
    private const int ResolveTriesPerFrame = 1;        // 프레임당 시도 횟수

    // ───────── 전투/연출 옵션 ─────────
    public bool stationary = true;
    public bool flashOnHit = true;
    public bool spawnHitFX = true;

    public GameObject damageTextPrefab;
    public GameObject cDamageTextPrefab;
    public GameObject hitEffectPrefab;

    private static IBossHPView s_cachedView; // 한 번 찾으면 모든 보스가 공유
    private IBossHPView hpView;
    private bool isDead;

    public float currentHP;
    private float maxHP;
    private float criticalChance;

    private SpriteRenderer spriteRenderer;
    private BulletSpawner bulletSpawner;
    private Rigidbody2D rb;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        bulletSpawner = FindFirstObjectByType<BulletSpawner>();
        rb = GetComponent<Rigidbody2D>();

        if (stationary && rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Static;
        }
    }

    private void Start()
    {
        maxHP = GameManager.Instance.middleBoss1Stats.maxHP;
        currentHP = maxHP;
        criticalChance = GameManager.Instance.playerStats.criticalChance;

        StartCoroutine(EnsureBindHpViewAndInit());
    }

    private IEnumerator EnsureBindHpViewAndInit()
    {
        // 이미 캐시돼 있으면 바로 사용
        hpView = s_cachedView;
        if (hpView == null)
        {
            float end = Time.realtimeSinceStartup + ResolveTimeout;
            // 몇 프레임 동안 UI 로딩/활성 기다리며 탐색
            while (hpView == null && Time.realtimeSinceStartup < end)
            {
                for (int i = 0; i < ResolveTriesPerFrame && hpView == null; i++)
                    hpView = TryResolveViewOnce();

                if (hpView != null) break;
                yield return null; // 다음 프레임
            }

            if (hpView != null) s_cachedView = hpView;
        }

        if (hpView != null)
        {
            hpView.Init(maxHP, currentHP);
            hpView.Show();
        }
        else
        {
            Debug.LogError("[MiddleBoss1HP] 씬에서 IBossHPView를 찾지 못했음. HP UI에 구현 컴포넌트 붙여줘.");
        }
    }

    private IBossHPView TryResolveViewOnce()
    {
        // 1) Tag 우선
        var tagged = GameObject.FindWithTag(BossHpViewTag);
        if (tagged)
        {
            var v = GetViewFrom(tagged);
            if (v != null) return v;
        }

        // 2) Name 2순위
        var named = GameObject.Find(BossHpViewName);
        if (named)
        {
            var v = GetViewFrom(named);
            if (v != null) return v;
        }

        // 3) 씬 전역 첫 번째
        var all = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var m in all)
        {
            if (m is IBossHPView v) return v;
        }

        return null;
    }

    private IBossHPView GetViewFrom(GameObject go)
    {
        if (!go) return null;

        // 자기 자신
        var self = go.GetComponents<MonoBehaviour>();
        for (int i = 0; i < self.Length; i++)
            if (self[i] is IBossHPView v1) return v1;

        // 자식
        var children = go.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < children.Length; i++)
            if (children[i] is IBossHPView v2) return v2;

        // 부모
        var p = go.transform.parent;
        while (p != null)
        {
            var parents = p.GetComponents<MonoBehaviour>();
            for (int i = 0; i < parents.Length; i++)
                if (parents[i] is IBossHPView v3) return v3;
            p = p.parent;
        }
        return null;
    }

    // ───────── 외부 호출 ─────────
    public void OnBossActivated()
    {
        if (hpView == null) return;
        hpView.Init(maxHP, currentHP);
        hpView.Show();
    }

    // ───────── 데미지 처리 ─────────
    public void TakeDamage()
    {
        if (isDead) return;

        bool crit = Random.Range(0f, 100f) < criticalChance;
        int damage = crit
            ? Mathf.RoundToInt(GameManager.Instance.playerStats.attack * 2f)
            : Mathf.RoundToInt(GameManager.Instance.playerStats.attack);

        ApplyDamage(damage, crit);
    }

    public void FireballTakeDamage(int damage) => ApplyDamage(damage, false);
    public void SkillTakeDamage(int damage) => ApplyDamage(damage, false);

    private void ApplyDamage(int damage, bool crit)
    {
        if (isDead || damage <= 0) return;

        currentHP = Mathf.Clamp(currentHP - damage, 0, maxHP);
        hpView?.SetHP(currentHP, maxHP);

        if (!bulletSpawner || !bulletSpawner.slowSkillActive)
        {
            if (flashOnHit && spriteRenderer)
            {
                spriteRenderer.DOComplete();
                DOTween.Sequence()
                    .Append(spriteRenderer.DOColor(Color.red, 0.06f))
                    .Append(spriteRenderer.DOColor(Color.white, 0.08f));
            }
            if (spawnHitFX) PlayHitEffect();
        }

        if (crit)
        {
            GameManager.Instance.audioManager.PlayArrowHitSound(1.5f);
            ShowDamageText(damage, true);
            GameManager.Instance.cameraShake.GenerateImpulse();
        }
        else
        {
            GameManager.Instance.audioManager.PlayArrowHitSound(1.5f);
            ShowDamageText(damage, false);
        }

        if (currentHP <= 0) Die();
    }

    private void PlayHitEffect()
    {
        if (!hitEffectPrefab) return;
        var fx = PoolManager.Instance.SpawnFromPool(hitEffectPrefab.name, transform.position, Quaternion.identity);
        if (!fx) return;
        DOVirtual.DelayedCall(0.3f, () => PoolManager.Instance.ReturnToPool(fx));
    }

    private void ShowDamageText(int damage, bool critical)
    {
        var prefab = critical ? cDamageTextPrefab : damageTextPrefab;
        if (!prefab) return;

        var obj = PoolManager.Instance.SpawnFromPool(prefab.name, transform.position, Quaternion.identity);
        if (!obj) return;

        var txt = obj.GetComponent<TMPro.TMP_Text>();
        if (txt) txt.text = damage.ToString();

        var t = obj.transform;
        t.DOMoveY(t.position.y + 0.5f, 0.5f).SetEase(Ease.OutCubic);
        t.DOScale(1.2f, 0.2f).OnComplete(() => t.DOScale(1f, 0.25f));
        DOVirtual.DelayedCall(0.6f, () => PoolManager.Instance.ReturnToPool(obj));
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        hpView?.Hide();
        GameManager.Instance.cameraShake.GenerateImpulse();

        var heal = FindFirstObjectByType<PlayerHeal>();
        if (heal != null && heal.hpHeal)
        {
            GameManager.Instance.playerStats.currentHP += heal.hpHealAmount;
            GameManager.Instance.playerStats.currentHP =
                Mathf.Clamp(GameManager.Instance.playerStats.currentHP, 0, GameManager.Instance.playerStats.maxHP);
        }

        GetComponent<MiddleBoss>()?.SetDead();
        GetComponent<EnemiesDie>()?.Die();
    }
}
