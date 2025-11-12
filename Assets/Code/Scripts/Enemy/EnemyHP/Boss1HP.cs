using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Reflection;

[DisallowMultipleComponent]
public class Boss1HP : MonoBehaviour
{
    private const string BossHpViewTag = "LastBossHPView"; // 있으면 최우선
    private const string BossHpViewName = "BossHP_UI";  // 2순위
    private const float ResolveTimeout = 2f;           // UI 로딩 대기 최대 2초

    // 인터페이스 없이 덕 타이핑(리플렉션)으로 바인딩
    private struct ViewInvoker
    {
        public Component target;
        public Action<float, float> Init;   // (max, current)
        public Action<float, float> SetHP;  // (current, max)
        public Action Show;
        public Action Hide;
        public bool IsValid => target && Init != null && SetHP != null && Show != null && Hide != null;
    }

    private static ViewInvoker s_cachedView;
    private ViewInvoker hpView;

    [Header("스탯/전투")]
    public float currentHP;
    private float maxHP;
    private float criticalChance;
    private bool isDead = false;

    [Header("넉백 옵션")]
    public bool useKnockback = true;
    public float knockbackDistance = 0.3f;
    public float knockbackDuration = 0.1f;

    [Header("데미지 텍스트(풀 명)")]
    public GameObject damageTextPrefab;
    public GameObject cDamageTextPrefab;

    [Header("히트 이펙트(풀 명)")]
    public GameObject hitEffectPrefab;

    private Transform playerTransform;
    private SpriteRenderer spriteRenderer;
    private BulletSpawner bulletSpawner;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        bulletSpawner = FindFirstObjectByType<BulletSpawner>();

        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;
    }

    private void Start()
    {
        maxHP = GameManager.Instance.boss1Stats.maxHP;
        currentHP = maxHP;
        criticalChance = GameManager.Instance.playerStats.criticalChance;

        StartCoroutine(EnsureBindHpViewAndInit());
    }

    private IEnumerator EnsureBindHpViewAndInit()
    {
        hpView = s_cachedView;
        if (!hpView.IsValid)
        {
            float end = Time.realtimeSinceStartup + ResolveTimeout;
            while (!hpView.IsValid && Time.realtimeSinceStartup < end)
            {
                hpView = TryResolveViewOnce();
                if (hpView.IsValid) { s_cachedView = hpView; break; }
                yield return null;
            }
        }

        if (hpView.IsValid)
        {
            // 위치 인자 호출 (named argument 사용 금지)
            hpView.Init(maxHP, currentHP);
            hpView.Show();
        }
        else
        {
            Debug.LogError("[Boss1HP] 씬에서 Boss HP UI(Init/SetHP/Show/Hide 포함 컴포넌트)를 찾지 못했습니다. Tag=BossHPView 또는 이름 BossHP_UI 권장.");
        }
    }

    private ViewInvoker TryResolveViewOnce()
    {
        // 1) Tag 우선
        var tagged = GameObject.FindWithTag(BossHpViewTag);
        var v = GetViewFrom(tagged);
        if (v.IsValid) return v;

        // 2) 이름
        var named = GameObject.Find(BossHpViewName);
        v = GetViewFrom(named);
        if (v.IsValid) return v;

        // 3) 씬 전역
        var all = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            v = TryBuildInvoker(all[i]);
            if (v.IsValid) return v;
        }

        return default;
    }

    private ViewInvoker GetViewFrom(GameObject go)
    {
        if (go == null) return default;

        // 자신
        var self = go.GetComponents<MonoBehaviour>();
        for (int i = 0; i < self.Length; i++)
        {
            var v = TryBuildInvoker(self[i]);
            if (v.IsValid) return v;
        }
        // 자식
        var children = go.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < children.Length; i++)
        {
            var v = TryBuildInvoker(children[i]);
            if (v.IsValid) return v;
        }
        // 부모
        var p = go.transform.parent;
        while (p != null)
        {
            var parents = p.GetComponents<MonoBehaviour>();
            for (int i = 0; i < parents.Length; i++)
            {
                var v = TryBuildInvoker(parents[i]);
                if (v.IsValid) return v;
            }
            p = p.parent;
        }
        return default;
    }

    private ViewInvoker TryBuildInvoker(Component comp)
    {
        if (comp == null) return default;

        var t = comp.GetType();

        MethodInfo mInit = t.GetMethod("Init", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        MethodInfo mSetHP = t.GetMethod("SetHP", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        MethodInfo mShow = t.GetMethod("Show", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        MethodInfo mHide = t.GetMethod("Hide", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (mInit == null || mSetHP == null || mShow == null || mHide == null) return default;

        if (!CheckSig(mInit, typeof(float), typeof(float))) return default;
        if (!CheckSig(mSetHP, typeof(float), typeof(float))) return default;
        if (!CheckSig(mShow)) return default;
        if (!CheckSig(mHide)) return default;

        var inv = new ViewInvoker { target = comp };
        inv.Init = (a, b) => mInit.Invoke(comp, new object[] { a, b });   // (max, current)
        inv.SetHP = (a, b) => mSetHP.Invoke(comp, new object[] { a, b });   // (current, max)
        inv.Show = () => mShow.Invoke(comp, null);
        inv.Hide = () => mHide.Invoke(comp, null);

        return inv;
    }

    private bool CheckSig(MethodInfo m, params Type[] paramTypes)
    {
        if (m.ReturnType != typeof(void)) return false;
        var ps = m.GetParameters();
        if (ps.Length != paramTypes.Length) return false;
        for (int i = 0; i < ps.Length; i++)
            if (ps[i].ParameterType != paramTypes[i]) return false;
        return true;
    }

    // ====== 데미지 처리 ======
    public void TakeDamage()
    {
        if (isDead) return;

        bool isCritical = UnityEngine.Random.Range(0f, 100f) < criticalChance; // ← 모호성 제거
        int damage = isCritical
            ? Mathf.RoundToInt(GameManager.Instance.playerStats.attack * 2f)
            : Mathf.RoundToInt(GameManager.Instance.playerStats.attack);

        ApplyDamage(damage, isCritical);

        if (useKnockback && playerTransform != null)
        {
            var dir = (transform.position - playerTransform.position).normalized;
            transform.DOMove(transform.position + dir * knockbackDistance, knockbackDuration)
                     .SetEase(Ease.OutQuad);
        }
    }

    public void FireballTakeDamage(int damage) => ApplyDamage(damage, false);
    public void SkillTakeDamage(int damage) => ApplyDamage(damage, false);

    private void ApplyDamage(int damage, bool isCritical)
    {
        if (isDead || damage <= 0) return;

        currentHP = Mathf.Clamp(currentHP - damage, 0, maxHP);
        if (hpView.IsValid) hpView.SetHP(currentHP, maxHP);

        if (bulletSpawner == null || !bulletSpawner.slowSkillActive)
        {
            PlayDamageFlash();
            PlayHitEffect();
        }

        if (isCritical)
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

        var fireBoss = GetComponent<FireBoss>();
        if (fireBoss != null) fireBoss.OnBossTakeDamage();

        if (currentHP <= 0) Die();
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        if (hpView.IsValid) hpView.Hide();

        GameManager.Instance.cameraShake.GenerateImpulse();

        var playerHeal = FindFirstObjectByType<PlayerHeal>();
        if (playerHeal != null && playerHeal.hpHeal)
        {
            GameManager.Instance.playerStats.currentHP += playerHeal.hpHealAmount;
            GameManager.Instance.playerStats.currentHP =
                Mathf.Clamp(GameManager.Instance.playerStats.currentHP, 0, GameManager.Instance.playerStats.maxHP);
        }

        var enemiesDie = GetComponent<EnemiesDie>();
        if (enemiesDie != null) enemiesDie.Die();

        // ✅ 보스 사망 연출이 끝났을 때 GameManager로 알림
        GameManager.Instance.OnBoss1Dead();
    }

    // ====== 이펙트/텍스트 ======
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

        var t = obj.transform;
        var txt = obj.GetComponent<TMP_Text>();
        if (txt != null) txt.text = damage.ToString();

        t.DOMoveY(t.position.y + 0.5f, 0.5f).SetEase(Ease.OutCubic);
        t.DOScale(1.2f, 0.2f).OnComplete(() => t.DOScale(1f, 0.3f));
        DOVirtual.DelayedCall(0.6f, () => PoolManager.Instance.ReturnToPool(obj));
    }

    private void PlayDamageFlash()
    {
        if (!spriteRenderer) return;
        spriteRenderer.DOComplete();
        DOTween.Sequence()
               .Append(spriteRenderer.DOColor(Color.red, 0.1f))
               .Append(spriteRenderer.DOColor(Color.white, 0.1f));
    }
}
