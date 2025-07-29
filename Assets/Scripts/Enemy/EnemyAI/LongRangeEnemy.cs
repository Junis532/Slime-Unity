using DG.Tweening;
using UnityEngine;

public class LongRangeEnemy : EnemyBase
{
    private bool isLive = true;
    private SpriteRenderer spriter;
    private EnemyAnimation enemyAnimation;

    private Vector2 currentVelocity;
    private Vector2 currentDirection;

    public float smoothTime = 0.1f;
    public float safeDistance = 3f;         // 플레이어가 이 거리 안에 오면 도망 + 공격 시작

    public GameObject bulletPrefab;         // 발사할 탄환 프리팹
    public float bulletSpeed = 3f;          // 탄환 속도
    public float fireCooldown = 1.5f;       // 발사 쿨다운
    private float lastFireTime;             // 마지막 발사 시점

    public float bulletLifetime = 3f;       // 총알 생존 시간 (초)

    [Header("회피 관련")]
    public float avoidanceRange = 2f;       // 장애물 감지 범위
    public LayerMask obstacleMask;          // 장애물 레이어 지정

    // 행동/멈춤 주기용 변수
    public float moveDuration = 4f;
    public float idleDuration = 3f;
    private float actionTimer = 0f;
    private bool isIdle = false;

    void Start()
    {
        spriter = GetComponent<SpriteRenderer>();
        enemyAnimation = GetComponent<EnemyAnimation>();

        originalSpeed = GameManager.Instance.longRangeEnemyStats.speed;
        speed = originalSpeed;
    }

    void Update()
    {
        if (!isLive) return;

        actionTimer += Time.deltaTime;

        if (isIdle)
        {
            if (actionTimer >= idleDuration)
            {
                isIdle = false;
                actionTimer = 0f;
            }

            enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);
            return;
        }
        else
        {
            if (actionTimer >= moveDuration)
            {
                isIdle = true;
                actionTimer = 0f;
                enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);
                return;
            }
        }

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        Vector2 currentPos = transform.position;
        Vector2 toPlayer = (Vector2)player.transform.position - currentPos;
        float distance = toPlayer.magnitude;
        Vector2 dirToPlayer = toPlayer.normalized;

        // ------------------ 장애물 레이캐스트 검사 ------------------
        RaycastHit2D hit = Physics2D.Raycast(currentPos, dirToPlayer, avoidanceRange, obstacleMask);

        Vector2 avoidanceVector = Vector2.zero;

        if (hit.collider != null)
        {
            // 장애물이 감지됨 → 옆으로 회피 방향 계산
            Vector2 hitNormal = hit.normal; // 장애물 표면 노멀 벡터

            // hitNormal에 수직인 방향(옆으로)
            Vector2 sideStep = Vector2.Perpendicular(hitNormal);

            // 오른쪽 또는 왼쪽 방향으로 선택(여기선 오른쪽으로)
            avoidanceVector = sideStep.normalized * 1.5f;

            Debug.DrawRay(currentPos, sideStep * 2, Color.green);
        }

        // ------------------ 최종 이동 방향 계산 ------------------
        Vector2 moveDir;

        if (distance < safeDistance)
        {
            // 가까우면 도망가면서 공격
            moveDir = (-dirToPlayer + avoidanceVector).normalized;

            if (Time.time - lastFireTime >= fireCooldown)
            {
                Shoot(dirToPlayer); // 플레이어 쪽으로 총알 발사
                lastFireTime = Time.time;
            }
        }
        else
        {
            // 멀면 플레이어 쪽으로 이동 + 회피
            moveDir = (dirToPlayer + avoidanceVector).normalized;
        }

        currentDirection = Vector2.SmoothDamp(currentDirection, moveDir, ref currentVelocity, smoothTime);
        Vector2 nextVec = currentDirection * speed * Time.deltaTime;
        transform.Translate(nextVec);

        // 좌우 반전
        if (currentDirection.magnitude > 0.01f)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (currentDirection.x < 0 ? -1 : 1);
            transform.localScale = scale;
        }

        // 애니메이션 처리
        if (currentDirection.magnitude > 0.01f)
            enemyAnimation.PlayAnimation(EnemyAnimation.State.Move);
        else
            enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, avoidanceRange);
        // 레이캐스트 시각화는 Debug.DrawRay()로 확인하세요.
    }
}
