using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

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
                {
                    Debug.LogWarning("RC 00 안에 BoxCollider2D가 없습니다!");
                }
            }
            else
            {
                Debug.LogWarning("RC 00 오브젝트를 찾을 수 없습니다!");
            }
        }
    }

    void Update()
    {
        if (!isLive) return;
        if (isSkillPlaying) return;

        skillTimer += Time.deltaTime;
        if (skillTimer >= skillInterval)
        {
            skillTimer = 0f;
            currentSkillIndex = Random.Range(0, 3);
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
        }
    }

    // ────────── 스킬 1: 회전 탄막 ──────────
    private IEnumerator SkillBulletCircle()
    {
        float duration = 5f;
        float fireInterval = 0.5f;
        float elapsed = 0f;
        float currentAngleOffset = 0f;
        float rotateOffsetPerWave = bulletAngle;

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

                activeSkillObjects.Add(go); // 추적 리스트에 등록
            }

            currentAngleOffset += rotateOffsetPerWave;
            yield return new WaitForSeconds(fireInterval);
            elapsed += fireInterval;
        }

        yield return StartCoroutine(SkillEndDelay());
    }

    // ────────── 스킬 2: 좌우 레이저 ──────────
    private IEnumerator SkillLaserPattern()
    {
        if (mapCollider == null)
        {
            Debug.LogWarning("mapCollider가 설정되지 않았습니다!");
            yield return StartCoroutine(SkillEndDelay());
            yield break;
        }

        Bounds bounds = mapCollider.bounds;
        Vector3 leftPos = new Vector3(bounds.min.x, transform.position.y, 0f);
        Vector3 rightPos = new Vector3(bounds.max.x, transform.position.y, 0f);

        // 레이저 생성
        GameObject leftLaser = new GameObject("LeftLaser");
        LineRenderer leftLR = leftLaser.AddComponent<LineRenderer>();
        SetupLaser(leftLR, Color.red);
        leftLR.SetPosition(0, leftPos + Vector3.up * bounds.extents.y);
        leftLR.SetPosition(1, leftPos - Vector3.up * bounds.extents.y);

        GameObject rightLaser = new GameObject("RightLaser");
        LineRenderer rightLR = rightLaser.AddComponent<LineRenderer>();
        SetupLaser(rightLR, Color.red);
        rightLR.SetPosition(0, rightPos + Vector3.up * bounds.extents.y);
        rightLR.SetPosition(1, rightPos - Vector3.up * bounds.extents.y);

        activeSkillObjects.Add(leftLaser);
        activeSkillObjects.Add(rightLaser);

        float moveDistance = 5f;
        float moveDuration = 1f;
        float elapsed = 0f;
        float checkInterval = 0.05f;

        Vector3 leftStart0 = leftLR.GetPosition(0);
        Vector3 leftStart1 = leftLR.GetPosition(1);
        Vector3 rightStart0 = rightLR.GetPosition(0);
        Vector3 rightStart1 = rightLR.GetPosition(1);

        // 십자탄 발사
        Vector2[] crossDirs = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        foreach (Vector2 dir in crossDirs)
        {
            GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
            Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = dir.normalized * bulletSpeed;
            activeSkillObjects.Add(bullet);
        }

        // 🔥 1초 후 X자탄 발사
        StartCoroutine(FireXPattern(1f));

        while (elapsed < moveDuration)
        {
            float t = elapsed / moveDuration;
            leftLR.SetPosition(0, leftStart0 + Vector3.right * moveDistance * t);
            leftLR.SetPosition(1, leftStart1 + Vector3.right * moveDistance * t);
            rightLR.SetPosition(0, rightStart0 - Vector3.right * moveDistance * t);
            rightLR.SetPosition(1, rightStart1 - Vector3.right * moveDistance * t);

            // 플레이어 충돌 체크
            CheckLaserHit(leftLR);
            CheckLaserHit(rightLR);

            elapsed += checkInterval;
            yield return new WaitForSeconds(checkInterval);
        }

        yield return new WaitForSeconds(3f);

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
        RaycastHit2D[] hits = Physics2D.LinecastAll(lr.GetPosition(0), lr.GetPosition(1), LayerMask.GetMask("Player"));
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider.CompareTag("Player"))
                GameManager.Instance.playerDamaged.TakeDamage(laserDamage);
        }
    }

    // 🔥 X자 탄막 코루틴
    private IEnumerator FireXPattern(float delay)
    {
        yield return new WaitForSeconds(delay);

        Vector2[] diagDirs = {
            new Vector2(1, 1).normalized,
            new Vector2(-1, 1).normalized,
            new Vector2(1, -1).normalized,
            new Vector2(-1, -1).normalized
        };

        foreach (Vector2 dir in diagDirs)
        {
            GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
            Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = dir * bulletSpeed;
            activeSkillObjects.Add(bullet);
        }
    }

    // ────────── 스킬 3: 검 휘두르기 ──────────
    [Header("검 휘두르기 패턴 설정")]
    public float swordRotateSpeed = 360f;
    public float swordStartAngle = 180f;
    private IEnumerator SkillSwordPattern()
    {
        if (mapCollider == null)
        {
            Debug.LogWarning("mapCollider가 설정되지 않았습니다!");
            yield return StartCoroutine(SkillEndDelay());
            yield break;
        }

        float radius = Mathf.Max(mapCollider.bounds.size.x, mapCollider.bounds.size.y) / 2f;
        GameObject lineObj = new GameObject("SwordLine");
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        SetupLaser(lr, Color.yellow);
        lr.sortingLayerName = "Foreground";
        lr.sortingOrder = 10;
        activeSkillObjects.Add(lineObj);

        float currentAngle = swordStartAngle;
        float elapsed = 0f;
        float rotateDuration = 360f / swordRotateSpeed;

        while (elapsed < rotateDuration)
        {
            currentAngle += swordRotateSpeed * Time.deltaTime;
            float rad = currentAngle * Mathf.Deg2Rad;
            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0) * radius + Vector3.back * 0.1f;
            lr.SetPosition(0, startPos);
            lr.SetPosition(1, endPos);

            RaycastHit2D hit = Physics2D.Raycast(startPos, (endPos - startPos).normalized, radius, LayerMask.GetMask("Player"));
            if (hit.collider != null && hit.collider.CompareTag("Player"))
            {
                GameManager.Instance.playerDamaged.TakeDamage(laserDamage);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(lineObj);
        activeSkillObjects.Remove(lineObj);

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
