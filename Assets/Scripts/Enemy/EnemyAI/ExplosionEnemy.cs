using DG.Tweening;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class ExplosionEnemy : EnemyBase
{
    private bool isLive = true;

    private SpriteRenderer spriter;
    private EnemyAnimation enemyAnimation;
    private NavMeshAgent agent;

    public float explosionRange = 1.5f;
    public GameObject explosionEffectPrefab;

    void Start()
    {
        spriter = GetComponent<SpriteRenderer>();
        enemyAnimation = GetComponent<EnemyAnimation>();
        agent = GetComponent<NavMeshAgent>();

        originalSpeed = GameManager.Instance.enemyStats.speed;
        speed = originalSpeed;

        // NavMeshAgent 설정
        agent.updateRotation = false;
        agent.updateUpAxis = false; // 2D용
        agent.speed = speed;
    }

    void Update()
    {
        if (!isLive) return;

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);

        // 플레이어와의 거리 확인 후 폭발
        if (distanceToPlayer <= explosionRange)
        {
            Explode(transform.position);
            return;
        }

        // 플레이어 쫓아감
        agent.SetDestination(player.transform.position);

        // 애니메이션 처리
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
    }
}
