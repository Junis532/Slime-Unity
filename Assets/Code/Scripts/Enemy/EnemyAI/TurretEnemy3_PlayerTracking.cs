using DG.Tweening;
using UnityEngine;

public class TurretEnemy3_PlayerTracking : EnemyBase
{
    private bool isLive = true;
    private SpriteRenderer spriter;
    private EnemyAnimation enemyAnimation;

    [Header("발사 쿨다운 설정 (순환)")]
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
    public bool showLineRenderer = true;
    private LineRenderer lineRenderer;
    private bool isPreparingToFire = false;

    [Header("부채꼴 각도 설정")]
    public float spreadAngle = 15f; // 중앙 기준 ±15도

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

        // -------------------------------
        // 매 프레임 Crystal 레이어 존재 여부 체크
        int crystalLayer = LayerMask.NameToLayer("Crystal");
        bool crystalExists = false;

        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
        {
            if (obj.layer == crystalLayer)
            {
                crystalExists = true;
                break;
            }
        }

        // 존재하면 Enemy 태그 제거, 없으면 Enemy 태그 설정
        if (crystalExists)
        {
            if (gameObject.tag == "Enemy")
                gameObject.tag = "Untagged";
        }
        else
        {
            if (gameObject.tag != "Enemy")
                gameObject.tag = "Enemy";
        }
        // -------------------------------

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            if (lineRenderer != null) lineRenderer.enabled = false;
            return;
        }

        if (lineRenderer != null) lineRenderer.enabled = showLineRenderer;

        Vector2 toPlayer = player.transform.position - transform.position;

        // 좌우 반전
        if (toPlayer.x != 0)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (toPlayer.x < 0 ? -1 : 1);
            transform.localScale = scale;
        }

        if (showLineRenderer && lineRenderer.enabled)
        {
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, player.transform.position);
        }

        float currentCooldown = fireIntervals[fireIndex];

        if (Time.time - lastFireTime >= currentCooldown && !isPreparingToFire)
        {
            StartCoroutine(PrepareAndShoot(toPlayer.normalized));
        }

        enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);
    }

    private System.Collections.IEnumerator PrepareAndShoot(Vector2 dir)
    {
        isPreparingToFire = true;

        float duration = 1f;

        // 발사 준비: 본체 빨간색
        if (spriter != null) spriter.DOColor(Color.red, duration);

        yield return new WaitForSeconds(duration);

        // 발사: 중앙 + 좌우 부채꼴 3발
        ShootSpread(dir);

        fireIndex = (fireIndex + 1) % fireIntervals.Length;
        lastFireTime = Time.time;

        if (spriter != null) spriter.DOColor(Color.white, 0.2f);

        yield return new WaitForSeconds(0.3f);

        isPreparingToFire = false;
    }

    private void ShootSpread(Vector2 dir)
    {
        // 중앙
        Shoot(dir);

        // 좌우 ±spreadAngle
        Shoot(RotateVector(dir, spreadAngle));
        Shoot(RotateVector(dir, -spreadAngle));
    }

    private Vector2 RotateVector(Vector2 v, float angle)
    {
        float rad = angle * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos).normalized;
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
        if (lineRenderer != null) Destroy(lineRenderer);
        isLive = false;
    }
}
