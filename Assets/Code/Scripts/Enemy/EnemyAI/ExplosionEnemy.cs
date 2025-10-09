using DG.Tweening;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class ExplosionEnemy : EnemyBase
{
    private bool isLive = true;
    private bool isExploding = false;

    private SpriteRenderer spriter;
    private EnemyAnimation enemyAnimation;
    private NavMeshAgent agent;
    private GameObject player;

    [Header("폭발 관련 설정")]
    public float explosionRange = 1.5f;
    public GameObject explosionEffectPrefab;

    [Header("지연 폭발 모드")]
    public bool useTimedExplosion = false;
    public float explosionDelay = 3f;

    [Header("플레이어 접촉 폭발 설정")]
    public float playerTriggeredExplosionDelay = 1f; // 플레이어 접촉 시 폭발까지 시간

    [Header("깜빡임 설정")]
    public float blinkDuration = 0.2f;
    public float blinkStartTime = 1f;

    private Tween blinkTween;
    private float timer = 0f;
    private bool isTriggeredByPlayer = false;

    void Start()
    {
        spriter = GetComponent<SpriteRenderer>();
        enemyAnimation = GetComponent<EnemyAnimation>();
        agent = GetComponent<NavMeshAgent>();

        originalSpeed = GameManager.Instance.explosionEnemyStats.speed;
        speed = originalSpeed;

        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.speed = speed;

        player = GameObject.FindWithTag("Player");
    }

    void Update()
    {
        if (!isLive || isExploding || player == null) return;

        if (!isTriggeredByPlayer)
        {
            // 플레이어 추적
            agent.SetDestination(player.transform.position);

            // 이동 애니메이션
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

            // 타이머 폭발
            if (useTimedExplosion)
                TimedExplosionUpdate();
            else
                CheckProximityExplosion();
        }
        else
        {
            // 플레이어 접촉 폭발 진행
            PlayerTriggeredExplosionUpdate();
        }
    }

    private void CheckProximityExplosion()
    {
        if (Vector2.Distance(transform.position, player.transform.position) <= explosionRange)
            Explode(transform.position);
    }

    private void TimedExplosionUpdate()
    {
        timer += Time.deltaTime;

        if (timer >= explosionDelay - blinkStartTime && blinkTween == null)
            StartBlinking();

        if (timer >= explosionDelay)
        {
            StopBlinking();
            Explode(transform.position);
        }
    }

    private void PlayerTriggeredExplosionUpdate()
    {
        timer += Time.deltaTime;

        // 폭발까지 깜빡임 유지
        if (blinkTween == null)
            StartBlinking();

        if (timer >= playerTriggeredExplosionDelay)
        {
            StopBlinking();
            Explode(transform.position);
        }
    }

    private void StartBlinking()
    {
        if (spriter == null) return;

        blinkTween = spriter.DOFade(0f, blinkDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutQuad);
    }

    private void StopBlinking()
    {
        if (blinkTween != null && blinkTween.IsActive())
        {
            blinkTween.Kill();
            spriter.color = new Color(spriter.color.r, spriter.color.g, spriter.color.b, 1f);
        }
    }

    private void Explode(Vector3 position)
    {
        if (!isLive || isExploding) return;

        isExploding = true;
        isLive = false;
        StopBlinking();

        if (explosionEffectPrefab != null)
        {
            GameObject effect = Instantiate(explosionEffectPrefab, position, Quaternion.identity);
            Destroy(effect, 0.3f);
        }

        if (player != null && Vector2.Distance(transform.position, player.transform.position) <= explosionRange)
        {
            // 🚨 스킬 사용 중이면 데미지 무시
            if (GameManager.Instance.joystickDirectionIndicator.IsUsingSkill)
            {
                Debug.Log("스킬 사용 중이라 폭발 데미지 무시");
                return; // 폭발 처리가 루프나 함수 내부에 있다고 가정하고 return 또는 break 사용
            }

            int damage = GameManager.Instance.explosionEnemyStats.attack;

            // 넉백 방향 계산을 위해 폭발한 몬스터의 위치를 '적 위치'로 전달합니다.
            Vector3 enemyPosition = transform.position;

            // 수정된 PlayerDamaged.TakeDamage(데미지, 적 위치) 형식으로 호출
            // 기존의 playerCollider와 contactPoint 인수는 제거됩니다.
            GameManager.Instance.playerDamaged.TakeDamage(damage, enemyPosition);
        }

        Destroy(gameObject);
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isLive || isExploding) return;

        if (collision.CompareTag("Player"))
        {
            agent.isStopped = true;
            isTriggeredByPlayer = true;
            timer = 0f;

            StartBlinking();
        }
    }

    private void OnDestroy()
    {
        StopBlinking();
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, explosionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRange);
    }
}
