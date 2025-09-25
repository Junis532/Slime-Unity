using UnityEngine;
using UnityEngine.AI;
using DG.Tweening;

[RequireComponent(typeof(NavMeshAgent))]
public class Enemy : EnemyBase
{
    private bool isLive = true;

    private SpriteRenderer spriter;
    private EnemyAnimation enemyAnimation;

    private NavMeshAgent agent;

    [Header("AI & 이동 설정")]
    public bool AIEnabled = true;             // AI 켜고 끌 수 있음
    public bool useAngleMove = false;         // 각도 이동 모드
    public float moveAngle = 0f;              // 이동 각도 (도 단위)
    public string obstacleTag = "Obstacle";   // 충돌 반전 태그
    public float angleMoveSpeed = 5f;         // 각도 이동 속도
    private Vector2 moveDirection;

    void Start()
    {
        spriter = GetComponent<SpriteRenderer>();
        enemyAnimation = GetComponent<EnemyAnimation>();
        agent = GetComponent<NavMeshAgent>();

        originalSpeed = GameManager.Instance.enemyStats.speed;
        speed = originalSpeed;

        agent.updateRotation = false;
        agent.updateUpAxis = false; // 2D용
        agent.speed = speed;

        if (useAngleMove)
        {
            SetMoveDirection();
        }
    }

    void Update()
    {
        if (!isLive || !AIEnabled) return;

        if (!CanMove)
        {
            if (agent.hasPath)
                agent.ResetPath();
            enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);
            return;
        }

        if (useAngleMove)
        {
            AngleMove();
        }
        else
        {
            ChasePlayer();
        }
    }

    private void SetMoveDirection()
    {
        float rad = moveAngle * Mathf.Deg2Rad;
        moveDirection = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
    }

    private void AngleMove()
    {
        Vector3 nextPos = transform.position + (Vector3)moveDirection * angleMoveSpeed * Time.deltaTime;
        transform.position = nextPos;

        // 스프라이트 반전
        if (moveDirection.x != 0)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (moveDirection.x < 0 ? -1 : 1);
            transform.localScale = scale;
        }

        enemyAnimation.PlayAnimation(EnemyAnimation.State.Move);
    }

    private void ChasePlayer()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        agent.SetDestination(player.transform.position);

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

    public override void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
        if (agent != null)
            agent.speed = newSpeed;
        if (useAngleMove)
            angleMoveSpeed = newSpeed; // 각도 이동에도 적용
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isLive) return;

        if (collision.CompareTag("Player"))
        {
            if (GameManager.Instance.joystickDirectionIndicator.IsUsingSkill)
            {
                Debug.Log("스킬 사용 중이라 몬스터 데미지 무시");
                return;
            }

            int damage = GameManager.Instance.enemyStats.attack;
            GameManager.Instance.playerDamaged.TakeDamage(damage);
        }

        // 장애물 충돌 시 각도 이동 반전
        if (useAngleMove && collision.CompareTag(obstacleTag))
        {
            moveDirection = -moveDirection;
        }
    }
}
