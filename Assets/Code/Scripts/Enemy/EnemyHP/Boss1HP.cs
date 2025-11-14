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
    private const string BossHpViewTag = "LastBossHPView"; // 우선순위 1
    private const string BossHpViewName = "BossHP_UI";     // 우선순위 2
    private const float ResolveTimeout = 2f;               // UI 로딩 대기 최대 2초

    // ====== UI 덕 타이핑 구조체 ======
    private struct ViewInvoker
    {
        public Component target;
        public Action<float, float> Init;   // (max, current)
        public Action<float, float> SetHP;  // (current, max)
        public Action Show;
        public Action Hide;

        public bool IsValid =>
            target != null &&
            Init != null &&
            SetHP != null &&
            Show != null &&
            Hide != null;
    }

    // ====== 정적 캐싱 (씬 전환 후 자동 무효화 포함) ======
    private static ViewInvoker s_cachedView;
    private static int s_cachedSceneHash;

    private ViewInvoker hpView;

    // ====== 보스 스탯 ======
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
        if (playerObj) playerTransform = playerObj.transform;
    }

    private void Start()
    {
        maxHP = GameManager.Instance.boss1Stats.maxHP;
        currentHP = maxHP;
        criticalChance = GameManager.Instance.playerStats.criticalChance;

        StartCoroutine(EnsureBindHpViewAndInit());
    }

    // =================================================================
    // === UI 바인딩 (죽은 참조 방지 + 씬 전환 자동 재탐색 + 캐싱) ===
    // =================================================================
    private IEnumerator EnsureBindHpViewAndInit()
    {
        // 씬이 바뀌었으면 캐싱 무효화
        if (s_cachedSceneHash != gameObject.scene.GetHashCode())
        {
            s_cachedSceneHash = gameObject.scene.GetHashCode();
            s_cachedView = default;
        }

        hpView = s_cachedView;

        // 기존 캐싱이 죽었는지 확인
        if (hpView.IsValid && hpView.target == null)
        {
            s_cachedView = default;
            hpView = default;
        }

        // 유효하지 않으면 새로 탐색
        if (!hpView.IsValid)
        {
            float end = Time.realtimeSinceStartup + ResolveTimeout;
            while (!hpView.IsValid && Time.realtimeSinceStartup < end)
            {
                hpView = TryResolveViewOnce();
                if (hpView.IsValid)
                {
                    s_cachedView = hpView; // 캐싱
                    break;
                }
                yield return null;
            }
        }

        if (hpView.IsValid)
        {
            hpView.Init(maxHP, currentHP);
            hpView.Show();
        }
        else
        {
            Debug.LogError("[Boss1HP] Boss HP UI를 찾지 못했습니다. Tag='LastBossHPView' 또는 이름='BossHP_UI' 객체를 확인하세요.");
        }
    }

    private ViewInvoker TryResolveViewOnce()
    {
        // 1) Tag
        var go = GameObject.FindWithTag(BossHpViewTag);
        var v = GetViewFrom(go);
        if (v.IsValid) return v;

        // 2) 이름
        go = GameObject.Find(BossHpViewName);
        v = GetViewFrom(go);
        if (v.IsValid) return v;

        // 3) 씬 전체
        var comps = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in comps)
        {
            v = TryBuildInvoker(c);
            if (v.IsValid) return v;
        }

        return default;
    }

    private ViewInvoker GetViewFrom(GameObject root)
    {
        if (!root) return default;

        // 자신
        foreach (var c in root.GetComponents<MonoBehaviour>())
        {
            var v = TryBuildInvoker(c);
            if (v.IsValid) return v;
        }

        // 자식
        foreach (var c in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            var v = TryBuildInvoker(c);
            if (v.IsValid) return v;
        }

        // 부모
        var p = root.transform.parent;
        while (p)
        {
            foreach (var c in p.GetComponents<MonoBehaviour>())
            {
                var v = TryBuildInvoker(c);
                if (v.IsValid) return v;
            }
            p = p.parent;
        }

        return default;
    }

    private ViewInvoker TryBuildInvoker(Component comp)
    {
        if (!comp) return default;

        var t = comp.GetType();

        MethodInfo mInit = t.GetMethod("Init", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        MethodInfo mSetHP = t.GetMethod("SetHP", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        MethodInfo mShow = t.GetMethod("Show", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        MethodInfo mHide = t.GetMethod("Hide", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        if (mInit == null || mSetHP == null || mShow == null || mHide == null) return default;
        if (!CheckSig(mInit, typeof(float), typeof(float))) return default;
        if (!CheckSig(mSetHP, typeof(float), typeof(float))) return default;
        if (!CheckSig(mShow)) return default;
        if (!CheckSig(mHide)) return default;

        return new ViewInvoker
        {
            target = comp,
            Init = (max, cur) => mInit.Invoke(comp, new object[] { max, cur }),
            SetHP = (cur, max) => mSetHP.Invoke(comp, new object[] { cur, max }),
            Show = () => mShow.Invoke(comp, null),
            Hide = () => mHide.Invoke(comp, null)
        };
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

    // =================================================================
    // ======   데미지 처리   =================================================
    // =================================================================

    public void TakeDamage()
    {
        if (isDead) return;

        bool isCritical = UnityEngine.Random.Range(0f, 100f) < criticalChance;
        int damage = isCritical
            ? Mathf.RoundToInt(GameManager.Instance.playerStats.attack * 2f)
            : Mathf.RoundToInt(GameManager.Instance.playerStats.attack);

        ApplyDamage(damage, isCritical);

        if (useKnockback && playerTransform)
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

        if (hpView.IsValid)
            hpView.SetHP(currentHP, maxHP);

        if (!bulletSpawner || !bulletSpawner.slowSkillActive)
        {
            PlayDamageFlash();
            PlayHitEffect();
        }

        if (isCritical)
        {
            GameManager.Instance.audioManager.PlayArrowHitSound(1.5f);
            GameManager.Instance.cameraShake.GenerateImpulse();
            ShowDamageText(damage, true);
        }
        else
        {
            GameManager.Instance.audioManager.PlayArrowHitSound(1.5f);
            ShowDamageText(damage, false);
        }

        var fireBoss = GetComponent<FireBoss>();
        if (fireBoss != null)
            fireBoss.OnBossTakeDamage();

        if (currentHP <= 0)
            Die();
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        if (hpView.IsValid)
            hpView.Hide();

        GameManager.Instance.cameraShake.GenerateImpulse();

        var heal = FindFirstObjectByType<PlayerHeal>();
        if (heal && heal.hpHeal)
        {
            GameManager.Instance.playerStats.currentHP += heal.hpHealAmount;
            GameManager.Instance.playerStats.currentHP =
                Mathf.Clamp(GameManager.Instance.playerStats.currentHP, 0, GameManager.Instance.playerStats.maxHP);
        }

        var die = GetComponent<EnemiesDie>();
        if (die) die.Die();

        GameManager.Instance.OnBoss1Dead();
    }

    // =================================================================
    // ======   이펙트   ======================================================
    // =================================================================

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

        var txt = obj.GetComponent<TMP_Text>();
        if (txt) txt.text = damage.ToString();

        var t = obj.transform;
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
