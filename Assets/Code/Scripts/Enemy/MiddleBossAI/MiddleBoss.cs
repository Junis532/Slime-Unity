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
    public float laserDuration = 2f;
    public int laserDamage = 100;
    public Material laserMaterial;

    // ★ 인스펙터에서 좌우 레이저 위치 조절 가능
    [Header("레이저 소환 위치 조정")]
    public float leftLaserOffsetX = -2f;   // 보스 위치 기준 좌측 레이저 X 오프셋
    public float rightLaserOffsetX = 2f;   // 보스 위치 기준 우측 레이저 X 오프셋


    // ────────── 패턴 3: 검 ──────────
    [Header("검 휘두르기 패턴 설정")]
    public float swordRotateSpeed = 360f;
    public float swordStartAngle = 180f;
    public float swordWarningDuration = 1.0f;

    // ────────── 패턴 4: 점프 후 원형탄 ──────────
    [Header("점프 후 원형탄 패턴")]
    public float jumpHeight = 5f;
    public float jumpDuration = 0.5f;
    public int jumpBulletCount = 8;
    public float jumpBulletSpeed = 6f;

    // 🔥 경고 프리팹 설정
    [Header("경고 설정")]
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
            else
                Debug.LogWarning("RC 00 오브젝트를 찾을 수 없습니다!");
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
        float fireInterval = 0.5f;
        float elapsed = 0f;
        float currentAngleOffset = 0f;

        while (elapsed < duration)
        {
            Vector3 origin = transform.position;
            float step = 360f / bulletsPerWave;

            for (int i = 0; i < bulletsPerWave; i++)
            {
                float ang = (step * i + currentAngleOffset) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                GameObject go = Instantiate(bulletPrefab, origin, Quaternion.identity);
                Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
                if (rb) rb.linearVelocity = dir * bulletSpeed;

                activeSkillObjects.Add(go);
            }

            currentAngleOffset += bulletAngle;
            elapsed += fireInterval;
            yield return new WaitForSeconds(fireInterval);
        }

        yield return StartCoroutine(SkillEndDelay());
    }

    // ────────── 스킬 2: 레이저 (경고 위치에서 시작 후 움직이도록 수정) ──────────
    private IEnumerator SkillLaserPattern()
    {
        if (mapCollider == null)
        {
            Debug.LogWarning("mapCollider가 설정되지 않았습니다!");
            yield return StartCoroutine(SkillEndDelay());
            yield break;
        }

        Bounds bounds = mapCollider.bounds;
        float centerY = transform.position.y;
        List<GameObject> activeWarnings = new List<GameObject>();
        float warningLength = bounds.size.y + 10f;

        // ⚠️ 경고 위치 생성
        Vector3 leftWarningPos = transform.position + Vector3.left * 2f;  // 인스펙터 값으로 대체 가능
        Vector3 rightWarningPos = transform.position + Vector3.right * 2f; // 인스펙터 값으로 대체 가능

        if (warningPrefab != null)
        {
            GameObject warningLeft = Instantiate(warningPrefab, leftWarningPos, Quaternion.Euler(0, 0, 90f));
            warningLeft.transform.localScale = new Vector3(warningLength, warningThicknessScale, warningThicknessScale);
            activeWarnings.Add(warningLeft);
            activeSkillObjects.Add(warningLeft);

            GameObject warningRight = Instantiate(warningPrefab, rightWarningPos, Quaternion.Euler(0, 0, 90f));
            warningRight.transform.localScale = new Vector3(warningLength, warningThicknessScale, warningThicknessScale);
            activeWarnings.Add(warningRight);
            activeSkillObjects.Add(warningRight);
        }

        // ⏱️ 경고 시간 대기
        yield return new WaitForSeconds(1f);

        // ⚠️ 경고 제거
        foreach (var warning in activeWarnings)
        {
            if (warning != null) Destroy(warning);
            activeSkillObjects.Remove(warning);
        }
        activeWarnings.Clear();

        // ⚡ 레이저 발사 (경고 위치 그대로)
        GameObject leftLaser = new GameObject("LeftLaser");
        LineRenderer leftLR = leftLaser.AddComponent<LineRenderer>();
        SetupLaser(leftLR, Color.red);

        GameObject rightLaser = new GameObject("RightLaser");
        LineRenderer rightLR = rightLaser.AddComponent<LineRenderer>();
        SetupLaser(rightLR, Color.red);

        activeSkillObjects.Add(leftLaser);
        activeSkillObjects.Add(rightLaser);

        // 레이저 길이 추가
        float laserExtraLength = 5f; // 위/아래로 추가할 길이

        // 초기 위치
        leftLR.SetPosition(0, leftWarningPos + Vector3.up * (bounds.extents.y + laserExtraLength));
        leftLR.SetPosition(1, leftWarningPos + Vector3.down * (bounds.extents.y + laserExtraLength));
        rightLR.SetPosition(0, rightWarningPos + Vector3.up * (bounds.extents.y + laserExtraLength));
        rightLR.SetPosition(1, rightWarningPos + Vector3.down * (bounds.extents.y + laserExtraLength));

        // 레이저 움직임 설정
        float pulseSpeed = 7f;
        float laserElapsed = 0f;
        float laserActiveDuration = 8f;
        float startTime = Time.time;

        float fireInterval = 0.5f;
        float fireTimer = 0f;
        int patternIndex = 0;
        string[] patternSequence = { "X", "Y", "X", "Y", "X", "Y" };

        while (laserElapsed < laserActiveDuration)
        {
            laserElapsed += Time.deltaTime;
            fireTimer += Time.deltaTime;

            // 경고 위치 기준으로 PingPong 이동
            float moveOffset = Mathf.PingPong((Time.time - startTime) * pulseSpeed, bounds.extents.x * 0.7f);

            Vector3 curLeftPos = leftWarningPos + Vector3.left * moveOffset;
            Vector3 curRightPos = rightWarningPos + Vector3.right * moveOffset;

            leftLR.SetPosition(0, curLeftPos + Vector3.up * (bounds.extents.y + laserExtraLength));
            leftLR.SetPosition(1, curLeftPos + Vector3.down * (bounds.extents.y + laserExtraLength));
            rightLR.SetPosition(0, curRightPos + Vector3.up * (bounds.extents.y + laserExtraLength));
            rightLR.SetPosition(1, curRightPos + Vector3.down * (bounds.extents.y + laserExtraLength));

            CheckLaserHit(leftLR);
            CheckLaserHit(rightLR);

            // 보조 탄막
            if (fireTimer >= fireInterval)
            {
                string pattern = patternSequence[patternIndex % patternSequence.Length];
                Vector2[] dirs = pattern == "X"
                    ? new Vector2[] { new Vector2(1, 1), new Vector2(-1, 1), new Vector2(1, -1), new Vector2(-1, -1) }
                    : new Vector2[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

                foreach (Vector2 dir in dirs)
                {
                    GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
                    Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
                    if (rb != null) rb.linearVelocity = dir.normalized * bulletSpeed;
                    activeSkillObjects.Add(bullet);
                }

                patternIndex++;
                fireTimer = 0f;
            }

            yield return null;
        }

        Destroy(leftLaser);
        Destroy(rightLaser);
        activeSkillObjects.Remove(leftLaser);
        activeSkillObjects.Remove(rightLaser);

        yield return StartCoroutine(SkillEndDelay());
    }


    private void SetupLaser(LineRenderer lr, Color color)
    {
        lr.positionCount = 2;
        lr.startWidth = 0.15f;
        lr.endWidth = 0.15f;
        lr.material = laserMaterial != null ? laserMaterial : new Material(Shader.Find("Sprites/Default"));
        lr.startColor = color;
        lr.endColor = color;

        // 발광 효과 (Emission)
        if (lr.material.HasProperty("_EmissionColor"))
            lr.material.SetColor("_EmissionColor", color * 2f);
    }

    private void CheckLaserHit(LineRenderer lr)
    {
        RaycastHit2D[] hits = Physics2D.LinecastAll(
            lr.GetPosition(0),
            lr.GetPosition(1),
            LayerMask.GetMask("Player")
        );

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider.CompareTag("Player"))
            {
                Vector3 enemyPosition = transform.position;
                // 플레이어가 데미지 입는 로직 (게임 매니저 인스턴스 사용)
                GameManager.Instance.playerDamaged.TakeDamage(laserDamage, enemyPosition);
            }
        }
    }

    // ────────── 스킬 3: 검 휘두르기 (1초 경고가 이미 구현되어 있음) ──────────
    private IEnumerator SkillSwordPattern()
    {
        if (mapCollider == null)
        {
            Debug.LogWarning("mapCollider가 설정되지 않았습니다!");
            yield return StartCoroutine(SkillEndDelay());
            yield break;
        }

        float radius = Mathf.Max(mapCollider.bounds.size.x, mapCollider.bounds.size.y) / 2f;
        Vector3 center = transform.position;
        List<GameObject> activeWarnings = new List<GameObject>();

        // ⚠️ 1단계: 경고 표시 (swordWarningDuration = 1.0f)
        if (warningPrefab != null)
        {
            float currentWarningAngle = swordStartAngle;
            Quaternion rotA = Quaternion.Euler(0, 0, currentWarningAngle);
            Quaternion rotB = Quaternion.Euler(0, 0, currentWarningAngle + 180f);
            float finalLength = radius * 2f * warningLengthScale;

            GameObject warningA = Instantiate(warningPrefab, center, rotA);
            warningA.transform.localScale = new Vector3(finalLength, warningThicknessScale, warningThicknessScale);
            activeWarnings.Add(warningA);
            activeSkillObjects.Add(warningA);

            GameObject warningB = Instantiate(warningPrefab, center, rotB);
            warningB.transform.localScale = new Vector3(finalLength, warningThicknessScale, warningThicknessScale);
            activeWarnings.Add(warningB);
            activeSkillObjects.Add(warningB);

            // ⚡ 1초 대기 (swordWarningDuration)
            yield return new WaitForSeconds(swordWarningDuration);

            foreach (var warning in activeWarnings)
            {
                if (warning != null) Destroy(warning);
                activeSkillObjects.Remove(warning);
            }
            activeWarnings.Clear();
        }

        // ⚔️ 2단계: 실제 레이저 회전 시작
        GameObject laserA = new GameObject("RotatingLaserA");
        LineRenderer lrA = laserA.AddComponent<LineRenderer>();
        SetupLaser(lrA, Color.red); // 빨간색 레이저

        GameObject laserB = new GameObject("RotatingLaserB");
        LineRenderer lrB = laserB.AddComponent<LineRenderer>();
        SetupLaser(lrB, Color.red); // 빨간색 레이저

        lrA.sortingLayerName = "Foreground";
        lrB.sortingLayerName = "Foreground";
        lrA.sortingOrder = 10;
        lrB.sortingOrder = 10;

        activeSkillObjects.Add(laserA);
        activeSkillObjects.Add(laserB);

        float currentAngle = swordStartAngle;
        float elapsed = 0f;
        float rotateDuration = 360f / swordRotateSpeed;

        while (elapsed < rotateDuration)
        {
            currentAngle += swordRotateSpeed * Time.deltaTime;
            float rad = currentAngle * Mathf.Deg2Rad;

            Vector3 dirA = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0);
            Vector3 endA = center + dirA * radius;
            lrA.SetPosition(0, center);
            lrA.SetPosition(1, endA);

            Vector3 dirB = -dirA;
            Vector3 endB = center + dirB * radius;
            lrB.SetPosition(0, center);
            lrB.SetPosition(1, endB);

            CheckLaserDamage(center, dirA, radius);
            CheckLaserDamage(center, dirB, radius);

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(laserA);
        Destroy(laserB);
        activeSkillObjects.Remove(laserA);
        activeSkillObjects.Remove(laserB);

        yield return StartCoroutine(SkillEndDelay());
    }

    private void CheckLaserDamage(Vector3 start, Vector3 dir, float distance)
    {
        RaycastHit2D hit = Physics2D.Raycast(start, dir, distance, LayerMask.GetMask("Player"));
        if (hit.collider != null && hit.collider.CompareTag("Player"))
        {
            Vector3 enemyPosition = transform.position;
            GameManager.Instance.playerDamaged.TakeDamage(laserDamage, enemyPosition);
        }
    }

    // ────────── 스킬 4: 점프 후 원형탄 ──────────
    private IEnumerator SkillJumpAndShoot()
    {
        Vector3 startPos = transform.position;
        Vector3 peakPos = startPos + Vector3.up * jumpHeight;
        List<GameObject> activeWarnings = new List<GameObject>();

        yield return transform.DOMove(peakPos, jumpDuration).SetEase(Ease.OutQuad).WaitForCompletion();

        if (warningPrefab != null)
        {
            float step = 360f / jumpBulletCount;
            for (int i = 0; i < jumpBulletCount; i++)
            {
                float rotationZ = step * i;
                float angleRad = rotationZ * Mathf.Deg2Rad;
                Vector3 shotDir = new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0);
                Vector3 warningPos = startPos + shotDir * warningOffsetDistance;

                GameObject warning = Instantiate(warningPrefab, warningPos, Quaternion.Euler(0, 0, rotationZ));
                warning.transform.localScale = new Vector3(warningLengthScale, warningThicknessScale, warningThicknessScale);

                activeWarnings.Add(warning);
                activeSkillObjects.Add(warning);
            }
        }

        yield return transform.DOMove(startPos, jumpDuration).SetEase(Ease.InQuad).WaitForCompletion();

        foreach (var warning in activeWarnings)
        {
            if (warning != null) Destroy(warning);
            activeSkillObjects.Remove(warning);
        }
        activeWarnings.Clear();

        Vector3 origin = transform.position;
        float stepAngle = 360f / jumpBulletCount;

        for (int i = 0; i < jumpBulletCount; i++)
        {
            float angle = stepAngle * i * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            GameObject bullet = Instantiate(bulletPrefab, origin, Quaternion.identity);
            Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = dir * jumpBulletSpeed;
            activeSkillObjects.Add(bullet);
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
        foreach (var obj in activeSkillObjects)
        {
            if (obj != null) Destroy(obj);
        }
        activeSkillObjects.Clear();
    }

    public void SetDead()
    {
        isLive = false;
        ClearAllSkillObjects();
    }
}