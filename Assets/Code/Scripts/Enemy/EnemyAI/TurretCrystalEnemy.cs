using UnityEngine;
using UnityEngine.AI;
using DG.Tweening;

[RequireComponent(typeof(NavMeshAgent))]
public class TurretCrystalEnemy : EnemyBase
{
    private bool isLive = true;

    private SpriteRenderer spriter;
    private EnemyAnimation enemyAnimation;
    private NavMeshAgent agent;

    [Header("움직임 비활성화")]
    public bool isStationary = true; // true이면 이동하지 않음

    void Start()
    {
        spriter = GetComponent<SpriteRenderer>();
        enemyAnimation = GetComponent<EnemyAnimation>();
        agent = GetComponent<NavMeshAgent>();

        originalSpeed = GameManager.Instance.enemyStats.speed;
        speed = originalSpeed;

        // NavMeshAgent 2D 설정
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.speed = speed;

        // 이동 비활성화 시 경로 초기화
        if (isStationary && agent.hasPath)
            agent.ResetPath();
    }

    void Update()
    {
        if (!isLive) return;

        // ❌ 이동 비활성화: 항상 Idle 애니메이션
        if (isStationary)
        {
            if (agent.hasPath)
                agent.ResetPath();

            enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);
            return;
        }

        // ❌ 필요하면 기존 AI 이동 코드는 여기에 추가 가능
    }

    public override void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
        if (agent != null)
            agent.speed = newSpeed;
    }

}
