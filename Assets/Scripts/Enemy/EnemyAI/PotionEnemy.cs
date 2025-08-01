using UnityEngine;
using UnityEngine.AI;
using DG.Tweening;

[RequireComponent(typeof(NavMeshAgent))]
public class PotionEnemy : EnemyBase
{
    private bool isLive = true;

    private SpriteRenderer spriter;
    private EnemyAnimation enemyAnimation;
    private NavMeshAgent agent;

    public float stopCooldown = 3f;
    public float stopDuration = 0.5f;
    private float stopTimer = 0f;
    private float pauseTimer = 0f;
    private bool isStopping = false;

    [Header("범위 표시 프리팹")]
    public GameObject dashPreviewPrefab;
    public float previewDistanceFromEnemy = 0f;
    public float previewBackOffset = 0f;
    private GameObject dashPreviewInstance;

    [Header("포션 관련")]
    public GameObject potionPrefab;
    public float potionLifetime = 2f;

    void Start()
    {
        spriter = GetComponent<SpriteRenderer>();
        enemyAnimation = GetComponent<EnemyAnimation>();
        agent = GetComponent<NavMeshAgent>();

        originalSpeed = GameManager.Instance.potionEnemyStats.speed;
        speed = originalSpeed;

        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.speed = speed;

        if (dashPreviewPrefab != null)
        {
            dashPreviewInstance = Instantiate(dashPreviewPrefab, transform.position, Quaternion.identity);
            dashPreviewInstance.SetActive(false);
        }
    }

    void Update()
    {
        if (!isLive) return;

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        stopTimer += Time.deltaTime;

        if (isStopping)
        {
            pauseTimer += Time.deltaTime;
            agent.isStopped = true;
            enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);

            Vector3 dir = (player.transform.position - transform.position).normalized;

            if (dashPreviewInstance != null)
            {
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                dashPreviewInstance.transform.rotation = Quaternion.Euler(0f, 0f, angle);

                Vector3 basePos = transform.position + dir * previewDistanceFromEnemy;
                Vector3 offset = -dashPreviewInstance.transform.up * previewBackOffset;
                dashPreviewInstance.transform.position = basePos + offset;
                dashPreviewInstance.SetActive(true);
            }

            if (pauseTimer >= stopDuration)
            {
                isStopping = false;
                pauseTimer = 0f;
                stopTimer = 0f;

                if (potionPrefab != null)
                {
                    GameObject potion = PoolManager.Instance.SpawnFromPool(potionPrefab.name, transform.position, Quaternion.identity);
                    if (potion != null)
                    {
                        var pb = potion.GetComponent<PotionBehavior>();
                        if (pb != null)
                            pb.StartLifetime(potionLifetime);
                    }
                }

                if (dashPreviewInstance != null)
                    dashPreviewInstance.SetActive(false);
            }

            return;
        }

        if (stopTimer >= stopCooldown)
        {
            isStopping = true;
            pauseTimer = 0f;
            return;
        }

        agent.isStopped = false;
        agent.SetDestination(player.transform.position);

        Vector2 velocity = agent.velocity;

        if (velocity.magnitude > 0.1f)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (velocity.x < 0 ? -1 : 1);
            transform.localScale = scale;

            enemyAnimation.PlayAnimation(EnemyAnimation.State.Move);
        }
        else
        {
            enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);
        }

        if (dashPreviewInstance != null && !isStopping)
        {
            dashPreviewInstance.SetActive(false);
        }
    }

    void OnDisable()
    {
        if (dashPreviewInstance != null)
            dashPreviewInstance.SetActive(false);
    }

    void OnDestroy()
    {
        if (dashPreviewInstance != null)
            Destroy(dashPreviewInstance);
    }
}
