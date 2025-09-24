using DG.Tweening;
using UnityEngine;

public class TurretEnemy_FixedAngle : EnemyBase
{
    private bool isLive = true;
    private SpriteRenderer spriter;
    private EnemyAnimation enemyAnimation;

    [Header("발사 범위 / 라인 표시")]
    public float fireRange = 5f;

    [Header("발사 간격 (순환)")]
    public float[] fireIntervals = { 1f, 3f, 2f };
    private int fireIndex = 0;
    private float lastFireTime;

    [Header("첫 발사 딜레이")]
    public float firstFireDelay = 2f;

    [Header("탄환 설정")]
    public GameObject bulletPrefab;
    public float bulletSpeed = 1.5f;
    public float bulletLifetime = 3f;

    [Header("LineRenderer 설정")]
    public bool showLineRenderer = true; // 여기서 켜고 끔
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
        lineRenderer.enabled = showLineRenderer;

        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.red;
        lineRenderer.endColor = Color.red;
        lineRenderer.sortingOrder = 2;
        lineRenderer.sortingLayerName = "Default";

        lastFireTime = Time.time - fireIntervals[0] + firstFireDelay;
    }

    void Update()
    {
        if (!isLive) return;

        float rad = fixedAngle * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;

        // LineRenderer 켜기/끄기 체크
        if (lineRenderer != null)
            lineRenderer.enabled = showLineRenderer;

        if (showLineRenderer && lineRenderer.enabled)
        {
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, (Vector2)transform.position + dir * fireRange);
        }

        float currentCooldown = fireIntervals[fireIndex % fireIntervals.Length];

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

        // 본체 색이 흰색 → 빨강으로 변함
        if (spriter != null)
        {
            spriter.DOColor(Color.red, duration);
        }

        // 발사 준비 대기
        yield return new WaitForSeconds(duration);

        // 발사
        Shoot(dir);
        lastFireTime = Time.time;
        fireIndex = (fireIndex + 1) % fireIntervals.Length;

        // 발사 후 본체 색 다시 흰색으로 복귀
        if (spriter != null)
        {
            spriter.DOColor(Color.white, 0.2f); // 0.2초 동안 자연스럽게
        }

        // 발사 후 잠시 대기
        yield return new WaitForSeconds(0.3f);

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
