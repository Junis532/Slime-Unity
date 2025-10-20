using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiddleBoss : MonoBehaviour
{
    private bool isLive = true;
    private SpriteRenderer spriter;
    private EnemyAnimation enemyAnimation;

    // ────────── 스킬/타이밍 ──────────
    [Header("패턴 타이밍")]
    public float skillInterval = 4f;
    private float skillTimer = 0f;
    private bool isSkillPlaying = false;

    // ────────── 패턴 온/오프 ──────────
    [Header("패턴 온/오프")]
    public bool enableBulletCircle = true;  // 스킬1: 전맵 탄 살포(회전)
    public bool enableLaserPattern = true;  // 스킬2: 교차 스윕 레이저
    public bool enableSwordPattern = true; // 스킬3: 검(회전 레이저)
    public bool enableJumpPattern = false;// 스킬4: 점프 후 원형탄(기본 꺼짐)

    // ────────── 패턴별 경고 표시 ──────────
    [Header("경고 표시 토글")]
    public bool warnBulletCircle = false; // 전맵 탄 살포엔 기본적으로 경고선 불필요
    public bool warnLaserPattern = true;
    public bool warnSwordPattern = true;
    public bool warnJumpPattern = false;

    // ────────── 패턴 1: 탄막 ──────────
    [Header("탄막 패턴")]
    public GameObject bulletPrefab;
    public int bulletsPerWave = 12;
    public int bulletAngle = 0;
    public float bulletSpeed = 6f;

    // ────────── 패턴 2: 레이저 ──────────
    [Header("레이저 패턴")]
    public Collider2D mapCollider;
    public int laserDamage = 100;
    public Material laserMaterial;

    [Header("레이저 시작점 조정")]
    public float leftLaserOffsetX = -2f;
    public float rightLaserOffsetX = 2f;
    public Vector2 laserExtraStartOffset = Vector2.zero;
    public bool useStartAnchors = false;
    public Transform leftLaserAnchor;
    public Transform rightLaserAnchor;

    // ────────── 규칙적 교차 스윕 설정 ──────────
    public enum SweepWaveform { Sine, Triangle }

    [Header("규칙적 교차 스윕")]
    public bool useRegularCrossing = true;
    public float crossingHz = 0.9f;
    public float crossingAmplitudeUnits = 6f;
    public SweepWaveform waveform = SweepWaveform.Triangle;
    [Range(0f, 0.45f)] public float edgeHoldRatio = 0.2f;

    [Header("경계/길이/보조탄")]
    public bool clampToBounds = true;
    public float clampMargin = 0.5f;
    public float laserActiveDuration = 8f;
    public float laserOverrun = 5f;
    public float fireInterval = 0.5f;

    // ────────── 패턴 3: 검 ──────────
    [Header("검 휘두르기")]
    public float swordRotateSpeed = 360f;
    public float swordStartAngle = 180f;
    public float swordWarningDuration = 1f;

    // ────────── 패턴 4: 점프 후 원형탄 ──────────
    [Header("점프 후 원형탄")]
    public float jumpHeight = 5f;
    public float jumpDuration = 0.5f;
    public int jumpBulletCount = 8;
    public float jumpBulletSpeed = 6f;

    // ────────── 경고 프리팹(선택) ──────────
    [Header("경고 프리팹(선택)")]
    public GameObject warningPrefab;
    public float warningLengthScale = 2f;
    public float warningThicknessScale = 0.5f;
    public float warningOffsetDistance = 1.5f;

    // ────────── 공통 경고 옵션 ──────────
    [Header("공통 경고 옵션")]
    public float preWarnDuration = 1.0f;
    public float warnLineWidth = 0.28f;
    public Color warnLineColor = new(1f, 0.6f, 0.2f, 0.95f);
    public Material warnLineMaterial;
    public bool keepWarnDuringTransition = false;   // 기본: 본 패턴 직전 즉시 제거
    public float warnTransitionTime = 0.15f;

    // ────────── 디버그 ──────────
    [Header("디버그")]
    public bool debugForceBulletCircle = false; // 체크하면 즉시 방사형 탄막 실행

    private readonly List<GameObject> activeSkillObjects = new();
    private readonly List<LineRenderer> _warnLines = new();

    void Start()
    {
        spriter = GetComponent<SpriteRenderer>();
        enemyAnimation = GetComponent<EnemyAnimation>();

        if (mapCollider == null)
        {
            GameObject roomObj = GameObject.Find("RC 00");
            if (roomObj != null)
            {
                mapCollider = roomObj.GetComponent<BoxCollider2D>();
                if (mapCollider == null)
                    Debug.LogWarning("RC 00 안에 BoxCollider2D가 없습니다!");
            }
            else Debug.LogWarning("RC 00 오브젝트를 찾을 수 없습니다!");
        }
    }

    void Update()
    {
        // 디버그: 강제 방사형 탄막
        if (debugForceBulletCircle && !isSkillPlaying)
        {
            debugForceBulletCircle = false;
            isSkillPlaying = true;
            StartCoroutine(SkillBulletCircle());
            return;
        }

        if (!isLive || isSkillPlaying) return;

        skillTimer += Time.deltaTime;
        if (skillTimer >= skillInterval)
        {
            skillTimer = 0f;
            UseRandomSkill();
        }
    }

    private void UseRandomSkill()
    {
        isSkillPlaying = true;

        var skills = new List<System.Func<IEnumerator>>();
        if (enableBulletCircle) skills.Add(SkillBulletCircle);
        if (enableLaserPattern) skills.Add(SkillLaserPattern);
        if (enableSwordPattern) skills.Add(SkillSwordPattern);
        if (enableJumpPattern) skills.Add(SkillJumpAndShoot);

        if (skills.Count == 0)
        {
            Debug.LogWarning("활성화된 보스 패턴이 없습니다.");
            isSkillPlaying = false;
            return;
        }

        int idx = Random.Range(0, skills.Count);
        StartCoroutine(skills[idx]());
    }

    // ────────────────────────────────────────────────────────────
    // 경고 라인 유틸
    // ────────────────────────────────────────────────────────────
    private LineRenderer CreateWarnLine(string name, float widthMul = 1f, int order = 9)
    {
        var go = new GameObject(name);
        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.startWidth = lr.endWidth = Mathf.Max(0.001f, warnLineWidth * widthMul);
        lr.material = warnLineMaterial != null ? warnLineMaterial : new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = warnLineColor;
        lr.useWorldSpace = true;
        lr.sortingLayerName = "Foreground";
        lr.sortingOrder = order;

        _warnLines.Add(lr);
        activeSkillObjects.Add(go);
        return lr;
    }

    private void KillAllWarnLines()
    {
        for (int i = _warnLines.Count - 1; i >= 0; i--)
        {
            if (_warnLines[i]) Destroy(_warnLines[i].gameObject);
        }
        _warnLines.Clear();
    }

    private void SetLineVertical(LineRenderer lr, Vector3 center, float halfLen)
    {
        lr.SetPosition(0, center + Vector3.up * halfLen);
        lr.SetPosition(1, center + Vector3.down * halfLen);
    }

    private void SetLineByDir(LineRenderer lr, Vector3 start, Vector3 dir, float len)
    {
        lr.SetPosition(0, start);
        lr.SetPosition(1, start + dir.normalized * len);
    }

    private IEnumerator _CoDelayedKillWarnLines(float delay)
    {
        yield return new WaitForSeconds(delay);
        KillAllWarnLines();
    }

    // ────────────────────────────────────────────────────────────
    // Rigidbody2D 보장 유틸(없으면 붙이고, 공중 탄환용 설정)
    // ────────────────────────────────────────────────────────────
    private Rigidbody2D EnsureRB2D(GameObject go)
    {
        var rb = go.GetComponent<Rigidbody2D>();
        if (!rb) rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.freezeRotation = true;
        return rb;
    }

    // ────────────────────────────────────────────────────────────
    // 스킬 1: 전맵 탄 살포(회전 탄막) — 기본 경고선 없음
    // ────────────────────────────────────────────────────────────
    private IEnumerator SkillBulletCircle()
    {
        int count = Mathf.Max(1, bulletsPerWave);
        float step = 360f / count;
        float duration = 5f;
        float fireIntervalLocal = 0.5f;

        Debug.Log($"[MiddleBoss] SkillBulletCircle START (count:{count}, speed:{bulletSpeed})");

        float warnOffset = 0f;
        if (warnBulletCircle) // 기본 false
        {
            float warnTime = 0f;
            float warnRadius = 3.0f;

            for (int i = 0; i < count; i++)
                CreateWarnLine($"Warn_BulletDir_{i}", 1.35f, 9);

            while (warnTime < preWarnDuration)
            {
                Vector3 origin = transform.position;
                int idx = 0;
                foreach (var wl in _warnLines)
                {
                    float a = (step * idx + warnOffset) * Mathf.Deg2Rad;
                    Vector3 dir = new(Mathf.Cos(a), Mathf.Sin(a), 0f);
                    SetLineByDir(wl, origin, dir, warnRadius);
                    idx++;
                }
                warnOffset += bulletAngle * Time.deltaTime * (1f / fireIntervalLocal);
                warnTime += Time.deltaTime;
                yield return null;
            }
            KillAllWarnLines(); // 본 패턴 직전 제거
        }

        // 본 패턴
        float elapsed = 0f;
        float offset = warnOffset; // 경고 없었다면 0
        while (elapsed < duration)
        {
            Vector3 origin = transform.position;
            for (int i = 0; i < count; i++)
            {
                float a = (step * i + offset) * Mathf.Deg2Rad;
                Vector2 dir = new(Mathf.Cos(a), Mathf.Sin(a));
                var go = Instantiate(bulletPrefab, origin, Quaternion.identity);
                var rb = EnsureRB2D(go);
                rb.linearVelocity = dir * bulletSpeed;   // 안전: velocity 사용
                activeSkillObjects.Add(go);
            }

            offset += bulletAngle;
            elapsed += fireIntervalLocal;
            yield return new WaitForSeconds(fireIntervalLocal);
        }

        Debug.Log("[MiddleBoss] SkillBulletCircle END");
        yield return StartCoroutine(SkillEndDelay());
    }

    // ────────────────────────────────────────────────────────────
    // 스킬 2: 규칙적 교차 스윕 레이저
    // ────────────────────────────────────────────────────────────
    private IEnumerator SkillLaserPattern()
    {
        if (mapCollider == null)
        {
            Debug.LogWarning("mapCollider 미지정!");
            yield return StartCoroutine(SkillEndDelay());
            yield break;
        }

        Bounds b = mapCollider.bounds;

        // 시작점
        Vector3 leftBase = (useStartAnchors && leftLaserAnchor) ? leftLaserAnchor.position : transform.position + new Vector3(leftLaserOffsetX, 0);
        Vector3 rightBase = (useStartAnchors && rightLaserAnchor) ? rightLaserAnchor.position : transform.position + new Vector3(rightLaserOffsetX, 0);
        leftBase += (Vector3)laserExtraStartOffset;
        rightBase += (Vector3)laserExtraStartOffset;

        float over = Mathf.Max(0f, laserOverrun);
        float topY = b.extents.y + over;

        float centerX = (leftBase.x + rightBase.x) * 0.5f;
        float halfSep0 = Mathf.Abs(rightBase.x - leftBase.x) * 0.5f;
        float amp = Mathf.Max(0f, crossingAmplitudeUnits);
        float period = Mathf.Max(0.0001f, 1f / Mathf.Max(0.0001f, crossingHz));

        // 경고 라인(토글)
        float t0 = Time.time;
        if (warnLaserPattern)
        {
            var warnL = CreateWarnLine("Warn_Left", 1.8f, 9);
            var warnR = CreateWarnLine("Warn_Right", 1.8f, 9);

            float warnElapsed = 0f;
            while (warnElapsed < preWarnDuration)
            {
                float wv = Waveform01((Time.time - t0) / period, waveform);
                if (edgeHoldRatio > 0f) wv = ApplyEdgeHold(wv, edgeHoldRatio);

                float inward = Mathf.Lerp(-amp, amp, wv);
                float lx = centerX - halfSep0 + inward;
                float rx = centerX + halfSep0 - inward;

                if (clampToBounds)
                {
                    float minX = b.min.x + clampMargin;
                    float maxX = b.max.x - clampMargin;
                    lx = Mathf.Clamp(lx, minX, maxX);
                    rx = Mathf.Clamp(rx, minX, maxX);
                }

                Vector3 curL = new(lx, transform.position.y, 0f);
                Vector3 curR = new(rx, transform.position.y, 0f);

                SetLineVertical(warnL, curL, topY);
                SetLineVertical(warnR, curR, topY);

                warnElapsed += Time.deltaTime;
                yield return null;
            }

            if (!keepWarnDuringTransition) KillAllWarnLines();
            else StartCoroutine(_CoDelayedKillWarnLines(Mathf.Max(0f, warnTransitionTime)));
        }

        // 본 레이저
        GameObject leftLaser = new("LeftLaser");
        GameObject rightLaser = new("RightLaser");
        var leftLR = leftLaser.AddComponent<LineRenderer>();
        var rightLR = rightLaser.AddComponent<LineRenderer>();
        SetupLaser(leftLR, Color.red);
        SetupLaser(rightLR, Color.red);
        leftLR.sortingLayerName = "Foreground";
        rightLR.sortingLayerName = "Foreground";
        leftLR.sortingOrder = rightLR.sortingOrder = 10;
        activeSkillObjects.Add(leftLaser);
        activeSkillObjects.Add(rightLaser);

        float startT = Time.time;
        float elapsed = 0f, timer = 0f;
        int patIdx = 0;
        string[] patSeq = { "X", "Y", "X", "Y" };

        while (elapsed < laserActiveDuration)
        {
            elapsed += Time.deltaTime;
            timer += Time.deltaTime;

            float wv = Waveform01((Time.time - startT) / period, waveform);
            if (edgeHoldRatio > 0f) wv = ApplyEdgeHold(wv, edgeHoldRatio);

            float inward = Mathf.Lerp(-amp, amp, wv);
            float lx = centerX - halfSep0 + inward;
            float rx = centerX + halfSep0 - inward;

            if (clampToBounds)
            {
                float minX = b.min.x + clampMargin;
                float maxX = b.max.x - clampMargin;
                lx = Mathf.Clamp(lx, minX, maxX);
                rx = Mathf.Clamp(rx, minX, maxX);
            }

            Vector3 curL = new(lx, transform.position.y, 0f);
            Vector3 curR = new(rx, transform.position.y, 0f);

            SetLineVertical(leftLR, curL, topY);
            SetLineVertical(rightLR, curR, topY);

            CheckLaserHit(leftLR);
            CheckLaserHit(rightLR);

            // 보조 탄막
            if (timer >= fireInterval)
            {
                string p = patSeq[patIdx % patSeq.Length];
                Vector2[] dirs = p == "X"
                    ? new[] { new Vector2(1, 1), new Vector2(-1, 1), new Vector2(1, -1), new Vector2(-1, -1) }
                    : new[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

                foreach (var d in dirs)
                {
                    var bObj = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
                    var rb = EnsureRB2D(bObj);
                    rb.linearVelocity = d.normalized * bulletSpeed;
                    activeSkillObjects.Add(bObj);
                }
                patIdx++;
                timer = 0f;
            }

            yield return null;
        }

        Destroy(leftLaser);
        Destroy(rightLaser);
        KillAllWarnLines(); // 혹시 남았으면 정리

        yield return StartCoroutine(SkillEndDelay());
    }

    // ────────────────────────────────────────────────────────────
    // 스킬 3: 검(양방향 회전)
    // ────────────────────────────────────────────────────────────
    private IEnumerator SkillSwordPattern()
    {
        if (mapCollider == null)
        {
            Debug.LogWarning("mapCollider 미지정!");
            yield return StartCoroutine(SkillEndDelay());
            yield break;
        }

        float r = Mathf.Max(mapCollider.bounds.size.x, mapCollider.bounds.size.y) / 2f;
        Vector3 c = transform.position;

        float ang = swordStartAngle;

        if (warnSwordPattern)
        {
            var warnA = CreateWarnLine("Warn_Sword_A", 1.6f, 9);
            var warnB = CreateWarnLine("Warn_Sword_B", 1.6f, 9);

            float wtime = 0f;
            while (wtime < swordWarningDuration)
            {
                ang += swordRotateSpeed * Time.deltaTime;
                float rad = ang * Mathf.Deg2Rad;
                Vector3 dir = new(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
                SetLineByDir(warnA, c, dir, r);
                SetLineByDir(warnB, c, -dir, r);
                wtime += Time.deltaTime;
                yield return null;
            }
            KillAllWarnLines();
        }

        var la = new GameObject("RotLaserA").AddComponent<LineRenderer>();
        var lb = new GameObject("RotLaserB").AddComponent<LineRenderer>();
        SetupLaser(la, Color.red); SetupLaser(lb, Color.red);

        float time = 0f;
        while (time < 360f / Mathf.Max(1f, swordRotateSpeed))
        {
            ang += swordRotateSpeed * Time.deltaTime;
            float rad = ang * Mathf.Deg2Rad;
            Vector3 da = new(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
            SetLineByDir(la, c, da, r);
            SetLineByDir(lb, c, -da, r);
            CheckLaserDamage(c, da, r);
            CheckLaserDamage(c, -da, r);
            time += Time.deltaTime;
            yield return null;
        }

        Destroy(la.gameObject); Destroy(lb.gameObject);
        yield return StartCoroutine(SkillEndDelay());
    }

    // ────────────────────────────────────────────────────────────
    // 스킬 4: 점프 후 원형탄 (기본 비활성)
    // ────────────────────────────────────────────────────────────
    private IEnumerator SkillJumpAndShoot()
    {
        if (!enableJumpPattern) { yield return StartCoroutine(SkillEndDelay()); yield break; }

        Vector3 s = transform.position;
        Vector3 p = s + Vector3.up * jumpHeight;

        int count = Mathf.Max(1, jumpBulletCount);
        float step = 360f / count;

        if (warnJumpPattern)
        {
            float warnLen = warningLengthScale;
            for (int i = 0; i < count; i++)
                CreateWarnLine($"Warn_JumpBullet_{i}", 1.25f, 9);

            float warnElapsed = 0f;
            while (warnElapsed < preWarnDuration)
            {
                int idx = 0;
                foreach (var wl in _warnLines)
                {
                    float a = step * idx;
                    float rad = a * Mathf.Deg2Rad;
                    Vector3 dir = new(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
                    SetLineByDir(wl, s, dir, warnLen);
                    idx++;
                }
                warnElapsed += Time.deltaTime;
                yield return null;
            }
        }

        yield return transform.DOMove(p, jumpDuration).SetEase(Ease.OutQuad).WaitForCompletion();
        yield return new WaitForSeconds(0.05f);
        yield return transform.DOMove(s, jumpDuration).SetEase(Ease.InQuad).WaitForCompletion();

        KillAllWarnLines();

        for (int i = 0; i < count; i++)
        {
            float a = step * i * Mathf.Deg2Rad;
            Vector2 d = new(Mathf.Cos(a), Mathf.Sin(a));
            var bObj = Instantiate(bulletPrefab, s, Quaternion.identity);
            var rb = EnsureRB2D(bObj);
            rb.linearVelocity = d * jumpBulletSpeed;
            activeSkillObjects.Add(bObj);
        }

        yield return StartCoroutine(SkillEndDelay());
    }

    // ────────────────────────────────────────────────────────────
    // 공통: 레이저/파형/대미지 체크
    // ────────────────────────────────────────────────────────────
    private float Waveform01(float t, SweepWaveform form)
    {
        float u = t - Mathf.Floor(t); // 0..1
        return (form == SweepWaveform.Sine)
            ? 0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * u)
            : 1f - Mathf.Abs(1f - 2f * u);
    }

    private float ApplyEdgeHold(float wv, float edge)
    {
        float hold = Mathf.Clamp(edge, 0f, 0.45f);
        if (hold <= 0f) return wv;
        if (wv < hold) return 0f;
        if (wv > 1f - hold) return 1f;
        float range = 1f - 2f * hold;
        float mid = (wv - hold) / range;
        return mid * mid * (3f - 2f * mid); // S-curve
    }

    private void SetupLaser(LineRenderer lr, Color c)
    {
        lr.positionCount = 2;
        lr.startWidth = lr.endWidth = 0.18f; // 실제 레이저 두께
        lr.material = laserMaterial != null ? laserMaterial : new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = c;
        lr.useWorldSpace = true;
    }

    private void CheckLaserHit(LineRenderer lr)
    {
        var hits = Physics2D.LinecastAll(lr.GetPosition(0), lr.GetPosition(1), LayerMask.GetMask("Player"));
        foreach (var h in hits)
            if (h.collider && h.collider.CompareTag("Player"))
                GameManager.Instance?.playerDamaged?.TakeDamage(laserDamage, transform.position);
    }

    private void CheckLaserDamage(Vector3 start, Vector3 dir, float dist)
    {
        var h = Physics2D.Raycast(start, dir, dist, LayerMask.GetMask("Player"));
        if (h.collider && h.collider.CompareTag("Player"))
            GameManager.Instance?.playerDamaged?.TakeDamage(laserDamage, transform.position);
    }

    // ────────────────────────────────────────────────────────────
    // 공통 종료/정리
    // ────────────────────────────────────────────────────────────
    private IEnumerator SkillEndDelay()
    {
        yield return new WaitForSeconds(1f);
        isSkillPlaying = false;
    }

    public void ClearAllSkillObjects()
    {
        foreach (var o in activeSkillObjects)
            if (o) Destroy(o);
        activeSkillObjects.Clear();
        KillAllWarnLines();
    }

    public void SetDead()
    {
        isLive = false;
        ClearAllSkillObjects();
    }

    void OnDisable()
    {
        KillAllWarnLines();
    }
}
