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

    // ────────── 패턴 3: 검 ──────────
    [Header("검 휘두르기 패턴 설정")]
    public float swordRotateSpeed = 360f;
    public float swordStartAngle = 180f;

    // ────────── 패턴 4: 점프 후 원형탄 ──────────
    [Header("점프 후 원형탄 패턴")]
    public float jumpHeight = 5f;        // 점프 높이
    public float jumpDuration = 0.5f;    // 점프 시간
    public int jumpBulletCount = 8;      // 원형 탄환 개수 (인스펙터 조절)
    public float jumpBulletSpeed = 6f;   // 탄환 속도

    // 🔥 생성된 오브젝트 추적 리스트
    private List<GameObject> activeSkillObjects = new List<GameObject>();

    void Start()
    {
        spriter = GetComponent<SpriteRenderer>();
        enemyAnimation = GetComponent<EnemyAnimation>();

        // ── 맵 콜라이더 자동 찾기 ──
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
            currentSkillIndex = Random.Range(0, 4); // ✅ 0~3까지 (4가지 스킬)
            UseRandomSkill();
        }
    }

    private void UseRandomSkill()
    {
        isSkillPlaying = true;
        switch (currentSkillIndex)
        {
            case 0:
                StartCoroutine(SkillBulletCircle());
                break;
            case 1:
                StartCoroutine(SkillLaserPattern());
                break;
            case 2:
                StartCoroutine(SkillSwordPattern());
                break;
            case 3:
                StartCoroutine(SkillJumpAndShoot());
                break;
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

    // ────────── 스킬 2: 레이저 + X/Y 탄막 반복 ──────────
    private IEnumerator SkillLaserPattern()
    {
        if (mapCollider == null)
        {
            Debug.LogWarning("mapCollider가 설정되지 않았습니다!");
            yield return StartCoroutine(SkillEndDelay());
            yield break;
        }

        Bounds bounds = mapCollider.bounds;

        // 레이저 생성
        GameObject leftLaser = new GameObject("LeftLaser");
        LineRenderer leftLR = leftLaser.AddComponent<LineRenderer>();
        SetupLaser(leftLR, Color.red);
        GameObject rightLaser = new GameObject("RightLaser");
        LineRenderer rightLR = rightLaser.AddComponent<LineRenderer>();
        SetupLaser(rightLR, Color.red);

        activeSkillObjects.Add(leftLaser);
        activeSkillObjects.Add(rightLaser);

        float minDistance = bounds.extents.x * 0.3f;
        float maxDistance = bounds.extents.x;
        float pulseSpeed = 7f;
        float laserElapsed = 0f;
        float laserActiveDuration = 8f;

        string[] patternSequence = { "X", "Y", "X", "Y", "X", "Y" };
        int patternIndex = 0;
        float fireInterval = 0.5f;
        float fireTimer = 0f;

        while (laserElapsed < laserActiveDuration)
        {
            laserElapsed += Time.deltaTime;
            fireTimer += Time.deltaTime;

            float offset = Mathf.PingPong(Time.time * pulseSpeed, maxDistance - minDistance) + minDistance;
            leftLR.SetPosition(0, new Vector3(transform.position.x - offset, transform.position.y + bounds.extents.y, 0));
            leftLR.SetPosition(1, new Vector3(transform.position.x - offset, transform.position.y - bounds.extents.y, 0));
            rightLR.SetPosition(0, new Vector3(transform.position.x + offset, transform.position.y + bounds.extents.y, 0));
            rightLR.SetPosition(1, new Vector3(transform.position.x + offset, transform.position.y - bounds.extents.y, 0));

            CheckLaserHit(leftLR);
            CheckLaserHit(rightLR);

            if (fireTimer >= fireInterval)
            {
                string pattern = patternSequence[patternIndex % patternSequence.Length];
                if (pattern == "X")
                {
                    Vector2[] diagDirs = {
                        new Vector2(1,1).normalized, new Vector2(-1,1).normalized,
                        new Vector2(1,-1).normalized, new Vector2(-1,-1).normalized
                    };
                    foreach (Vector2 dir in diagDirs)
                    {
                        GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
                        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
                        if (rb != null) rb.linearVelocity = dir * bulletSpeed;
                        activeSkillObjects.Add(bullet);
                    }
                }
                else
                {
                    Vector2[] crossDirs = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
                    foreach (Vector2 dir in crossDirs)
                    {
                        GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
                        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
                        if (rb != null) rb.linearVelocity = dir * bulletSpeed;
                        activeSkillObjects.Add(bullet);
                    }
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
        lr.startWidth = 0.1f;
        lr.endWidth = 0.1f;
        lr.material = laserMaterial != null ? laserMaterial : new Material(Shader.Find("Sprites/Default"));
        lr.startColor = color;
        lr.endColor = color;
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
                JoystickDirectionIndicator indicator = hit.collider.GetComponent<JoystickDirectionIndicator>();
                if (indicator != null && indicator.IsUsingSkill)
                    continue;

                GameManager.Instance.playerDamaged.TakeDamage(laserDamage);
            }
        }
    }

    // ────────── 스킬 3: 검 휘두르기 ──────────
    // ────────── 스킬 3: 회전 레이저 ──────────
    private IEnumerator SkillSwordPattern()
    {
        if (mapCollider == null)
        {
            Debug.LogWarning("mapCollider가 설정되지 않았습니다!");
            yield return StartCoroutine(SkillEndDelay());
            yield break;
        }

        // 🔹 맵 크기에 맞춰 레이저 길이 설정
        float radius = Mathf.Max(mapCollider.bounds.size.x, mapCollider.bounds.size.y) / 2f;

        // 🔹 레이저 2개 생성 (양쪽 방향)
        GameObject laserA = new GameObject("RotatingLaserA");
        LineRenderer lrA = laserA.AddComponent<LineRenderer>();
        SetupLaser(lrA, Color.cyan);

        GameObject laserB = new GameObject("RotatingLaserB");
        LineRenderer lrB = laserB.AddComponent<LineRenderer>();
        SetupLaser(lrB, Color.cyan);

        lrA.sortingLayerName = "Foreground";
        lrB.sortingLayerName = "Foreground";
        lrA.sortingOrder = 10;
        lrB.sortingOrder = 10;

        activeSkillObjects.Add(laserA);
        activeSkillObjects.Add(laserB);

        // 🔹 회전 관련 변수
        float currentAngle = swordStartAngle;
        float elapsed = 0f;
        float rotateDuration = 360f / swordRotateSpeed; // 1바퀴 도는 시간

        while (elapsed < rotateDuration)
        {
            // 각도 갱신
            currentAngle += swordRotateSpeed * Time.deltaTime;
            float rad = currentAngle * Mathf.Deg2Rad;

            Vector3 center = transform.position;

            // 🔹 첫 번째 레이저 (정방향)
            Vector3 dirA = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0);
            Vector3 endA = center + dirA * radius;
            lrA.SetPosition(0, center);
            lrA.SetPosition(1, endA);

            // 🔹 두 번째 레이저 (반대방향)
            Vector3 dirB = -dirA;
            Vector3 endB = center + dirB * radius;
            lrB.SetPosition(0, center);
            lrB.SetPosition(1, endB);

            // 🔸 충돌 판정
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

    // 🔸 레이저 데미지 체크 함수 (보조)
    private void CheckLaserDamage(Vector3 start, Vector3 dir, float distance)
    {
        RaycastHit2D hit = Physics2D.Raycast(start, dir, distance, LayerMask.GetMask("Player"));
        if (hit.collider != null && hit.collider.CompareTag("Player"))
        {
            GameManager.Instance.playerDamaged.TakeDamage(laserDamage);
        }
    }


    // ────────── 스킬 4: 점프 후 원형탄 ──────────
    private IEnumerator SkillJumpAndShoot()
    {
        Vector3 startPos = transform.position;
        Vector3 peakPos = startPos + Vector3.up * jumpHeight;

        // 점프
        yield return transform.DOMove(peakPos, jumpDuration)
            .SetEase(Ease.OutQuad)
            .WaitForCompletion();

        // 착지
        yield return transform.DOMove(startPos, jumpDuration)
            .SetEase(Ease.InQuad)
            .WaitForCompletion();

        // 착지 순간 원형탄 1회 발사
        Vector3 origin = transform.position;
        float step = 360f / jumpBulletCount;

        for (int i = 0; i < jumpBulletCount; i++)
        {
            float angle = step * i * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            GameObject bullet = Instantiate(bulletPrefab, origin, Quaternion.identity);
            Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = dir * jumpBulletSpeed;
            activeSkillObjects.Add(bullet);
        }

        yield return StartCoroutine(SkillEndDelay());
    }

    // ────────── 공통 종료 ──────────
    private IEnumerator SkillEndDelay()
    {
        yield return new WaitForSeconds(1f);
        isSkillPlaying = false;
    }

    // 🔥 보스 죽을 때 스킬 오브젝트 정리
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