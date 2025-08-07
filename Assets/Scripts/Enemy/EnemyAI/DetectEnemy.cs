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
        float distance = 0f;

        if (player != null)
        {
            Vector2 toPlayer = (Vector2)player.transform.position - (Vector2)transform.position;
            distance = toPlayer.magnitude;

            if (toPlayer.x != 0)
            {
                Vector3 scale = transform.localScale;
                scale.x = Mathf.Abs(scale.x) * (toPlayer.x < 0 ? -1 : 1);
                transform.localScale = scale;
            }

            if (!hasDetectedPlayer && distance <= detectionRange)
            {
                hasDetectedPlayer = true;
            }
        }

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

        // 애니메이션 처리
        if (agent.velocity.magnitude > 0.1f)
        {
            enemyAnimation.PlayAnimation(EnemyAnimation.State.Move);
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
            int damage = GameManager.Instance.enemyStats.attack;
            GameManager.Instance.playerStats.currentHP -= damage;
            GameManager.Instance.playerDamaged.PlayDamageEffect();

            if (GameManager.Instance.playerStats.currentHP <= 0)
                GameManager.Instance.playerStats.currentHP = 0;
        }
    }

    private void OnDestroy()
    {
        isLive = false;
    }
}
