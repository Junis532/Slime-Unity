using DG.Tweening;
using UnityEngine;

public class TurretEnemy : EnemyBase
{
    private bool isLive = true;
    private SpriteRenderer spriter;
    private EnemyAnimation enemyAnimation;

    public float fireRange = 5f;             // 발사 범위
    public float fireCooldown = 1.5f;        // 발사 쿨다운
    private float lastFireTime;

    public GameObject bulletPrefab;
    public float bulletSpeed = 1.5f;
    public float bulletLifetime = 3f;

    private LineRenderer lineRenderer;

    private bool isPreparingToFire = false; // 발사 준비중 상태

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

        // 발사 쿨타임 체크
        if (Time.time - lastFireTime >= fireCooldown && !isPreparingToFire)
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

        // 원래 선 굵기/색 저장
        float startWidth = 0.05f;
        Color startColor = Color.red;

        // 준비 동안 선 가늘어지고 투명해짐
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration; // 0 → 1

            float width = Mathf.Lerp(startWidth, 0f, t);
            lineRenderer.startWidth = width;
            lineRenderer.endWidth = width;

            Color fadeColor = Color.Lerp(startColor, new Color(startColor.r, startColor.g, startColor.b, 0f), t);
            lineRenderer.startColor = fadeColor;
            lineRenderer.endColor = fadeColor;

            yield return null;
        }

        // 발사 직전 선 숨기기
        lineRenderer.enabled = false;

        // 발사 방향 계산
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            Vector2 dir = (player.transform.position - transform.position).normalized;
            Shoot(dir);
            lastFireTime = Time.time;
        }

        // 발사 후 잠시 대기
        yield return new WaitForSeconds(0.3f);

        if (isLive)
        {
            // 선 굵기/색상 복구
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
