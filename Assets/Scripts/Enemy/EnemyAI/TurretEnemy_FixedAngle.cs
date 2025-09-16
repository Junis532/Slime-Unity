using UnityEngine;

public class TurretEnemy_FixedAngle : EnemyBase
{
    private bool isLive = true;
    private SpriteRenderer spriter;
    private EnemyAnimation enemyAnimation;

    [Header("발사 설정")]
    public float fireRange = 5f;
    public float fireCooldown = 1.5f;
    private float lastFireTime;

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

        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.red;
        lineRenderer.endColor = Color.red;
        lineRenderer.sortingOrder = 7;
        lineRenderer.sortingLayerName = "Default";
    }

    void Update()
    {
        if (!isLive) return;

        float rad = fixedAngle * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;

        // 라인 표시
        if (!lineRenderer.enabled) lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, (Vector2)transform.position + dir * fireRange);

        if (Time.time - lastFireTime >= fireCooldown && !isPreparingToFire)
        {
            StartCoroutine(PrepareAndShoot(dir));
        }

        enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);
    }

    private System.Collections.IEnumerator PrepareAndShoot(Vector2 dir)
    {
        isPreparingToFire = true;

        float duration = 1f;
        float timer = 0f;

        float startWidth = 0.05f;
        Color startColor = Color.red;

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

        Shoot(dir);
        lastFireTime = Time.time;

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
