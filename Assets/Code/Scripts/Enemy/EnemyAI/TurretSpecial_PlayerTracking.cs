using DG.Tweening;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TurretSpecial_PlayerTracking : MonoBehaviour
{
    private bool isLive = true;
    private SpriteRenderer spriter;
    private TurretEnemyAnimation enemyAnimation;
    private Coroutine attackRoutine;

    [Header("공격 애니메이션 준비 시간")]
    public float attackPrepareDuration = 0.5f;

    [Header("발사 쿨다운 설정 (순환)")]
    public float[] fireIntervals = { 1f, 3f, 2f };
    private int fireIndex = 0;
    private float lastFireTime;

    [Header("첫 발사 딜레이")]
    public float firstFireDelay = 2f;

    [Header("탄환 설정")]
    public GameObject bulletPrefab;
    private float bulletSpeed = 1.2f;
    public float bulletLifetime = 3f;

    [Header("두 번째 Bullet 설정")]
    public GameObject secondaryBulletPrefab;
    [Header("두 번째 Bullet 속도 변경")]
    public float secondaryDelay = 1f;
    public float secondarySpeed = 2f;

    [Header("탄환 패턴 설정")]
    [Range(1, 10)] public int bulletCount = 1;
    [Range(0f, 180f)] public float spreadAngle = 0f;

    [Header("LineRenderer 설정")]
    public bool showLineRenderer = true;
    private LineRenderer lineRenderer;
    private bool isPreparingToFire = false;

    // 🔹 발사 순서 제어 (1, 1, 2 반복)
    private int bulletPatternIndex = 0; // 0 → bullet1, 1 → bullet1, 2 → bullet2

    void Start()
    {
        spriter = GetComponent<SpriteRenderer>();
        enemyAnimation = GetComponent<TurretEnemyAnimation>();
        if (!enemyAnimation) Debug.LogError("TurretEnemyAnimation 컴포넌트를 지정하세요.");

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

        if (fireIntervals.Length > 0)
            lastFireTime = Time.time - fireIntervals[0] + firstFireDelay;

        if (enemyAnimation != null)
            enemyAnimation.PlayAnimation(TurretEnemyAnimation.State.Idle);
    }
    void Update()
    {
        if (!isLive) return;

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            if (lineRenderer != null)
                lineRenderer.enabled = false;
            enemyAnimation?.PlayAnimation(TurretEnemyAnimation.State.Idle);
            return;
        }

        Vector2 toPlayer = player.transform.position - transform.position;
        Vector2 dir = toPlayer.normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // 좌우 반전
        if (Mathf.Abs(toPlayer.x) > 0.01f)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (toPlayer.x < 0 ? -1 : 1);
            transform.localScale = scale;
        }

        if (!isPreparingToFire && enemyAnimation != null)
            enemyAnimation.PlayAnimation(TurretEnemyAnimation.State.Idle, angle);

        if (showLineRenderer && lineRenderer != null)
        {
            lineRenderer.enabled = true;
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, player.transform.position);
        }
        else if (lineRenderer != null)
            lineRenderer.enabled = false;

        if (fireIntervals.Length == 0) return;
        float currentCooldown = fireIntervals[fireIndex % fireIntervals.Length];

        if (Time.time - lastFireTime >= currentCooldown && !isPreparingToFire)
        {
            if (attackRoutine != null) StopCoroutine(attackRoutine);
            attackRoutine = StartCoroutine(PrepareAndShoot());
        }
    }

    private IEnumerator PrepareAndShoot()
    {
        isPreparingToFire = true;
        float totalPrepTime = fireIntervals[fireIndex % fireIntervals.Length];

        GameObject player = GameObject.FindWithTag("Player");
        float initialAngle = 0;
        if (player != null)
        {
            Vector2 toPlayer = player.transform.position - transform.position;
            initialAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
        }

        TurretEnemyAnimation.State prepareState = GetPrepareState(initialAngle);
        enemyAnimation?.PlayAnimation(prepareState);

        if (spriter != null)
        {
            spriter.DOKill();
            spriter.DOColor(Color.red, totalPrepTime);
        }

        if (totalPrepTime > 0)
            yield return new WaitForSeconds(totalPrepTime);

        player = GameObject.FindWithTag("Player");
        Vector2 finalDir = Vector2.right;
        float finalAngle = 0f;
        if (player != null)
        {
            Vector2 toPlayer = player.transform.position - transform.position;
            finalDir = toPlayer.normalized;
            finalAngle = Mathf.Atan2(finalDir.y, finalDir.x) * Mathf.Rad2Deg;
        }

        Shoot(finalDir, finalAngle);

        if (spriter != null)
        {
            spriter.DOKill();
            spriter.DOColor(Color.white, 0.1f);
        }

        lastFireTime = Time.time;
        fireIndex = (fireIndex + 1) % fireIntervals.Length;
        isPreparingToFire = false;
        attackRoutine = null;
    }

    private TurretEnemyAnimation.State GetPrepareState(float angle)
    {
        angle = (angle % 360 + 360) % 360;
        float verticalTolerance = 25f;
        if ((angle >= 90f - verticalTolerance && angle <= 90f + verticalTolerance) ||
            (angle >= 270f - verticalTolerance && angle <= 270f + verticalTolerance))
            return TurretEnemyAnimation.State.FrontShootPrepare;
        return TurretEnemyAnimation.State.ShootPrepare;
    }

    private void Shoot(Vector2 centerDir, float centerAngle)
    {
        if ((!bulletPrefab && !secondaryBulletPrefab) || bulletCount <= 0) return;

        float startAngle = spreadAngle <= 0.01f ? centerAngle : centerAngle - spreadAngle / 2f;
        float angleStep = bulletCount > 1 ? spreadAngle / (bulletCount - 1) : 0f;

        // 🔹 발사 순서 결정: 0,1 → bulletPrefab / 2 → secondaryBulletPrefab
        GameObject bulletToShoot = bulletPrefab;
        if (bulletPatternIndex == 2 && secondaryBulletPrefab != null)
            bulletToShoot = secondaryBulletPrefab;

        for (int i = 0; i < bulletCount; i++)
        {
            float currentAngle = startAngle + i * angleStep;
            Vector2 shotDir = new Vector2(Mathf.Cos(currentAngle * Mathf.Deg2Rad),
                                          Mathf.Sin(currentAngle * Mathf.Deg2Rad));
            SpawnBullet(shotDir, bulletToShoot);
            GameManager.Instance.audioManager.PlayerTurretShootingSound(1.5f);
        }

        // 다음 순서로 이동 (0→1→2→0...)
        bulletPatternIndex = (bulletPatternIndex + 1) % 3;
    }

    private void SpawnBullet(Vector2 dir, GameObject prefab)
    {
        if (!prefab) return;

        GameObject bullet = Instantiate(prefab, transform.position, Quaternion.identity);
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb)
            rb.linearVelocity = dir.normalized * bulletSpeed;

        // 🔹 secondaryBulletPrefab인 경우, 일정 시간 후 속도 변경
        if (prefab == secondaryBulletPrefab && secondaryDelay > 0f && rb != null)
            StartCoroutine(ChangeBulletSpeed(rb, secondaryDelay, secondarySpeed));

        Destroy(bullet, bulletLifetime);
    }

    private IEnumerator ChangeBulletSpeed(Rigidbody2D rb, float delay, float newSpeed)
    {
        yield return new WaitForSeconds(delay);
        if (rb != null)
            rb.linearVelocity = rb.linearVelocity.normalized * newSpeed;
    }

    private void OnDestroy()
    {
        if (lineRenderer != null)
            Destroy(lineRenderer);
        spriter?.DOKill();
        if (attackRoutine != null)
            StopCoroutine(attackRoutine);
        isLive = false;
    }
}
