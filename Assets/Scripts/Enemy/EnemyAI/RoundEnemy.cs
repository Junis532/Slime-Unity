using UnityEngine;

public class RoundEnemy : EnemyBase
{
    private bool isLive = true;

    private SpriteRenderer spriter;
    private EnemyAnimation enemyAnimation;

    [Header("원형 경로 설정")]
    public Vector2 pathCenter = Vector2.zero;   // 원 중심
    public float orbitRadius = 3f;               // 원 반지름

    private float currentAngle = 0f;             // 현재 각도 (도 단위)
    public float orbitSpeed = 90f;               // 각속도 (도/초)

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

        // 시작 각도 계산 (현재 위치가 pathCenter에서 어느 방향인지)
        Vector2 offset = (Vector2)transform.position - pathCenter;
        if (offset.magnitude > 0.01f)
            currentAngle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
        else
            currentAngle = 0f;
    }

    void Update()
    {
        if (!isLive) return;

        // 각도 갱신 (시계 방향)
        currentAngle += orbitSpeed * Time.deltaTime;
        if (currentAngle >= 360f) currentAngle -= 360f;

        // 원 위 목표 위치 계산
        float rad = currentAngle * Mathf.Deg2Rad;
        Vector2 target = pathCenter + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * orbitRadius;

        Vector2 currentPos = transform.position;
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

        // 최종 이동 방향 결정
        Vector2 finalDir = (dirToTarget + avoidanceVector).normalized;
        currentDirection = Vector2.SmoothDamp(currentDirection, finalDir, ref currentVelocity, smoothTime);
        Vector2 moveVec = currentDirection * speed * Time.deltaTime;
        transform.Translate(moveVec);

        // 방향 반전 처리
        if (currentDirection.x != 0)
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

    private void OnTriggerEnter2D(Collider2D collision)
    {


        if (collision.CompareTag("Player"))
        {
            if (GameManager.Instance.joystickDirectionIndicator.IsUsingSkill)
            {
                Debug.Log("스킬 사용 중이라 몬스터 데미지 무시");
                return;
            }

            // ✅ 이제는 PlayerDamaged 쪽에 위임
            int damage = GameManager.Instance.enemyStats.attack;
            GameManager.Instance.playerDamaged.TakeDamage(damage);
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

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(pathCenter, orbitRadius);
    }
}
