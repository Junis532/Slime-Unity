using DG.Tweening;
using UnityEngine;

public class ExplosionEnemy : EnemyBase
{
    private bool isLive = true;

    private SpriteRenderer spriter;
    private EnemyAnimation enemyAnimation;

    private Vector2 currentVelocity;
    private Vector2 currentDirection;

    public float smoothTime = 0.1f;
    public float explosionRange = 1.5f;
    public GameObject explosionEffectPrefab;

    [Header("회피 관련")]
    public float avoidanceRange = 1.5f;
    public LayerMask obstacleMask;

    [Header("행동/멈춤 주기")]
    public float moveDuration = 4f;
    public float idleDuration = 3f;
    private float actionTimer = 0f;
    private bool isIdle = false;

    void Start()
    {
        spriter = GetComponent<SpriteRenderer>();
        enemyAnimation = GetComponent<EnemyAnimation>();

        originalSpeed = GameManager.Instance.enemyStats.speed;
        speed = originalSpeed;
    }

    void Update()
    {
        if (!isLive) return;

        actionTimer += Time.deltaTime;

        // 폭발 거리 내에 플레이어가 있으면 즉시 폭발
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        Vector2 currentPos = transform.position;
        Vector2 dirToPlayer = ((Vector2)player.transform.position - currentPos);
        float distanceToPlayer = dirToPlayer.magnitude;

        if (distanceToPlayer <= explosionRange)
        {
            Explode(player.transform.position);
            return;
        }

        // 행동/멈춤 상태 전환
        if (isIdle)
        {
            if (actionTimer >= idleDuration)
            {
                isIdle = false;
                actionTimer = 0f;
            }

            enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);
            return;
        }
        else
        {
            if (actionTimer >= moveDuration)
            {
                isIdle = true;
                actionTimer = 0f;
                return;
            }
        }

        // 장애물 회피 로직
        RaycastHit2D hit = Physics2D.Raycast(currentPos, dirToPlayer.normalized, avoidanceRange, obstacleMask);
        Vector2 avoidanceVector = Vector2.zero;

        if (hit.collider != null)
        {
            Vector2 hitNormal = hit.normal;
            Vector2 sideStep = Vector2.Perpendicular(hitNormal);
            avoidanceVector = sideStep.normalized * 1.5f;

            Debug.DrawRay(currentPos, sideStep * 2f, Color.green);
        }

        Vector2 finalDir = (dirToPlayer.normalized + avoidanceVector).normalized;
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

    private void Explode(Vector3 position)
    {
        if (!isLive) return;
        isLive = false;

        if (explosionEffectPrefab != null)
        {
            GameObject effect = Instantiate(explosionEffectPrefab, position, Quaternion.identity);
            Destroy(effect, 0.3f);
        }

        int damage = GameManager.Instance.enemyStats.attack;
        GameManager.Instance.playerStats.currentHP -= damage;

        if (GameManager.Instance.playerDamaged != null)
            GameManager.Instance.playerDamaged.PlayDamageEffect();

        if (GameManager.Instance.playerStats.currentHP <= 0)
        {
            GameManager.Instance.playerStats.currentHP = 0;
            // 플레이어 죽음 처리
        }

        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isLive) return;

        if (collision.CompareTag("Player"))
        {
            Explode(transform.position);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, avoidanceRange);
    }
}
