using UnityEngine;
using UnityEngine.AI;
using DG.Tweening;

/// <summary>
/// 플레이어의 정중앙이 아니라 (플레이어콜라이더반경 + 에이전트반경 + 여유)만큼
/// 떨어진 점을 목적지로 삼아, 가장자리에서 빙빙 도는 문제를 방지한 버전.
/// 플레이어 근처에 네비 구멍이 있을 때를 대비해 근접 시 중앙으로 스냅하는 보정 포함.
/// </summary>
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
    [Range(-180f, 180f)] public float moveAngle = 0f;
    public float angleMoveSpeed = 5f;
    private Vector2 moveDirection;

    [Header("추격 세부 설정")]
    [Tooltip("플레이어와의 추가 여유거리. 살짝 겹치게 하려면 음수 사용(-0.05 권장).")]
    public float desiredSeparation = -0.05f;

    [Tooltip("SetDestination 호출 간격(과도한 리패스 방지).")]
    public float repathInterval = 0.08f;

    [Tooltip("NavMesh.SamplePosition 허용 반경(너무 크게 잡으면 구멍 경계로 튕김).")]
    public float sampleMaxDistance = 0.5f;

    [Tooltip("이 거리 이하로 근접하면 플레이어 중앙을 직접 목적지로 지정(구멍 우회).")]
    public float centerSnapRange = 0.35f;

    [Tooltip("원인 파악/떨림 완화를 위해 장애물 회피를 끌지 여부.")]
    public bool disableObstacleAvoid = true;

    [Tooltip("애니메이션 전환 임계 속도.")]
    public float minMoveSpeedToAnimate = 0.1f;

    [Header("충돌/반전")]
    public string obstacleTag = "AIWall"; // 각도 이동 모드에서 반전될 장애물 태그

    private float _repathTimer;

    void Start()
    {
        spriter = GetComponent<SpriteRenderer>();
        enemyAnimation = GetComponent<EnemyAnimation>();
        agent = GetComponent<NavMeshAgent>();

        originalSpeed = GameManager.Instance.enemyStats.speed;
        speed = originalSpeed;

        // 2D 세팅
        agent.updateRotation = false;
        agent.updateUpAxis = false;

        // 기본 이동값
        agent.speed = speed;
        agent.acceleration = Mathf.Max(8f, speed * 4f);
        agent.angularSpeed = 720f;
        agent.autoBraking = false; // 목적지 근처 진동 완화
        agent.stoppingDistance = 0f;

        if (disableObstacleAvoid)
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;

        if (useAngleMove)
            SetMoveDirection();
    }

    void Update()
    {
        if (!isLive || !AIEnabled) return;

        if (!CanMove)
        {
            if (agent.hasPath) agent.ResetPath();
            enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);
            return;
        }

        if (useAngleMove) AngleMove();
        else ChasePlayer();
    }

    // ===== 각도 이동 =====
    private void SetMoveDirection()
    {
        float rad = moveAngle * Mathf.Deg2Rad;
        moveDirection = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
    }

    private void AngleMove()
    {
        Vector3 nextPos = transform.position + (Vector3)moveDirection * angleMoveSpeed * Time.deltaTime;
        transform.position = nextPos;

        // 좌우 반전
        if (moveDirection.x != 0f)
        {
            var s = transform.localScale;
            s.x = Mathf.Abs(s.x) * (moveDirection.x < 0 ? -1 : 1);
            transform.localScale = s;
        }

        // 🚨 수정: 일반 Move 대신 방향별 애니메이션 재생
        enemyAnimation.PlayDirectionalMoveAnimation(moveDirection);
    }

    // ===== 추격 로직 =====
    float GetPlayerRadius(Transform p)
    {
        // 실제 콜라이더 기반 반경 계산(가장 정확)
        var cc = p.GetComponent<CircleCollider2D>();
        if (cc) return Mathf.Max(cc.radius * Mathf.Max(p.lossyScale.x, p.lossyScale.y), 0.01f);

        var bc = p.GetComponent<BoxCollider2D>();
        if (bc) return Mathf.Max(bc.size.x * p.lossyScale.x, bc.size.y * p.lossyScale.y) * 0.5f;

        var cap = p.GetComponent<CapsuleCollider2D>();
        if (cap)
        {
            var size = cap.size;
            var r = Mathf.Max(size.x * p.lossyScale.x, size.y * p.lossyScale.y) * 0.5f;
            return Mathf.Max(r, 0.01f);
        }

        // 콜라이더가 없으면 작은 기본값
        return 0.1f;
    }

    private void ChasePlayer()
    {
        GameObject pObj = GameObject.FindWithTag("Player");
        if (!pObj) return;

        Vector3 myPos = transform.position;
        Vector3 playerPos = pObj.transform.position;
        Vector3 toPlayer = playerPos - myPos;
        float dist = toPlayer.magnitude;

        // 실제 콜라이더 반경 기반 목표 이격
        float playerR = GetPlayerRadius(pObj.transform);
        float targetDist = agent.radius + playerR + desiredSeparation;
        if (targetDist < 0f) targetDist = 0f; // 완전 겹침을 허용하려면 이 라인 제거

        Vector3 goal = myPos;
        if (dist > 0.001f)
        {
            goal = playerPos - toPlayer.normalized * targetDist;

            // 충분히 가까워졌는데도 네비 구멍 경계에서 맴돌면 중앙으로 스냅
            if (dist < centerSnapRange) goal = playerPos;
        }

        // NavMesh 위의 가장 가까운 점으로 스냅
        if (NavMesh.SamplePosition(goal, out var hit, sampleMaxDistance, NavMesh.AllAreas))
            goal = hit.position;

        // 과도한 리패스 방지
        _repathTimer -= Time.deltaTime;
        if (_repathTimer <= 0f)
        {
            agent.stoppingDistance = 0f;
            agent.SetDestination(goal);
            _repathTimer = repathInterval;
        }

        // 🚨 수정: 좌우 반전 + 방향별 애니메이션
        Vector2 v = agent.velocity;

        if (v.magnitude > minMoveSpeedToAnimate)
        {
            // 몬스터 속도 벡터를 전달하여 애니메이션 상태 결정 (MoveSide, MoveFront, MoveBack)
            enemyAnimation.PlayDirectionalMoveAnimation(v);

            // MoveSide 상태일 때만 좌우 반전 처리
            if (enemyAnimation.currentState == EnemyAnimation.State.MoveSide)
            {
                var s = transform.localScale;
                s.x = Mathf.Abs(s.x) * (v.x < 0 ? -1 : 1);
                transform.localScale = s;
            }
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
        {
            agent.speed = newSpeed;
            agent.acceleration = Mathf.Max(8f, newSpeed * 4f);
        }
        if (useAngleMove)
            angleMoveSpeed = newSpeed;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isLive) return;

        if (collision.CompareTag("Player"))
        {
            // 🚨 NEW: 충돌 시 Attack 애니메이션 재생
            enemyAnimation.PlayAnimation(EnemyAnimation.State.Attack);

            // GameManager.Instance.joystickDirectionIndicator는 PlayerController에서 사용하는 것으로 보입니다.
            // 해당 객체가 null이거나, 스킬 사용 중이면 데미지 무시
            if (GameManager.Instance.joystickDirectionIndicator == null || GameManager.Instance.joystickDirectionIndicator.IsUsingSkill)
            {
                Debug.Log("스킬 사용 중이거나 인디케이터 문제로 몬스터 데미지 무시");
                return;
            }

            int damage = GameManager.Instance.enemyStats.attack;

            // 넉백 방향 계산을 위해 몬스터의 현재 위치를 '적 위치'로 전달합니다.
            Vector3 enemyPosition = transform.position;

            // PlayerDamaged.TakeDamage(데미지, 적 위치) 형식으로 호출
            GameManager.Instance.playerDamaged.TakeDamage(damage, enemyPosition);
        }

        // 각도 이동 모드일 때 장애물 반전
        if (useAngleMove && collision.CompareTag(obstacleTag))
            moveDirection = -moveDirection;
    }
}