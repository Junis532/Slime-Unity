using UnityEngine;
using UnityEngine.AI;
using DG.Tweening;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyAnimation))]
public class RandomDashEnemy : EnemyBase
{
    private bool isLive = true;

    private SpriteRenderer spriter;
    private EnemyAnimation enemyAnimation;
    private NavMeshAgent agent;

    [Header("대쉬 관련 설정")]
    public float dashSpeed = 30f;
    public float dashDistance = 5f;
    public float dashDuration = 0.5f;
    public float waitDuration = 1f;
    public float scaleMultiplier = 1.2f; // DOTween 커짐 배율
    public float scaleTime = 0.1f;       // 커지는 시간

    private bool isPreparingToDash = false;
    private bool isDashing = false;
    private Vector3 dashDir;
    private Vector3 targetPos;
    private float dashTimeElapsed = 0f;
    private float waitTimer = 0f;

    void Start()
    {
        spriter = GetComponent<SpriteRenderer>();
        enemyAnimation = GetComponent<EnemyAnimation>();
        agent = GetComponent<NavMeshAgent>();

        originalSpeed = dashSpeed;
        speed = originalSpeed;

        agent.updateRotation = false;
        agent.updateUpAxis = false;
    }

    void Update()
    {
        if (!isLive) return;

        // 준비 대시
        if (!isDashing && !isPreparingToDash)
        {
            dashDir = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f).normalized;
            targetPos = transform.position + dashDir * dashDistance;

            Vector3 originalScale = transform.localScale;
            Vector3 enlargedScale = originalScale * scaleMultiplier;
            transform.DOScale(enlargedScale, scaleTime).OnComplete(() =>
            {
                transform.DOScale(originalScale, dashDuration);
            });

            isPreparingToDash = true;
            dashTimeElapsed = 0f;

            if (enemyAnimation != null)
                enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);
            return;
        }

        // 대시 준비 중
        if (isPreparingToDash)
        {
            dashTimeElapsed += Time.deltaTime;
            if (dashTimeElapsed >= scaleTime)
            {
                isDashing = true;
                isPreparingToDash = false;
                dashTimeElapsed = 0f;

                agent.speed = dashSpeed;
                agent.SetDestination(targetPos);

                if (enemyAnimation != null)
                    enemyAnimation.PlayAnimation(EnemyAnimation.State.Move);
            }
            return;
        }

        // 대시 중
        if (isDashing)
        {
            dashTimeElapsed += Time.deltaTime;

            // 매 프레임 이동 방향으로 Flip
            if (agent.velocity.sqrMagnitude > 0.01f)
            {
                FlipSprite(agent.velocity.x);
            }

            if (Vector3.Distance(transform.position, targetPos) < 0.1f || dashTimeElapsed >= dashDuration)
            {
                isDashing = false;
                waitTimer = 0f;
                agent.ResetPath();

                if (enemyAnimation != null)
                    enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);
            }
        }
        else
        {
            // 대시 후 대기
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitDuration)
                isPreparingToDash = false;
        }
    }

    private void FlipSprite(float directionX)
    {
        if (directionX == 0) return; // 방향이 없으면 변경하지 않음

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (directionX < 0 ? -1 : 1);
        transform.localScale = scale;
    }

}
