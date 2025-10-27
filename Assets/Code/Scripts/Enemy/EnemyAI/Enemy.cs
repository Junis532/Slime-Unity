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
    public bool AIEnabled = true;
    public bool useAngleMove = false;
    [Range(-360f, 360f)] public float moveAngle = 0f;
    public float angleMoveSpeed = 5f;
    private Vector2 moveDirection;

    [Header("추격 세부 설정")]
    public float desiredSeparation = -0.05f;
    public float repathInterval = 0.08f;
    public float sampleMaxDistance = 0.5f;
    public float centerSnapRange = 0.35f;
    public bool disableObstacleAvoid = true;
    public float minMoveSpeedToAnimate = 0.1f;

    [Header("속도 설정")]
    [Tooltip("이 적의 이동 속도를 설정합니다.")]
    public float moveSpeed = 3.5f; // 👈 인스펙터에서 직접 설정 가능

    [Header("충돌/반전")]
    public string obstacleTag = "AIWall";

    private float _repathTimer;

    void Start()
    {
        spriter = GetComponent<SpriteRenderer>();
        enemyAnimation = GetComponent<EnemyAnimation>();
        agent = GetComponent<NavMeshAgent>();

        // 인스펙터에서 설정한 속도 사용
        speed = moveSpeed;
        originalSpeed = speed;

        // 2D 세팅
        agent.updateRotation = false;
        agent.updateUpAxis = false;

        agent.speed = speed;
        agent.acceleration = Mathf.Max(8f, speed * 4f);
        agent.angularSpeed = 720f;
        agent.autoBraking = false;
        agent.stoppingDistance = 0f;

        if (disableObstacleAvoid)
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;

        if (useAngleMove)
            SetMoveDirection();
    }

    void Update()
    {
        if (!isLive || !AIEnabled || !CanMove)
        {
            if (agent.hasPath) agent.ResetPath();
            if (agent != null) agent.isStopped = true;
            enemyAnimation?.PlayAnimation(EnemyAnimation.State.Idle);
            return;
        }

        if (agent != null && agent.isStopped) agent.isStopped = false;

        if (useAngleMove) AngleMove();
        else ChasePlayer();
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

        if (moveDirection.x != 0f)
        {
            var s = transform.localScale;
            s.x = Mathf.Abs(s.x) * (moveDirection.x < 0 ? -1 : 1);
            transform.localScale = s;
        }

        enemyAnimation.PlayDirectionalMoveAnimation(moveDirection);
    }

    private void ChasePlayer()
    {
        GameObject pObj = GameObject.FindWithTag("Player");
        if (!pObj) return;

        Vector3 myPos = transform.position;
        Vector3 playerPos = pObj.transform.position;
        Vector3 toPlayer = playerPos - myPos;
        float dist = toPlayer.magnitude;

        float playerR = GetPlayerRadius(pObj.transform);
        float targetDist = agent.radius + playerR + desiredSeparation;
        if (targetDist < 0f) targetDist = 0f;

        Vector3 goal = dist > 0.001f ? playerPos - toPlayer.normalized * targetDist : myPos;
        if (dist < centerSnapRange) goal = playerPos;

        if (NavMesh.SamplePosition(goal, out var hit, sampleMaxDistance, NavMesh.AllAreas))
            goal = hit.position;

        _repathTimer -= Time.deltaTime;
        if (_repathTimer <= 0f)
        {
            agent.stoppingDistance = 0f;
            agent.SetDestination(goal);
            _repathTimer = repathInterval;
        }

        Vector2 v = agent.velocity;
        if (v.magnitude > minMoveSpeedToAnimate)
        {
            enemyAnimation.PlayDirectionalMoveAnimation(v);
            if (enemyAnimation.currentState == EnemyAnimation.State.MoveSide)
            {
                var s = transform.localScale;
                s.x = Mathf.Abs(s.x) * (v.x < 0 ? -1 : 1);
                transform.localScale = s;
            }
        }
        else enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);
    }

    float GetPlayerRadius(Transform p)
    {
        var cc = p.GetComponent<CircleCollider2D>();
        if (cc) return Mathf.Max(cc.radius * Mathf.Max(p.lossyScale.x, p.lossyScale.y), 0.01f);
        var bc = p.GetComponent<BoxCollider2D>();
        if (bc) return Mathf.Max(bc.size.x * p.lossyScale.x, bc.size.y * p.lossyScale.y) * 0.5f;
        var cap = p.GetComponent<CapsuleCollider2D>();
        if (cap)
        {
            var size = cap.size;
            return Mathf.Max(size.x * p.lossyScale.x, size.y * p.lossyScale.y) * 0.5f;
        }
        return 0.1f;
    }

    // ===== AI 제어 함수 =====
    public void EnableAI()
    {
        AIEnabled = true;
        CanMove = true;
        if (agent != null)
        {
            agent.isStopped = false;
            GameObject pObj = GameObject.FindWithTag("Player");
            if (pObj != null)
            {
                Vector3 goal = pObj.transform.position;
                agent.SetDestination(goal);
            }
        }
    }

    public void DisableAI()
    {
        AIEnabled = false;
        if (agent != null)
            agent.isStopped = true;
    }

    public override void StopMovement()
    {
        base.StopMovement();
        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }
    }

    public override void ResumeMovement()
    {
        base.ResumeMovement();
        if (agent != null)
            agent.isStopped = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isLive) return;

        if (collision.CompareTag("Player"))
        {
            enemyAnimation.PlayAnimation(EnemyAnimation.State.Attack);

            if (GameManager.Instance.joystickDirectionIndicator == null || GameManager.Instance.joystickDirectionIndicator.IsUsingSkill)
                return;

            int damage = GameManager.Instance.enemyStats.attack;
            Vector3 enemyPosition = transform.position;
            GameManager.Instance.playerDamaged.TakeDamage(damage, enemyPosition);
        }

        if (useAngleMove && collision.CompareTag(obstacleTag))
            moveDirection = -moveDirection;
    }
}