using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class RandomDashEnemy : EnemyBase
{
    private bool isLive = true;

    private SpriteRenderer spriter;
    private EnemyAnimation enemyAnimation;
    private NavMeshAgent agent;

    [Header("대쉬 관련 설정")]
    public float dashDuration = 0.5f;   // 대쉬 시간
    public float waitDuration = 1f;     // 대쉬 후 대기 시간
    public float dashSpeed = 30f;       // 대쉬 속도
    public float dashDistance = 5f;     // 대쉬 거리

    void Start()
    {
        spriter = GetComponent<SpriteRenderer>();
        enemyAnimation = GetComponent<EnemyAnimation>();
        agent = GetComponent<NavMeshAgent>();

        originalSpeed = dashSpeed;
        speed = originalSpeed;

        agent.updateRotation = false;
        agent.updateUpAxis = false;

        StartCoroutine(DashLoop());
    }

    private IEnumerator DashLoop()
    {
        while (isLive)
        {
            // 랜덤 방향 설정
            Vector3 dashDir = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f).normalized;
            Vector3 targetPos = transform.position + dashDir * dashDistance;

            // 방향에 맞춰 스프라이트 반전
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (dashDir.x < 0 ? -1 : 1);
            transform.localScale = scale;

            // 대쉬 시작
            agent.speed = dashSpeed;
            agent.SetDestination(targetPos);
            //enemyAnimation.PlayAnimation(EnemyAnimation.State.Move);

            yield return new WaitForSeconds(dashDuration);

            // 멈춤
            agent.ResetPath();
            //enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);

            yield return new WaitForSeconds(waitDuration);
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
            GameManager.Instance.playerStats.currentHP -= damage;
            GameManager.Instance.playerDamaged.PlayDamageEffect();

            if (GameManager.Instance.playerStats.currentHP <= 0)
            {
                GameManager.Instance.playerStats.currentHP = 0;
                // 죽음 처리
            }
        }
    }
}
