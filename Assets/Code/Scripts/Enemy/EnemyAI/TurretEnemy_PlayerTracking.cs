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
    public float firstFireDelay = 2f;

    [Header("탄환 설정")]
    public GameObject bulletPrefab;
    public float bulletSpeed = 1.5f;
    public float bulletLifetime = 3f;

    [Header("LineRenderer 설정")]
    public bool showLineRenderer = true; // 여기서 켜고 끔
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
        lineRenderer.enabled = showLineRenderer;

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
            if (lineRenderer != null)
                lineRenderer.enabled = false;
            return;
        }

        if (lineRenderer != null)
            lineRenderer.enabled = showLineRenderer;

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
            StartCoroutine(PrepareAndShoot());
        }

        enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);
    }


    private System.Collections.IEnumerator PrepareAndShoot()
    {
        isPreparingToFire = true;

        float duration = 1f; // 발사 준비 시간

        // 발사 준비: 본체 색이 하얀색 → 빨강으로 변화
        if (spriter != null)
        {
            spriter.DOColor(Color.red, duration);
        }

        yield return new WaitForSeconds(duration);

        // 발사
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            Vector2 dir = (player.transform.position - transform.position).normalized;
            Shoot(dir);

            fireIndex = (fireIndex + 1) % fireIntervals.Length;
            lastFireTime = Time.time;
        }

        // 발사 후 본체 색 다시 하얀색으로
        if (spriter != null)
        {
            spriter.DOColor(Color.white, 0.2f); // 0.2초 동안 서서히 복귀
        }

        yield return new WaitForSeconds(0.3f); // 발사 후 잠시 대기

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
