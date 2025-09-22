using UnityEngine;

public class TurretEnemy_FixedAngle : EnemyBase
{
    private bool isLive = true;
    private SpriteRenderer spriter;
    private EnemyAnimation enemyAnimation;

    [Header("발사 범위 / 라인 표시")]
    public float fireRange = 5f;

    [Header("발사 간격 (순환)")]
    public float[] fireIntervals = { 1f, 3f, 2f }; // 각 탄마다 발사 대기 시간
    private int fireIndex = 0;
    private float lastFireTime;

    [Header("첫 발사 딜레이")]
    public float firstFireDelay = 2f; // 첫 발사는 2초 뒤 실행

    [Header("탄환 설정")]
    public GameObject bulletPrefab;
    public float bulletSpeed = 1.5f;
    public float bulletLifetime = 3f;

    private LineRenderer lineRenderer;
    private bool isPreparingToFire = false;

    [Header("고정 발사 각도 (도 단위)")]
    [Range(0f, 360f)]
    public float fixedAngle = 0f;

    void Start()
    {
        spriter = GetComponent<SpriteRenderer>();
        enemyAnimation = GetComponent<EnemyAnimation>();

        originalSpeed = GameManager.Instance.longRangeEnemyStats.speed;
        speed = originalSpeed;

        // LineRenderer 세팅
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.enabled = false;

        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.red;
        lineRenderer.endColor = Color.red;
        lineRenderer.sortingOrder = 2;
        lineRenderer.sortingLayerName = "Default";

        // 시작 시 첫 발사 시간 = 현재 시간 + firstFireDelay
        lastFireTime = Time.time - fireIntervals[0] + firstFireDelay;
    }

    void Update()
    {
        if (!isLive) return;

        // 현재 고정 각도 → 방향 벡터 변환
        float rad = fixedAngle * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;

        // 라인 표시
        if (!lineRenderer.enabled) lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, (Vector2)transform.position + dir * fireRange);

        // 현재 발사 대기 시간
        float currentCooldown = fireIntervals[fireIndex % fireIntervals.Length];

        // 첫 발사 딜레이 포함한 발사 조건
        if (Time.time - lastFireTime >= currentCooldown && !isPreparingToFire)
        {
            StartCoroutine(PrepareAndShoot(dir));
        }

        enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);
    }

    private System.Collections.IEnumerator PrepareAndShoot(Vector2 dir)
    {
        isPreparingToFire = true;

        float duration = 1f; // 발사 준비 시간
        float timer = 0f;

        float startWidth = 0.1f;
        Color startColor = Color.red;

        // 준비 동안 라인 가늘어지고 투명해짐
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;

            float width = Mathf.Lerp(startWidth, 0f, t);
            lineRenderer.startWidth = width;
            lineRenderer.endWidth = width;

            Color fadeColor = Color.Lerp(startColor, new Color(startColor.r, startColor.g, startColor.b, 0f), t);
            lineRenderer.startColor = fadeColor;
            lineRenderer.endColor = fadeColor;

            yield return null;
        }

        lineRenderer.enabled = false;

        // 발사
        Shoot(dir);
        lastFireTime = Time.time;

        // 다음 쿨다운으로 이동
        fireIndex = (fireIndex + 1) % fireIntervals.Length;

        yield return new WaitForSeconds(0.3f);

        if (isLive)
        {
            lineRenderer.startWidth = startWidth;
            lineRenderer.endWidth = startWidth;
            lineRenderer.startColor = startColor;
            lineRenderer.endColor = startColor;
            lineRenderer.enabled = true;
        }

        isPreparingToFire = false;
    }

    void Shoot(Vector2 dir)
    {
        GameObject bullet = PoolManager.Instance.SpawnFromPool(bulletPrefab.name, transform.position, Quaternion.identity);

        if (bullet != null)
        {
            BulletBehavior bulletBehavior = bullet.GetComponent<BulletBehavior>();
            if (bulletBehavior == null)
                bulletBehavior = bullet.AddComponent<BulletBehavior>();

            bulletBehavior.Initialize(dir.normalized, bulletSpeed, bulletLifetime);
        }
    }

    private void OnDestroy()
    {
        if (lineRenderer != null)
            Destroy(lineRenderer);

        isLive = false;
    }
}
