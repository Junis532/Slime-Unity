using UnityEngine;
using System.Collections;
using DG.Tweening;
// 충돌 방지 별칭
using UPRandom = UnityEngine.Random;
using UDebug = UnityEngine.Debug;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(EnemyAnimation))]
public class TurretEnemy_FixedAngle : MonoBehaviour
{
    [Header("🎯 애니메이션 (EnemyAnimation 사용)")]
    public EnemyAnimation enemyAnim;

    private bool isLive = true;
    private SpriteRenderer spriter;

    [Header("발사 범위 / 라인 표시")]
    public float fireRange = 5f;

    [Header("첫 발사 딜레이")]
    public float firstFireDelay = 0f;

    [Header("발사 전 예열 기본 시간")]
    public float preWindUp = 0.22f;

    [Header("발사 전 예열 확장 옵션")]
    public float extraPreWindUp = 0.25f;   // 예열 늘리기
    public int ringPulseCount = 2;         // 예열 펄스 횟수
    public float ringPulseScale = 1.08f;   // 펄스 배율

    [Header("Bullet 설정")]
    public GameObject bulletPrefab;
    public GameObject secondaryBulletPrefab;
    public float bulletSpeed = 1.5f;
    public float bulletLifetime = 3f;

    [Header("두 번째 Bullet 속도 변경")]
    public float secondaryDelay = 1f;
    public float secondarySpeed = 2f;

    [Header("메인 라인표시(LineRenderer)")]
    public bool showLineRenderer = true;
    private LineRenderer lineRenderer;

    [Header("고정 발사 각도 (도 단위)")]
    [Range(0f, 360f)]
    public float fixedAngle = 0f;

    [Header("Phase 스케줄")]
    public float cycleLength = 1.2f;
    public float[] firePhases = { 0f };

    private int phaseIdx = 0;
    private double cycleBase;
    private double nextFireAt;
    private bool isPrepping = false;
    private bool isShooting = false;

    private const float VerticalTolerance = 25f;

    // ───────────── 속도/타이밍 글로벌 스케일 ─────────────
    [Header("⏱ FX 속도 조절")]
    [Tooltip("1보다 크면 느려지고, 작으면 빨라짐. 예: 1.3 = 30% 느리게")]
    public float fxSpeedMultiplier = 1.25f;

    // ───────────── 예열 색/스케일 연출 ─────────────
    [Header("예열 색/스케일 연출")]
    public bool usePreScale = true;
    [Range(0.6f, 1f)] public float preScale = 0.86f;
    public float shootPopScale = 1.12f;
    public float shootPopDuration = 0.15f;
    public float settleDuration = 0.12f;
    public bool flashWhiteOnShoot = true;
    public float flashDuration = 0.05f;

    private Vector3 baseScale;
    private Sequence scaleSeq;
    private Sequence popSeq;

    // ───────────── HDR 글로우 / 알파 제어 ─────────────
    [Header("✨ 글로우 / 알파")]
    public bool useHDRGlow = true;
    [Range(1f, 4f)] public float glowIntensity = 2f;

    // ───────────── Pre-Fire FX (충전 링: 월드 반경 고정) ─────────────
    [Header("🔴 충전 링(월드 반경 고정)")]
    public bool useChargeRingFX = true;

    [Tooltip("끝 반경(월드 유닛). 숫자 그대로 화면 크기 결정")]
    public float ringAbsoluteEndRadius = 0.22f;
    [Tooltip("시작 반경(월드 유닛)")]
    public float ringAbsoluteStartRadius = 0.12f;

    [Tooltip("상한(월드 유닛)")]
    public float ringMaxRadius = 0.4f;

    public float ringWidthCore = 0.06f; // 코어
    public float ringWidthHalo = 0.10f; // 헤일로
    [Range(8, 96)] public int ringSegments = 48;
    public Color ringColor = new Color(1f, 0.25f, 0.25f, 1f);
    [Range(0f, 1f)] public float ringAlphaStart = 0.25f;
    [Range(0f, 1f)] public float ringAlphaEnd = 0.9f;

    // ───────────── 발사 쇼크웨이브 (그대로) ─────────────
    [Header("💥 발사 쇼크웨이브 FX")]
    public bool useShockwaveFX = true;
    public bool shockwaveAutoSize = true;
    public float shockwaveBodyMultiplier = 0.8f;
    public float shockwaveMaxRadius = 1.1f;
    public float shockwaveRadiusEnd = 1.1f;
    public float shockwaveRadiusStart = 0.22f;
    public float shockwaveWidth = 0.10f;
    public float shockwaveDuration = 0.25f;
    [Range(12, 96)] public int shockwaveSegments = 56;
    public Color shockwaveColor = new Color(1f, 0.95f, 0.9f, 1f);
    [Range(0f, 1f)] public float shockwaveAlphaStart = 0.9f;
    [Range(0f, 1f)] public float shockwaveAlphaEnd = 0.0f;

    // 내부용: 충전 링
    private GameObject ringRoot;
    private LineRenderer ringCoreLR;
    private LineRenderer ringHaloLR;
    private Sequence ringSeq;
    private float ringCurrentRadius;

    void Awake()
    {
        spriter = GetComponent<SpriteRenderer>();
        if (!enemyAnim) enemyAnim = GetComponent<EnemyAnimation>();
        if (!enemyAnim) UDebug.LogError("EnemyAnimation을 지정하세요.");

        baseScale = transform.localScale;

        // 메인 조준 라인
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.enabled = showLineRenderer;
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.red;
        lineRenderer.endColor = Color.red;
        lineRenderer.sortingOrder = 2;
        lineRenderer.sortingLayerName = "Default";

        if (useChargeRingFX)
            EnsureChargeRing();
    }

    void Start()
    {
        if (firePhases == null || firePhases.Length == 0)
            firePhases = new float[] { 0f };
        System.Array.Sort(firePhases);

        cycleBase = Time.timeAsDouble + firstFireDelay;
        phaseIdx = 0;
        nextFireAt = cycleBase + firePhases[phaseIdx];

        StartCoroutine(PhaseScheduleLoop());
    }

    void Update()
    {
        if (!isLive) return;

        float rad = fixedAngle * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;

        if (showLineRenderer && lineRenderer != null)
        {
            lineRenderer.enabled = true;
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, (Vector2)transform.position + dir * fireRange);
        }

        if (enemyAnim != null && !isPrepping && !isShooting)
            enemyAnim.PlayAnimation(EnemyAnimation.State.Idle);

        // 링은 월드 좌표 반경으로 그리므로, 위치만 따라가게 함
        if (ringRoot) ringRoot.transform.position = transform.position;
    }

    private IEnumerator PhaseScheduleLoop()
    {
        while (isLive)
        {
            float rad = fixedAngle * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;

            float totalPre = Mathf.Max(0.01f, (preWindUp + extraPreWindUp) * fxSpeedMultiplier);
            double prepStart = nextFireAt - totalPre;
            double now = Time.timeAsDouble;

            if (prepStart > now)
                yield return new WaitForSeconds((float)(prepStart - now));

            // ===== 발사 준비 =====
            isPrepping = true;

            enemyAnim?.PlayDirectionalMoveAnimation(dir);
            enemyAnim?.PlayAnimation(EnemyAnimation.State.AttackStart);

            KillAllSequences();
            if (spriter != null)
            {
                spriter.DOKill();
                spriter.color = Color.white;
            }

            // 붉어짐 + 스케일 축소
            scaleSeq = DOTween.Sequence();
            if (spriter != null)
                scaleSeq.Join(spriter.DOColor(Color.red, totalPre).SetEase(Ease.InOutSine));
            if (usePreScale)
                scaleSeq.Join(transform.DOScale(baseScale * preScale, totalPre).SetEase(Ease.InOutCubic));
            scaleSeq.Play();

            // 충전 링 시작 (월드 반경)
            if (useChargeRingFX)
                PlayChargeRing(totalPre);

            now = Time.timeAsDouble;
            if (nextFireAt > now)
                yield return new WaitForSeconds((float)(nextFireAt - now));

            // ===== 발사 =====
            isPrepping = false;
            isShooting = true;

            KillAllSequences();

            if (usePreScale)
                transform.localScale = baseScale * preScale;

            if (flashWhiteOnShoot && spriter != null)
            {
                spriter.DOKill();
                spriter.color = Color.white; // 순간 플래시
            }

            // 팝업
            float popUp = shootPopDuration * fxSpeedMultiplier;
            float settle = settleDuration * fxSpeedMultiplier;
            popSeq = DOTween.Sequence()
                .Append(transform.DOScale(baseScale * shootPopScale, popUp).SetEase(Ease.OutBack))
                .Append(transform.DOScale(baseScale, settle).SetEase(Ease.OutQuad));
            popSeq.Play();

            if (useShockwaveFX)
                SpawnShockwave();

            if (useChargeRingFX)
                StopChargeRingImmediate();

            Shoot(dir);

            // 후딜 애니
            bool isFront = IsFrontAngle(fixedAngle);
            var postState = isFront ? EnemyAnimation.State.FrontAttackEnd : EnemyAnimation.State.AttackEnd;

            float postDuration = 0f;
            if (enemyAnim != null)
            {
                enemyAnim.PlayAnimation(postState);
                postDuration = enemyAnim.GetEstimatedDuration(postState);
            }

            if (spriter != null)
                spriter.DOColor(Color.white, 0.1f * fxSpeedMultiplier);

            if (postDuration > 0f)
                yield return new WaitForSeconds(postDuration);

            isShooting = false;

            // 다음 사이클
            phaseIdx++;
            if (phaseIdx >= firePhases.Length)
            {
                phaseIdx = 0;
                cycleBase += cycleLength;
            }
            nextFireAt = cycleBase + firePhases[phaseIdx];

            yield return null;
        }
    }

    void Shoot(Vector2 dir)
    {
        GameObject bulletToShoot = null;
        if (bulletPrefab && secondaryBulletPrefab)
            bulletToShoot = (UPRandom.value < 0.5f) ? bulletPrefab : secondaryBulletPrefab;
        else if (bulletPrefab)
            bulletToShoot = bulletPrefab;
        else if (secondaryBulletPrefab)
            bulletToShoot = secondaryBulletPrefab;
        else
            return;

        GameObject bullet = Instantiate(bulletToShoot, transform.position, Quaternion.identity);
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb)
            rb.linearVelocity = dir.normalized * bulletSpeed;

        if (bulletToShoot == secondaryBulletPrefab && rb != null && secondaryDelay > 0f)
            StartCoroutine(ChangeBulletSpeed(rb, secondaryDelay, secondarySpeed));

        Destroy(bullet, bulletLifetime);
    }

    private IEnumerator ChangeBulletSpeed(Rigidbody2D rb, float delay, float newSpeed)
    {
        yield return new WaitForSeconds(delay);
        if (rb != null)
            rb.linearVelocity = rb.linearVelocity.normalized * newSpeed;
    }

    private bool IsFrontAngle(float ang)
    {
        ang = (ang % 360f + 360f) % 360f;
        return Mathf.Abs(ang - 90f) <= VerticalTolerance ||
               Mathf.Abs(ang - 270f) <= VerticalTolerance;
    }

    public void ResetCycle(double delay = 0.0)
    {
        cycleBase = Time.timeAsDouble + delay;
        phaseIdx = 0;
        nextFireAt = cycleBase + firePhases[phaseIdx];
    }

    private void OnDestroy()
    {
        KillAllSequences();
        StopChargeRingImmediate();
        if (lineRenderer != null) Destroy(lineRenderer);
        if (ringRoot != null) Destroy(ringRoot);
        isLive = false;
    }

    private void OnDisable()
    {
        KillAllSequences();
        StopChargeRingImmediate();
    }

    private void KillAllSequences()
    {
        if (scaleSeq != null && scaleSeq.IsActive()) scaleSeq.Kill();
        if (popSeq != null && popSeq.IsActive()) popSeq.Kill();
        if (ringSeq != null && ringSeq.IsActive()) ringSeq.Kill();
        transform.DOKill();
        spriter?.DOKill();
    }

    // ───────────── 충전 링: 월드 반경으로 직접 그리기 ─────────────
    private void EnsureChargeRing()
    {
        if (ringRoot != null) return;

        ringRoot = new GameObject("ChargeRingFX");
        ringRoot.transform.SetParent(transform, true); // 위치만 따라가게
        ringRoot.transform.position = transform.position;
        ringRoot.transform.localScale = Vector3.one;   // 스케일 영향 제거

        ringCoreLR = MakeRingLR("RingCore", ringWidthCore, (spriter != null ? spriter.sortingOrder : 0) + 1);
        ringHaloLR = MakeRingLR("RingHalo", ringWidthHalo, (spriter != null ? spriter.sortingOrder : 0) + 0);

        // 시작 투명
        SetLRColor(ringCoreLR, HDR(ringColor, 1f, 0f));
        SetLRColor(ringHaloLR, HDR(ringColor * new Color(1f, 0.7f, 0.7f, 1f), 0.7f, 0f));

        ringRoot.SetActive(false);
    }

    private LineRenderer MakeRingLR(string name, float width, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(ringRoot.transform, true);
        go.transform.position = ringRoot.transform.position;
        go.transform.localScale = Vector3.one;

        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; // ★ 월드 좌표로 직접 찍음 (부모 스케일 무시)
        lr.loop = true;
        lr.positionCount = ringSegments;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.sortingLayerID = spriter != null ? spriter.sortingLayerID : 0;
        lr.sortingOrder = sortingOrder;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        BuildUnitCircle(lr, ringSegments, 0.01f); // 최소 반경으로 초기화
        return lr;
    }

    private void PlayChargeRing(float duration)
    {
        EnsureChargeRing();
        ringRoot.SetActive(true);

        if (ringSeq != null && ringSeq.IsActive()) ringSeq.Kill();

        // 반경 결정(월드 유닛, 상한 적용)
        float endR = Mathf.Clamp(ringAbsoluteEndRadius, 0.01f, ringMaxRadius);
        float startR = Mathf.Clamp(ringAbsoluteStartRadius, 0.01f, endR - 0.005f);

        // 초기값 세팅
        ringCurrentRadius = startR;
        UpdateRingRadiusImmediate(startR);

        float aStart = Mathf.Clamp01(ringAlphaStart);
        float aEnd = Mathf.Clamp01(ringAlphaEnd);
        SetLRColor(ringCoreLR, HDR(ringColor, glowIntensity, aStart));
        SetLRColor(ringHaloLR, HDR(ringColor * new Color(1f, 0.7f, 0.7f, 1f), glowIntensity * 0.7f, aStart * 0.5f));

        // ① 상승
        ringSeq = DOTween.Sequence();
        float rise = duration * 0.55f;

        ringSeq.Append(DOVirtual.Float(startR, endR, rise, r => {
            ringCurrentRadius = r;
            UpdateRingRadiusImmediate(r);
        }).SetEase(Ease.OutCubic));

        ringSeq.Join(DOVirtual.Float(aStart, aEnd, rise, v => {
            SetLRColor(ringCoreLR, HDR(ringColor, glowIntensity, v));
            SetLRColor(ringHaloLR, HDR(ringColor * new Color(1f, 0.7f, 0.7f, 1f), glowIntensity * 0.7f, v * 0.5f));
        }));

        // ② 펄스
        float remain = Mathf.Max(0f, duration - rise);
        int pulses = Mathf.Max(0, ringPulseCount);
        if (remain > 0.01f && pulses > 0)
        {
            float single = remain / pulses;
            for (int i = 0; i < pulses; i++)
            {
                ringSeq.Append(DOVirtual.Float(endR, endR * ringPulseScale, single * 0.5f, r => {
                    ringCurrentRadius = r;
                    UpdateRingRadiusImmediate(r);
                }).SetEase(Ease.InOutSine));
                ringSeq.Append(DOVirtual.Float(endR * ringPulseScale, endR, single * 0.5f, r => {
                    ringCurrentRadius = r;
                    UpdateRingRadiusImmediate(r);
                }).SetEase(Ease.InOutSine));
            }
        }

        ringSeq.Play();
    }

    private void UpdateRingRadiusImmediate(float r)
    {
        if (ringCoreLR) BuildUnitCircle(ringCoreLR, ringSegments, r);
        if (ringHaloLR) BuildUnitCircle(ringHaloLR, ringSegments, r);
    }

    private void StopChargeRingImmediate()
    {
        if (ringRoot == null) return;
        if (ringSeq != null && ringSeq.IsActive()) ringSeq.Kill();
        SetLRColor(ringCoreLR, HDR(ringColor, glowIntensity, 0f));
        SetLRColor(ringHaloLR, HDR(ringColor, glowIntensity * 0.7f, 0f));
        ringRoot.SetActive(false);
    }

    private void BuildUnitCircle(LineRenderer lr, int segments, float radius)
    {
        if (segments < 3) segments = 3;
        lr.positionCount = segments;
        float step = Mathf.PI * 2f / segments;
        Vector3 center = (ringRoot != null) ? ringRoot.transform.position : Vector3.zero;
        for (int i = 0; i < segments; i++)
        {
            float a = step * i;
            Vector3 p = new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f);
            lr.SetPosition(i, center + p);
        }
    }

    // ───────────── 쇼크웨이브 ─────────────
    private void SpawnShockwave()
    {
        GameObject wave = new GameObject("ShockwaveFX");
        wave.transform.position = transform.position;
        wave.transform.localScale = Vector3.one;

        LineRenderer wLR = wave.AddComponent<LineRenderer>();
        wLR.useWorldSpace = true;
        wLR.loop = true;
        wLR.positionCount = shockwaveSegments;
        wLR.startWidth = shockwaveWidth;
        wLR.endWidth = shockwaveWidth;

        wLR.sortingLayerID = spriter != null ? spriter.sortingLayerID : 0;
        wLR.sortingOrder = (spriter != null ? spriter.sortingOrder : 0) + 2;
        wLR.material = new Material(Shader.Find("Sprites/Default"));
        BuildUnitCircle(wLR, shockwaveSegments, shockwaveRadiusStart);

        float endRadius = shockwaveAutoSize && spriter
            ? Mathf.Min(Mathf.Max(Mathf.Max(spriter.bounds.extents.x, spriter.bounds.extents.y) * 2f * shockwaveBodyMultiplier, shockwaveRadiusStart + 0.01f), shockwaveMaxRadius)
            : Mathf.Min(shockwaveRadiusEnd, shockwaveMaxRadius);

        Color c0 = HDR(shockwaveColor, glowIntensity * 1.2f, shockwaveAlphaStart);
        SetLRColor(wLR, c0);

        float dur = Mathf.Max(0.01f, shockwaveDuration * fxSpeedMultiplier);

        Sequence seq = DOTween.Sequence();
        seq.Join(DOVirtual.Float(shockwaveRadiusStart, endRadius, dur, r => BuildUnitCircle(wLR, shockwaveSegments, r))
            .SetEase(Ease.OutCubic));
        seq.Join(DOVirtual.Float(shockwaveAlphaStart, shockwaveAlphaEnd, dur, a =>
        {
            SetLRColor(wLR, HDR(shockwaveColor, glowIntensity * 1.2f, a));
        }));
        seq.OnComplete(() => { if (wave != null) Destroy(wave); });
        seq.Play();
    }

    // ───────────── 공통 유틸 ─────────────
    private void SetLRColor(LineRenderer lr, Color c)
    {
        if (lr != null && lr.material != null)
            lr.material.color = c; // Sprites/Default: Tint
        if (lr != null)
        {
            lr.startColor = c;
            lr.endColor = c;
        }
    }

    private Color HDR(Color baseCol, float intensity, float alpha)
    {
        if (!useHDRGlow) intensity = 1f;
        return new Color(baseCol.r * intensity, baseCol.g * intensity, baseCol.b * intensity, alpha);
    }
}
