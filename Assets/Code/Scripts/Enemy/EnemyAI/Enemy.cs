using UnityEngine;
using UnityEngine.AI; // NavMeshAgent
using DG.Tweening;

[RequireComponent(typeof(NavMeshAgent))]
public class Enemy : EnemyBase
{
    private bool isLive = true;

    private SpriteRenderer spriter;
    private EnemyAnimation enemyAnimation;

    private NavMeshAgent agent;

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
    }

    void Update()
    {
        if (!isLive) return;

        // ✅ 이동 가능 여부 체크
        if (!CanMove)
        {
            if (agent.hasPath)
                agent.ResetPath();   // 이동 경로 초기화
            enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);
            return;
        }

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        agent.SetDestination(player.transform.position);

        // 이동 애니메이션 처리
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
    }
}
