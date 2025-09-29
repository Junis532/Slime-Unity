using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MiddleBoss1HP : MonoBehaviour
{
    [Header("체력 관련")]
    [SerializeField] private GameObject bossHpBarPrefab; // 💡 프리팹으로 연결
    private GameObject bossHpBarUI; // 💡 런타임에 생성될 오브젝트

    [Tooltip("HP 바 컨테이너(바의 프레임) 경로 (프리팹 기준)")]
    [SerializeField] private string hpBarRootPath = "HPBar";
    [Tooltip("채워지는 이미지 경로 (프리팹 기준)")]
    [SerializeField] private string hpFillPath = "HPBar/HPFilled";

    private Image hpBarFill;
    private RectTransform hpBarRoot; // 바 프레임(컨테이너)

    public float currentHP;
    private float maxHP;

    private BulletSpawner bulletSpawner;

    [Header("데미지 텍스트")]
    public GameObject damageTextPrefab;
    public GameObject cDamageTextPrefab;

    [Header("이펙트 프리팹")]
    public GameObject hitEffectPrefab;

    [Header("넉백 설정")]
    public bool enableKnockback = true;

    private Transform playerTransform;
    private SpriteRenderer spriteRenderer;
    private float criticalChance;
    private bool isDead = false;

    [Header("보스 HP바 등장 연출")]
    [Tooltip("보스가 활성화되면 자동으로 연출 실행")]
    [SerializeField] private bool playIntroOnSpawn = true;

    [Tooltip("가로로 쭉 늘어나는 스케일 연출 사용")]
    [SerializeField] private bool useScaleGrow = true;

    [Tooltip("sizeDelta.x를 0→원래 너비로 늘리는 연출(스케일 대신)")]
    [SerializeField] private bool useWidthGrow = false;

    [Tooltip("fillAmount 0 → 현재 체력비로 와이프 연출 사용")]
    [SerializeField] private bool useFillWipe = true;

    [Tooltip("연출 딜레이")]
    [SerializeField] private float introDelay = 0.1f;

    [Tooltip("연출 총 시간(스케일/필/너비 공통)")]
    [SerializeField] private float introDuration = 0.6f;

    [Tooltip("스케일 연출에 약간의 바운스 적용")]
    [SerializeField] private bool scaleBounce = true;

    [Tooltip("HP바를 보스 머리 위로 띄울 높이")]
    [SerializeField] private float hpBarHeightOffset = 2f;

    private Sequence hpIntroSeq;

    // ✅ 원래 값 캐싱 (쪼그라듦 방지)
    private Vector3 hpBarOrigScale = Vector3.one;
    private Vector2 hpBarOrigSize;

    void Start()
    {
        maxHP = GameManager.Instance.middleBoss1Stats.maxHP;
        currentHP = maxHP;
        criticalChance = GameManager.Instance.playerStats.criticalChance;

        // 💡 BossHP 프리팹 생성
        if (bossHpBarPrefab != null)
        {
            bossHpBarUI = Instantiate(bossHpBarPrefab);
            bossHpBarUI.SetActive(true);

            // 경로로 찾기
            var hpBarRootTr = bossHpBarUI.transform.Find(hpBarRootPath);
            if (hpBarRootTr != null) hpBarRoot = hpBarRootTr as RectTransform;

            var hpFillTr = bossHpBarUI.transform.Find(hpFillPath);
            if (hpFillTr != null) hpBarFill = hpFillTr.GetComponent<Image>();

            if (hpBarRoot == null)
                Debug.LogError($"'{hpBarRootPath}' RectTransform을(를) 찾을 수 없습니다. 경로를 확인하세요.");
            if (hpBarFill == null)
                Debug.LogError($"'{hpFillPath}' Image 컴포넌트를 찾을 수 없습니다. 경로를 확인하세요.");

            // ✅ 원래 스케일/사이즈 캐싱
            if (hpBarRoot != null)
            {
                hpBarOrigScale = hpBarRoot.localScale;
                hpBarOrigSize = hpBarRoot.sizeDelta;
            }
        }
        else
        {
            Debug.LogError("Boss HP Bar Prefab이 연결되지 않았습니다!");
        }

        spriteRenderer = GetComponent<SpriteRenderer>();
        bulletSpawner = FindFirstObjectByType<BulletSpawner>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            playerTransform = playerObj.transform;

        // 초기 HP UI 상태 셋업
        SetupHPBarInitialVisual();

        // 자동 인트로 연출
        if (playIntroOnSpawn)
            PlayHPBarIntro();
        else
            UpdateHPBarImmediate(); // 바로 실제 값 표시
    }

    private void Update()
    {
        // 보스가 존재하면 HP바 위치를 보스 위쪽으로 따라가도록
        if (bossHpBarUI != null && !isDead)
        {
            bossHpBarUI.transform.position = transform.position + Vector3.up * hpBarHeightOffset;
        }
    }

    // ====== HP 바 인트로 연출 ======
    private void SetupHPBarInitialVisual()
    {
        // 둘이 동시에 켜졌다면 스케일을 우선(충돌 방지)
        if (useScaleGrow) useWidthGrow = false;

        // 스케일 연출용: 시작 스케일 x=0 (원래 y/z 유지)
        if (useScaleGrow && hpBarRoot != null)
        {
            hpBarRoot.localScale = new Vector3(0f, hpBarOrigScale.y, hpBarOrigScale.z);
        }

        // 너비 연출용: 시작 너비 0
        if (useWidthGrow && hpBarRoot != null)
        {
            hpBarRoot.sizeDelta = new Vector2(0f, hpBarOrigSize.y);
        }

        // 필 와이프 연출용: fillAmount = 0 시작
        if (useFillWipe && hpBarFill != null)
        {
            hpBarFill.fillAmount = 0f;
        }

        // (인트로 연출을 쓰지 않을 때) 즉시 반영
        if (!useScaleGrow && !useWidthGrow && !useFillWipe)
        {
            UpdateHPBarImmediate();
        }
    }

    private void PlayHPBarIntro()
    {
        // 기존 시퀀스 정리
        if (hpIntroSeq != null && hpIntroSeq.IsActive())
            hpIntroSeq.Kill();

        hpIntroSeq = DOTween.Sequence().SetUpdate(false); // 타임스케일 영향 받기 원하면 SetUpdate(true)

        // 딜레이
        if (introDelay > 0f) hpIntroSeq.AppendInterval(introDelay);

        // 스케일 연출
        if (useScaleGrow && hpBarRoot != null)
        {
            var ease = scaleBounce ? Ease.OutBack : Ease.OutCubic;
            hpIntroSeq.Join(
                hpBarRoot.DOScaleX(hpBarOrigScale.x, introDuration).SetEase(ease) // ✅ 원래 X 스케일까지
            );
        }

        // 너비 연출
        if (useWidthGrow && hpBarRoot != null)
        {
            hpIntroSeq.Join(
                hpBarRoot.DOSizeDelta(new Vector2(hpBarOrigSize.x, hpBarOrigSize.y), introDuration).SetEase(Ease.OutCubic)
            );
        }

        // 필 와이프 연출
        if (useFillWipe && hpBarFill != null)
        {
            float targetFill = Mathf.Approximately(maxHP, 0f) ? 1f : (currentHP / maxHP);
            hpIntroSeq.Join(
                DOTween.To(() => hpBarFill.fillAmount, x => hpBarFill.fillAmount = x, targetFill, introDuration)
                       .SetEase(Ease.OutCubic)
            );
        }

        // 아무 것도 안 쓰면 안전망
        if (!useScaleGrow && !useWidthGrow && !useFillWipe)
        {
            hpIntroSeq.AppendCallback(UpdateHPBarImmediate);
        }
    }

    private void UpdateHPBarImmediate()
    {
        if (hpBarFill != null)
            hpBarFill.fillAmount = (maxHP <= 0f) ? 1f : (currentHP / maxHP);

        if (hpBarRoot != null)
        {
            // ✅ 원래 값으로 정확히 회복
            hpBarRoot.localScale = hpBarOrigScale;
            hpBarRoot.sizeDelta = hpBarOrigSize;
        }
    }

    private void UpdateHPBar()
    {
        // 인트로 와이프 중일 수 있으니, 진행 중이면 현재 fillAmount를 강제 덮지 않음.
        if (hpBarFill != null && (hpIntroSeq == null || !hpIntroSeq.IsActive()))
            hpBarFill.fillAmount = (maxHP <= 0f) ? 1f : (currentHP / maxHP);
    }

    // 외부에서 “보스가 이제 활성화됨” 타이밍에 호출하고 싶으면 사용
    public void OnBossActivated()
    {
        SetupHPBarInitialVisual();
        PlayHPBarIntro();
    }

    public void TakeDamage()
    {
        bool isCritical = Random.Range(0f, 100f) < criticalChance;
        int damage = isCritical
            ? Mathf.RoundToInt(GameManager.Instance.playerStats.attack * 2f)
            : Mathf.RoundToInt(GameManager.Instance.playerStats.attack);

        ApplyDamage(damage, isCritical);

        if (enableKnockback && playerTransform != null)
        {
            Vector3 knockbackDir = (transform.position - playerTransform.position).normalized;
            float knockbackDistance = 0.3f;
            float knockbackDuration = 0.1f;

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

        // 인트로 와이프가 진행 중일 때 바로 현재값으로 맞추고 싶으면 아래 주석 해제
        // if (hpIntroSeq != null && hpIntroSeq.IsActive()) hpIntroSeq.Kill();

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

        MiddleBoss middleBoss = GetComponent<MiddleBoss>();
        if (middleBoss != null)
            middleBoss.SetDead();

        // 💡 보스 HP바 숨기기
        if (bossHpBarUI != null)
            bossHpBarUI.SetActive(false);

        GameManager.Instance.cameraShake.GenerateImpulse();

        PlayerHeal playerHeal = FindFirstObjectByType<PlayerHeal>();
        if (playerHeal != null && playerHeal.hpHeal)
        {
            GameManager.Instance.playerStats.currentHP += playerHeal.hpHealAmount;
            GameManager.Instance.playerStats.currentHP =
                Mathf.Clamp(GameManager.Instance.playerStats.currentHP, 0, GameManager.Instance.playerStats.maxHP);
        }

        EnemiesDie enemiesDie = GetComponent<EnemiesDie>();
        if (enemiesDie != null) enemiesDie.Die();
    }

    private void OnDestroy()
    {
        if (hpIntroSeq != null && hpIntroSeq.IsActive())
            hpIntroSeq.Kill();
    }
}
