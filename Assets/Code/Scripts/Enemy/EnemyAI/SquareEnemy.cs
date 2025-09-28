using UnityEngine;

public class SquareEnemy : EnemyBase
{
    private bool isLive = true;

    private SpriteRenderer spriter;
    private EnemyAnimation enemyAnimation;

    [Header("경로 설정")]
    public Vector2 pathCenter = Vector2.zero;
    public float pathWidth = 20f;
    public float pathHeight = 12f;

    private Vector2[] waypoints;
    private int currentWaypointIndex = 0;

    private Vector2 currentVelocity;
    private Vector2 currentDirection;

    public float smoothTime = 0.1f;

    [Header("회피 관련")]
    public float avoidanceRange = 2f;
    public LayerMask obstacleMask;

    void Start()
    {
        spriter = GetComponent<SpriteRenderer>();
        enemyAnimation = GetComponent<EnemyAnimation>();

        originalSpeed = GameManager.Instance.enemyStats.speed;
        speed = originalSpeed;

        GenerateWaypoints();
        transform.position = waypoints[0];
        currentWaypointIndex = 1;
    }

    void GenerateWaypoints()
    {
        float halfWidth = pathWidth / 2f;
        float halfHeight = pathHeight / 2f;

        waypoints = new Vector2[]
        {
            pathCenter + new Vector2(-halfWidth,  halfHeight),
            pathCenter + new Vector2( halfWidth,  halfHeight),
            pathCenter + new Vector2( halfWidth, -halfHeight),
            pathCenter + new Vector2(-halfWidth, -halfHeight)
        };
    }

    void Update()
    {
        if (!isLive) return;

        Vector2 currentPos = transform.position;
        Vector2 target = waypoints[currentWaypointIndex];
        Vector2 dirToTarget = (target - currentPos).normalized;

        // 장애물 회피 검사
        RaycastHit2D hit = Physics2D.Raycast(currentPos, dirToTarget, avoidanceRange, obstacleMask);

        Vector2 avoidanceVector = Vector2.zero;
        if (hit.collider != null)
        {
            Vector2 hitNormal = hit.normal;
            Vector2 sideStep = Vector2.Perpendicular(hitNormal).normalized;
            avoidanceVector = sideStep * 1.5f;
            Debug.DrawRay(currentPos, sideStep * 2, Color.green);
        }

        // 최종 방향
        Vector2 finalDir = (dirToTarget + avoidanceVector).normalized;
        currentDirection = Vector2.SmoothDamp(currentDirection, finalDir, ref currentVelocity, smoothTime);
        Vector2 moveVec = currentDirection * speed * Time.deltaTime;
        transform.Translate(moveVec);

        if (Vector2.Distance(transform.position, target) < 0.1f)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        }

        // 방향 반전
        if (currentDirection.x != 0)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (currentDirection.x < 0 ? -1 : 1);
            transform.localScale = scale;
        }

        // 애니메이션 처리
        if (currentDirection.magnitude > 0.01f)
        {
            enemyAnimation.PlayAnimation(EnemyAnimation.State.Move);
        }
        else
        {
            enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // 스킬 사용 중이면 충돌 무시
            if (GameManager.Instance.joystickDirectionIndicator.IsUsingSkill)
            {
                Debug.Log("스킬 사용 중이라 몬스터 데미지 무시");
                return;
            }

            int damage = GameManager.Instance.enemyStats.attack;

            // 넉백 방향 계산을 위해 현재 몬스터의 위치를 '적 위치'로 전달합니다.
            Vector3 enemyPosition = transform.position;

            // 수정된 PlayerDamaged.TakeDamage(데미지, 적 위치) 형식으로 호출
            // 기존의 collision과 contactPoint 인수는 제거됩니다.
            GameManager.Instance.playerDamaged.TakeDamage(damage, enemyPosition);
        }
    }
    private void OnDestroy()
    {
        isLive = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, avoidanceRange);

        // 사각형 경로 표시
        Vector2 p1 = pathCenter + new Vector2(-pathWidth / 2f, pathHeight / 2f);
        Vector2 p2 = pathCenter + new Vector2(pathWidth / 2f, pathHeight / 2f);
        Vector2 p3 = pathCenter + new Vector2(pathWidth / 2f, -pathHeight / 2f);
        Vector2 p4 = pathCenter + new Vector2(-pathWidth / 2f, -pathHeight / 2f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p2, p3);
        Gizmos.DrawLine(p3, p4);
        Gizmos.DrawLine(p4, p1);
    }
}
