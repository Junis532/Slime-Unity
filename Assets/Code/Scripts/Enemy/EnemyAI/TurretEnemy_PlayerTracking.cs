using DG.Tweening;
using UnityEngine;

public class TurretEnemy_PlayerTracking : EnemyBase
{
    private bool isLive = true;
    private SpriteRenderer spriter;
    private EnemyAnimation enemyAnimation;

    [Header("발사 쿨다운 설정 (순환)")]
    public float[] fireIntervals = { 1f, 3f, 2f };
    private int fireIndex = 0;
    private float lastFireTime;

    [Header("첫 발사 딜레이")]
    public float firstFireDelay = 2f; // 게임 시작 후 첫 발사까지 기다릴 시간

    [Header("탄환 설정")]
    public GameObject bulletPrefab;
    public float bulletSpeed = 1.5f;
    public float bulletLifetime = 3f;

    private LineRenderer lineRenderer;
    private bool isPreparingToFire = false;

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

        // 첫 발사 딜레이 적용
        lastFireTime = Time.time - fireIntervals[0] + firstFireDelay;
    }

    void Update()
    {
        if (!isLive) return;

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            lineRenderer.enabled = false;
            return;
        }

        Vector2 toPlayer = player.transform.position - transform.position;

        // 좌우 반전
        if (toPlayer.x != 0)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (toPlayer.x < 0 ? -1 : 1);
            transform.localScale = scale;
        }

        // 발사 준비 중이든 아니든 선은 계속 보여준다
        if (!lineRenderer.enabled)
            lineRenderer.enabled = true;

        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, player.transform.position);

        // 현재 사용할 쿨다운
        float currentCooldown = fireIntervals[fireIndex];

        if (Time.time - lastFireTime >= currentCooldown && !isPreparingToFire)
        {
            StartCoroutine(PrepareAndShoot());
        }

        enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);
    }

    private System.Collections.IEnumerator PrepareAndShoot()
    {
        isPreparingToFire = true;

        float duration = 1f; // 준비 시간
        float timer = 0f;

        float startWidth = 0.1f;
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

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            Vector2 dir = (player.transform.position - transform.position).normalized;
            Shoot(dir);

            fireIndex = (fireIndex + 1) % fireIntervals.Length;
            lastFireTime = Time.time;
        }

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
