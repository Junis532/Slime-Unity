using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class DetectEnemy : EnemyBase
{
    private bool isLive = true;

    private SpriteRenderer spriter;
    private EnemyAnimation enemyAnimation;
    private NavMeshAgent agent;

    public float detectionRange = 5f;
    private bool hasDetectedPlayer = false;

    public float moveDuration = 4f;
    private float moveTimer = 0f;

    private Vector3 randomDestination;

    void Start()
    {
        spriter = GetComponent<SpriteRenderer>();
        enemyAnimation = GetComponent<EnemyAnimation>();
        agent = GetComponent<NavMeshAgent>();

        originalSpeed = GameManager.Instance.enemyStats.speed;
        speed = originalSpeed;

        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.speed = speed;

        PickRandomDestination();
    }

    void Update()
    {
        if (!isLive) return;

        GameObject player = GameObject.FindWithTag("Player");

        if (player != null)
        {
            Vector2 toPlayer = (Vector2)player.transform.position - (Vector2)transform.position;
            float distance = toPlayer.magnitude;

            if (!hasDetectedPlayer && distance <= detectionRange)
            {
                hasDetectedPlayer = true;
            }
        }

        // 이동
        if (hasDetectedPlayer && player != null)
        {
            agent.SetDestination(player.transform.position);
        }
        else
        {
            moveTimer += Time.deltaTime;

            if (moveTimer >= moveDuration || Vector3.Distance(transform.position, randomDestination) < 0.5f)
            {
                PickRandomDestination();
                moveTimer = 0f;
            }

            agent.SetDestination(randomDestination);
        }

        // --- 이동 방향으로 캐릭터 바라보기 ---
        Vector3 scale = transform.localScale;
        float vx = agent.velocity.x;

        if (Mathf.Abs(vx) > 0.01f) // 거의 정지 상태에서는 뒤집지 않음
        {
            scale.x = Mathf.Abs(scale.x) * (vx < 0 ? -1 : 1);
            transform.localScale = scale;
        }

        // 애니메이션 처리
        if (agent.velocity.magnitude > 0.1f)
        {
            enemyAnimation.PlayAnimation(EnemyAnimation.State.Move);
        }
        else
        {
            enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);
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

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isLive) return;

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
}
