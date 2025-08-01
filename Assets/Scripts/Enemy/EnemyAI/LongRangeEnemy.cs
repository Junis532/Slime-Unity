using DG.Tweening;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class LongRangeEnemy : EnemyBase
{
    private bool isLive = true;
    private SpriteRenderer spriter;
    private EnemyAnimation enemyAnimation;

    private NavMeshAgent agent;

    public float safeDistance = 3f;

    public GameObject bulletPrefab;
    public float bulletSpeed = 3f;
    public float fireCooldown = 1.5f;
    private float lastFireTime;
    public float bulletLifetime = 3f;

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
    }

    void Update()
    {
        if (!isLive) return;

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        Vector2 currentPos = transform.position;
        Vector2 toPlayer = (Vector2)player.transform.position - currentPos;
        float distance = toPlayer.magnitude;
        Vector2 dirToPlayer = toPlayer.normalized;

        if (distance < safeDistance)
        {
            // 플레이어와 멀어지도록 목적지 설정
            Vector2 escapeTarget = currentPos - dirToPlayer * safeDistance;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(escapeTarget, out hit, 2f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }

            // 발사 타이밍
            if (Time.time - lastFireTime >= fireCooldown)
            {
                Shoot(dirToPlayer);
                lastFireTime = Time.time;
            }
        }
        else
        {
            // 플레이어 쪽으로 천천히 접근
            agent.SetDestination(player.transform.position);
        }

        // 방향 처리 및 애니메이션
        Vector2 dir = agent.velocity;

        if (dir.magnitude > 0.1f)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (dir.x < 0 ? -1 : 1);
            transform.localScale = scale;

            enemyAnimation.PlayAnimation(EnemyAnimation.State.Move);
        }
        else
        {
            enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);
        }
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

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isLive) return;

        if (collision.CompareTag("Player"))
        {
            int damage = GameManager.Instance.longRangeEnemyStats.attack;
            GameManager.Instance.playerStats.currentHP -= damage;
            GameManager.Instance.playerDamaged.PlayDamageEffect();

            if (GameManager.Instance.playerStats.currentHP <= 0)
            {
                GameManager.Instance.playerStats.currentHP = 0;
                // 죽음 처리
            }
        }
    }
}
