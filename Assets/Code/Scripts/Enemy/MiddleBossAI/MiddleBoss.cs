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
    private int currentSkillIndex;

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
    [Tooltip("센터 기준 좌우가 서로 교차하는 규칙적 스윕 사용")]
    public bool useRegularCrossing = true;

    [Tooltip("교차 주파수(Hz). 1이면 1초에 왕복 1회")]
    public float crossingHz = 0.9f;

    [Tooltip("교차 진폭(유닛). 초기 반간격(half-sep)보다 크면 실제로 서로 겹쳐 지나감")]
    public float crossingAmplitudeUnits = 6f;

    [Tooltip("파형 선택 (사인: 부드러움 / 삼각: 속도 일정)")]
    public SweepWaveform waveform = SweepWaveform.Triangle;

    [Tooltip("엣지 홀드(양 끝에서 머무는 비율, 0~0.45 권장). 0이면 홀드 없음")]
    [Range(0f, 0.45f)] public float edgeHoldRatio = 0.2f;

    [Header("경계/길이/보조탄")]
    [Tooltip("맵 밖으로 안 나가게 클램프")]
    public bool clampToBounds = true;
    [Tooltip("좌우 여유 마진(유닛)")]
    public float clampMargin = 0.5f;

    [Tooltip("레이저가 활성화되는 시간(초)")]
    public float laserActiveDuration = 8f;

    [Tooltip("레이저가 위/아래로 얼마나 더 뻗을지(유닛)")]
    public float laserOverrun = 5f;

    [Tooltip("보조 탄막 발사 간격(초)")]
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

    [Header("경고 프리팹")]
    public GameObject warningPrefab;
    public float warningLengthScale = 2f;
    public float warningThicknessScale = 0.5f;
    public float warningOffsetDistance = 1.5f;

    private List<GameObject> activeSkillObjects = new List<GameObject>();

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
        if (!isLive || isSkillPlaying) return;

        skillTimer += Time.deltaTime;
        if (skillTimer >= skillInterval)
        {
            skillTimer = 0f;
            currentSkillIndex = Random.Range(0, 4);
            UseRandomSkill();
        }
    }

    private void UseRandomSkill()
    {
        isSkillPlaying = true;
        switch (currentSkillIndex)
        {
            case 0: StartCoroutine(SkillBulletCircle()); break;
            case 1: StartCoroutine(SkillLaserPattern()); break;
            case 2: StartCoroutine(SkillSwordPattern()); break;
            case 3: StartCoroutine(SkillJumpAndShoot()); break;
        }
    }

    // ────────── 스킬 1: 회전 탄막 ──────────
    private IEnumerator SkillBulletCircle()
    {
        float duration = 5f;
        float fireIntervalLocal = 0.5f;
        float elapsed = 0f;
        float offset = 0f;

        while (elapsed < duration)
        {
            Vector3 origin = transform.position;
            float step = 360f / Mathf.Max(1, bulletsPerWave);

            for (int i = 0; i < bulletsPerWave; i++)
            {
                float a = (step * i + offset) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                var go = Instantiate(bulletPrefab, origin, Quaternion.identity);
                var rb = go.GetComponent<Rigidbody2D>();
                if (rb) rb.linearVelocity = dir * bulletSpeed;
                activeSkillObjects.Add(go);
            }

            offset += bulletAngle;
            elapsed += fireIntervalLocal;
            yield return new WaitForSeconds(fireIntervalLocal);
        }

        yield return StartCoroutine(SkillEndDelay());
    }

    // ────────── 스킬 2: 규칙적 교차 스윕 레이저 ──────────
    private IEnumerator SkillLaserPattern()
    {
        if (mapCollider == null)
        {
            Debug.LogWarning("mapCollider 미지정!");
            yield return StartCoroutine(SkillEndDelay());
            yield break;
        }

        Bounds b = mapCollider.bounds;

        // 시작점 계산
        Vector3 leftBase = (useStartAnchors && leftLaserAnchor)
            ? leftLaserAnchor.position
            : transform.position + new Vector3(leftLaserOffsetX, 0);
        Vector3 rightBase = (useStartAnchors && rightLaserAnchor)
            ? rightLaserAnchor.position
            : transform.position + new Vector3(rightLaserOffsetX, 0);

        leftBase += (Vector3)laserExtraStartOffset;
        rightBase += (Vector3)laserExtraStartOffset;

        // 경고 표시
        List<GameObject> warns = new();
        if (warningPrefab)
        {
            float len = b.size.y + 10f;
            var wL = Instantiate(warningPrefab, leftBase, Quaternion.Euler(0, 0, 90f));
            var wR = Instantiate(warningPrefab, rightBase, Quaternion.Euler(0, 0, 90f));
            wL.transform.localScale = new Vector3(len, warningThicknessScale, warningThicknessScale);
            wR.transform.localScale = new Vector3(len, warningThicknessScale, warningThicknessScale);
            warns.Add(wL); warns.Add(wR);
            activeSkillObjects.Add(wL);
            activeSkillObjects.Add(wR);
        }
        yield return new WaitForSeconds(1f);
        foreach (var w in warns) if (w) Destroy(w);
        warns.Clear();

        // 레이저 생성
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

        float over = Mathf.Max(0f, laserOverrun);
        float topY = b.extents.y + over;

        // 초기 세팅
        leftLR.SetPosition(0, leftBase + Vector3.up * topY);
        leftLR.SetPosition(1, leftBase + Vector3.down * topY);
        rightLR.SetPosition(0, rightBase + Vector3.up * topY);
        rightLR.SetPosition(1, rightBase + Vector3.down * topY);

        // 규칙적 교차 스윕 파라미터
        float centerX = (leftBase.x + rightBase.x) * 0.5f;
        float halfSep0 = Mathf.Abs(rightBase.x - leftBase.x) * 0.5f;
        float amp = Mathf.Max(0f, crossingAmplitudeUnits); // 진폭 고정(랜덤/호흡 없음)
        float period = Mathf.Max(0.0001f, 1f / Mathf.Max(0.0001f, crossingHz));
        float startT = Time.time;

        // 보조 탄막
        float timer = 0f;
        int patIdx = 0;
        string[] patSeq = { "X", "Y", "X", "Y" };

        // 이동 루프
        float elapsed = 0f;
        while (elapsed < laserActiveDuration)
        {
            elapsed += Time.deltaTime;
            timer += Time.deltaTime;

            float t = (Time.time - startT) / period; // 사이클 기준 시간
            float u = t - Mathf.Floor(t);            // 0~1 정규화 위상

            // 파형 생성 (0~1)
            float wv = waveform == SweepWaveform.Sine
                ? 0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * u)     // 사인 누적형(0→1→0 형태로 맵핑)
                : 1f - Mathf.Abs(1f - 2f * u);                   // 삼각파(속도 일정)

            // 엣지 홀드(0~0.45) : 양 끝 구간을 평탄화해 '머무는' 느낌
            if (edgeHoldRatio > 0f)
            {
                float hold = Mathf.Clamp(edgeHoldRatio, 0f, 0.45f);
                // 아래는 wv를 0..1 사이에서 가장자리 구간을 클램프/리맵하는 간단한 방식
                if (wv < hold) wv = 0f;
                else if (wv > 1f - hold) wv = 1f;
                else
                {
                    // 가운데 구간을 0..1로 다시 매핑해 매끄럽게 이어줌
                    float range = 1f - 2f * hold;
                    wv = (wv - hold) / range;
                    // 부드러운 연결감(선택): S-curve
                    wv = wv * wv * (3f - 2f * wv);
                }
            }

            // 교차 오프셋 (센터에서 좌우로 이동)
            // inward>0이면 서로 가까워짐, <0이면 멀어짐. 삼각/사인이라 완전 규칙적.
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

            leftLR.SetPosition(0, curL + Vector3.up * topY);
            leftLR.SetPosition(1, curL + Vector3.down * topY);
            rightLR.SetPosition(0, curR + Vector3.up * topY);
            rightLR.SetPosition(1, curR + Vector3.down * topY);

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
                    var rb = bObj.GetComponent<Rigidbody2D>();
                    if (rb) rb.linearVelocity = d.normalized * bulletSpeed;
                    activeSkillObjects.Add(bObj);
                }

                patIdx++;
                timer = 0f;
            }

            yield return null;
        }

        Destroy(leftLaser);
        Destroy(rightLaser);
        yield return StartCoroutine(SkillEndDelay());
    }

    private void SetupLaser(LineRenderer lr, Color c)
    {
        lr.positionCount = 2;
        lr.startWidth = lr.endWidth = 0.15f;
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

    // ────────── 스킬 3: 검 ──────────
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

        List<GameObject> warns = new();
        if (warningPrefab)
        {
            float len = r * 2f * warningLengthScale;
            var a = Instantiate(warningPrefab, c, Quaternion.Euler(0, 0, swordStartAngle));
            var b = Instantiate(warningPrefab, c, Quaternion.Euler(0, 0, swordStartAngle + 180f));
            a.transform.localScale = b.transform.localScale = new(len, warningThicknessScale, warningThicknessScale);
            warns.Add(a); warns.Add(b);
            yield return new WaitForSeconds(swordWarningDuration);
            foreach (var w in warns) Destroy(w);
        }

        var la = new GameObject("RotLaserA").AddComponent<LineRenderer>();
        var lb = new GameObject("RotLaserB").AddComponent<LineRenderer>();
        SetupLaser(la, Color.red); SetupLaser(lb, Color.red);

        float ang = swordStartAngle;
        float time = 0f;
        while (time < 360f / Mathf.Max(1f, swordRotateSpeed))
        {
            ang += swordRotateSpeed * Time.deltaTime;
            float rad = ang * Mathf.Deg2Rad;
            Vector3 da = new(Mathf.Cos(rad), Mathf.Sin(rad));
            la.SetPosition(0, c); la.SetPosition(1, c + da * r);
            lb.SetPosition(0, c); lb.SetPosition(1, c - da * r);
            CheckLaserDamage(c, da, r);
            CheckLaserDamage(c, -da, r);
            time += Time.deltaTime;
            yield return null;
        }

        Destroy(la.gameObject); Destroy(lb.gameObject);
        yield return StartCoroutine(SkillEndDelay());
    }

    private void CheckLaserDamage(Vector3 start, Vector3 dir, float dist)
    {
        var h = Physics2D.Raycast(start, dir, dist, LayerMask.GetMask("Player"));
        if (h.collider && h.collider.CompareTag("Player"))
            GameManager.Instance?.playerDamaged?.TakeDamage(laserDamage, transform.position);
    }

    // ────────── 스킬 4: 점프 후 원형탄 ──────────
    private IEnumerator SkillJumpAndShoot()
    {
        Vector3 s = transform.position;
        Vector3 p = s + Vector3.up * jumpHeight;
        yield return transform.DOMove(p, jumpDuration).SetEase(Ease.OutQuad).WaitForCompletion();

        List<GameObject> warns = new();
        if (warningPrefab)
        {
            float step = 360f / Mathf.Max(1, jumpBulletCount);
            for (int i = 0; i < jumpBulletCount; i++)
            {
                float a = step * i;
                float rad = a * Mathf.Deg2Rad;
                Vector3 dir = new(Mathf.Cos(rad), Mathf.Sin(rad), 0);
                var w = Instantiate(warningPrefab, s + dir * warningOffsetDistance, Quaternion.Euler(0, 0, a));
                w.transform.localScale = new(warningLengthScale, warningThicknessScale, warningThicknessScale);
                warns.Add(w);
            }
        }

        yield return transform.DOMove(s, jumpDuration).SetEase(Ease.InQuad).WaitForCompletion();
        foreach (var w in warns) if (w) Destroy(w);

        float step2 = 360f / Mathf.Max(1, jumpBulletCount);
        for (int i = 0; i < jumpBulletCount; i++)
        {
            float a = step2 * i * Mathf.Deg2Rad;
            Vector2 d = new(Mathf.Cos(a), Mathf.Sin(a));
            var bObj = Instantiate(bulletPrefab, s, Quaternion.identity);
            var rb = bObj.GetComponent<Rigidbody2D>();
            if (rb) rb.linearVelocity = d * jumpBulletSpeed;
            activeSkillObjects.Add(bObj);
        }

        yield return StartCoroutine(SkillEndDelay());
    }

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
    }

    public void SetDead()
    {
        isLive = false;
        ClearAllSkillObjects();
    }
}
