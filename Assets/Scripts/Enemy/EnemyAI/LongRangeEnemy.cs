using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class LongRangeEnemy : EnemyBase
{
    private bool isLive = true;
    private SpriteRenderer spriter;
    private EnemyAnimation enemyAnimation;
    private NavMeshAgent agent;

    [Header("랜덤 이동 관련")]
    public float moveDuration = 3f;  // 이동 시간
    private float moveTimer = 0f;
    private Vector3 randomDestination;

    [Header("발사 관련")]
    public float fireCooldown = 2f;  // 쏘는 주기
    private float lastFireTime;
    public GameObject bulletPrefab;
    public float bulletSpeed = 1.5f;
    public float bulletLifetime = 3f;
    private bool isPreparingToFire = false;

    [Header("라인렌더러")]
    private LineRenderer lineRenderer;

    void Start()
    {
        spriter = GetComponent<SpriteRenderer>();
        enemyAnimation = GetComponent<EnemyAnimation>();
        agent = GetComponent<NavMeshAgent>();

        originalSpeed = GameManager.Instance.longRangeEnemyStats.speed;
        speed = originalSpeed;

        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.speed = speed;

        PickRandomDestination();

        // 라인렌더러 세팅
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

        // 좌우 반전
        Vector2 toPlayer = (Vector2)player.transform.position - (Vector2)transform.position;
        if (toPlayer.x != 0)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (toPlayer.x < 0 ? -1 : 1);
            transform.localScale = scale;
        }

        // 발사 쿨타임 체크
        if (Time.time - lastFireTime >= fireCooldown && !isPreparingToFire)
        {
            StartCoroutine(PrepareAndShoot(player));
        }
        else if (!isPreparingToFire) // 발사 준비중이 아닐 때만 이동
        {
            moveTimer += Time.deltaTime;
            if (moveTimer >= moveDuration || Vector3.Distance(transform.position, randomDestination) < 0.5f)
            {
                PickRandomDestination();
                moveTimer = 0f;
            }
            agent.SetDestination(randomDestination);

            if (agent.velocity.magnitude > 0.1f)
                enemyAnimation.PlayAnimation(EnemyAnimation.State.Move);
            else
                enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);
        }
    }

    private IEnumerator PrepareAndShoot(GameObject player)
    {
        isPreparingToFire = true;

        // 이동 멈춤
        agent.SetDestination(transform.position);
        enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);

        // 조준선 켜기
        lineRenderer.enabled = true;
        float timer = 0f;
        while (timer < 1f)
        {
            timer += Time.deltaTime;
            if (player != null)
            {
                lineRenderer.SetPosition(0, transform.position);
                lineRenderer.SetPosition(1, player.transform.position);
            }
            yield return null;
        }

        // 발사
        lineRenderer.enabled = false;
        if (player != null)
        {
            Vector2 dir = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
            Shoot(dir);
            lastFireTime = Time.time;
        }

        isPreparingToFire = false;
    }

    private void Shoot(Vector2 dir)
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

    private void PickRandomDestination()
    {
        float minX = -10f, maxX = 10f, minY = -6f, maxY = 6f;
        randomDestination = new Vector3(
            Random.Range(minX, maxX),
            Random.Range(minY, maxY),
            0f
        );
    }

    private void OnDestroy()
    {
        isLive = false;
        if (lineRenderer != null)
            Destroy(lineRenderer);
    }
}
