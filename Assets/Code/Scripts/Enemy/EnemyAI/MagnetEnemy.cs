using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class MagnetEnemy : EnemyBase
{
    private bool isLive = true;

    private SpriteRenderer spriter;
    private EnemyAnimation enemyAnimation;
    private NavMeshAgent agent;

    private GameObject player;

    [Header("추적 및 끌기")]
    public float detectionRange = 5f;
    public float pullForce = 1.5f;

    [Header("시각적 범위 표시")]
    public GameObject rangeVisualPrefab;
    private GameObject rangeVisualInstance;

    void Start()
    {
        spriter = GetComponent<SpriteRenderer>();
        enemyAnimation = GetComponent<EnemyAnimation>();
        agent = GetComponent<NavMeshAgent>();

        originalSpeed = GameManager.Instance.enemyStats.speed;
        speed = originalSpeed;

        player = GameObject.FindWithTag("Player");

        // NavMeshAgent 설정
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.speed = speed;

        if (rangeVisualPrefab != null)
        {
            rangeVisualInstance = Instantiate(rangeVisualPrefab, transform.position, Quaternion.identity, transform);
            rangeVisualInstance.transform.localScale = Vector3.one * detectionRange * 2f;
        }
    }

    void Update()
    {
        if (!isLive || player == null) return;

        Vector3 playerPos = player.transform.position;
        float distance = Vector2.Distance(transform.position, playerPos);

        // 이동
        agent.SetDestination(playerPos);

        // 좌우 반전
        Vector2 dir = agent.velocity;
        if (dir.x != 0)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (dir.x < 0 ? -1 : 1);
            transform.localScale = scale;
        }

        // 이동 애니메이션
        if (dir.magnitude > 0.1f)
        {
            enemyAnimation.PlayAnimation(EnemyAnimation.State.Move);
        }
        else
        {
            enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);
        }

        // 플레이어 끌기
        if (distance <= detectionRange)
        {
            Vector3 pullDir = (transform.position - playerPos).normalized;
            player.transform.position += pullDir * pullForce * Time.deltaTime;
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

        if (rangeVisualInstance != null)
        {
            Destroy(rangeVisualInstance);
        }
    }
}
