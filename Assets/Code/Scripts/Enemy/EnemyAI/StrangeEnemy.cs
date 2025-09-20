using DG.Tweening;
using UnityEngine;

public class StrangeEnemy : EnemyBase
{
    private bool isLive = true;

    private SpriteRenderer spriter;
    private EnemyAnimation enemyAnimation;

    private Vector2 currentVelocity;
    private Vector2 currentDirection;

    public float smoothTime = 0.1f;

    [Header("회피 관련")]
    public float avoidanceRange = 2f;
    public LayerMask obstacleMask;

    [Header("이상한 움직임")]
    public float weirdnessInterval = 1.5f;
    public float weirdnessStrength = 1.2f;
    private Vector2 weirdnessOffset = Vector2.zero;

    void Start()
    {
        spriter = GetComponent<SpriteRenderer>();
        enemyAnimation = GetComponent<EnemyAnimation>();

        originalSpeed = GameManager.Instance.enemyStats.speed;
        speed = originalSpeed;

        InvokeRepeating(nameof(UpdateWeirdnessOffset), 0f, weirdnessInterval);
    }

    void UpdateWeirdnessOffset()
    {
        // -1 ~ 1 사이의 랜덤 벡터 생성
        weirdnessOffset = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized * weirdnessStrength;
    }

    void Update()
    {
        if (!isLive) return;

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        Vector2 currentPos = transform.position;
        Vector2 dirToPlayer = ((Vector2)player.transform.position - currentPos).normalized;

        // 장애물 회피
        RaycastHit2D hit = Physics2D.Raycast(currentPos, dirToPlayer, avoidanceRange, obstacleMask);
        Vector2 avoidanceVector = Vector2.zero;

        if (hit.collider != null)
        {
            Vector2 hitNormal = hit.normal;
            Vector2 sideStep = Vector2.Perpendicular(hitNormal);
            avoidanceVector = sideStep.normalized * 1.5f;

            Debug.DrawRay(currentPos, sideStep * 2, Color.green);
        }

        // ▶ 이상한 방향 추가
        Vector2 finalDir = (dirToPlayer + avoidanceVector + weirdnessOffset).normalized;

        currentDirection = Vector2.SmoothDamp(currentDirection, finalDir, ref currentVelocity, smoothTime);
        Vector2 moveVec = currentDirection * speed * Time.deltaTime;
        transform.Translate(moveVec);

        if (currentDirection.magnitude > 0.01f)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (currentDirection.x < 0 ? -1 : 1);
            transform.localScale = scale;

            enemyAnimation.PlayAnimation(EnemyAnimation.State.Move);
        }
        else
        {
            enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);
        }
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, avoidanceRange);
    }
}
