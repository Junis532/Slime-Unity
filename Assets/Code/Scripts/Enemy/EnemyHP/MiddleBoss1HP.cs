using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
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
    private const string BossHpViewTag = "LastBossHPView";
    private const string BossHpViewName = "BossHP_UI";

    private const float ResolveTimeout = 2f;
    private const int ResolveTriesPerFrame = 1;

    // ───────── 옵션 ─────────
    public bool stationary = true;
    public bool flashOnHit = true;
    public bool spawnHitFX = true;

    public GameObject damageTextPrefab;
    public GameObject cDamageTextPrefab;
    public GameObject hitEffectPrefab;

    private static IBossHPView s_cachedView;     // 🔥 반드시 살펴보고 Unity null 확인 필요
    private IBossHPView hpView;
    private bool isDead;

    public float currentHP;
    private float maxHP;
    private float criticalChance;

    private SpriteRenderer spriteRenderer;
    private BulletSpawner bulletSpawner;
    private Rigidbody2D rb;

    private void OnEnable()
    {
        // 씬 재로드 또는 재시작 시 static 참조 자동 리셋
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        // 씬이 완전 바뀔 때마다 캐시 초기화
        s_cachedView = null;
    }

    private bool IsUnityNull(object obj)
    {
        return (obj is Object uObj) && uObj == null;
    }

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
        hpView = s_cachedView;

        // 🔥 static이지만 실제 Unity 객체는 파괴된 상태인지 검사
        if (hpView != null && IsUnityNull(hpView))
        {
            hpView = null;
            s_cachedView = null;
        }

        // 캐시가 없으면 새로 찾기
        if (hpView == null)
        {
            float end = Time.realtimeSinceStartup + ResolveTimeout;

            while (hpView == null && Time.realtimeSinceStartup < end)
            {
                for (int i = 0; i < ResolveTriesPerFrame && hpView == null; i++)
                {
                    hpView = TryResolveViewOnce();
                }

                if (hpView != null)
                {
                    s_cachedView = hpView;
                    break;
                }

                yield return null; // UI 로드 대기
            }
        }

        // 최종 초기화
        if (hpView != null)
        {
            hpView.Init(maxHP, currentHP);
            hpView.Show();
        }
        else
        {
            Debug.LogError("[MiddleBoss1HP] HP UI를 끝내 찾지 못했습니다. 씬에 IBossHPView 구현체가 필요합니다.");
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

        // 2) 이름
        var named = GameObject.Find(BossHpViewName);
        if (named)
        {
            var v = GetViewFrom(named);
            if (v != null) return v;
        }

        // 3) 최후의 수단: 씬 전체 검색
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
        foreach (var mb in self)
            if (mb is IBossHPView v1) return v1;

        // 자식
        var children = go.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var mb in children)
            if (mb is IBossHPView v2) return v2;

        // 부모 체인
        var p = go.transform.parent;
        while (p != null)
        {
            var parents = p.GetComponents<MonoBehaviour>();
            foreach (var mb in parents)
                if (mb is IBossHPView v3) return v3;

            p = p.parent;
        }

        return null;
    }

    // ───────── 외부 호출 ─────────
    public void OnBossActivated()
    {
        if (hpView == null || IsUnityNull(hpView)) return;

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

        // 타격 연출
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

        // 데미지 텍스트 + 사운드
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
