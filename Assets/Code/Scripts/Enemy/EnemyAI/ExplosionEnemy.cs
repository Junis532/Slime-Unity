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
    public float explosionRange = 1.5f;              // 💡 인스펙터에서 조정 가능
    public GameObject explosionEffectPrefab;
    // explosionDamage 변수는 Stats로 대체
    // public int explosionDamage = 100;

    [Header("지연 폭발 모드")]
    public bool useTimedExplosion = false;          // 🔛 켜면 일정 시간 뒤 폭발
    public float explosionDelay = 3f;               // 🔢 몇 초 뒤에 폭발할지

    [Header("깜빡임 설정")]
    public float blinkDuration = 0.2f;              // 깜빡이는 간격
    public float blinkStartTime = 1f;               // 폭발 전 몇 초부터 깜빡임 시작

    private Tween blinkTween;
    private float timer = 0f;
    private bool isTriggeredByPlayer = false;       // 💡 플레이어 접촉 여부

    void Start()
    {
        spriter = GetComponent<SpriteRenderer>();
        enemyAnimation = GetComponent<EnemyAnimation>();
        agent = GetComponent<NavMeshAgent>();

        originalSpeed = GameManager.Instance.explosionEnemyStats.speed;
        speed = originalSpeed;

        // NavMeshAgent 설정 (2D 대응)
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.speed = speed;

        player = GameObject.FindWithTag("Player");
    }

    void Update()
    {
        if (!isLive || isExploding) return;
        if (player == null) return;

        // 🧠 플레이어 추적 (계속 이동)
        agent.SetDestination(player.transform.position);

        // 🌀 이동 애니메이션 처리
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

        // 💣 폭발 조건 처리
        if (isTriggeredByPlayer)
        {
            TriggeredExplosionUpdate();
        }
        else if (useTimedExplosion)
        {
            TimedExplosionUpdate();
        }
        else
        {
            CheckProximityExplosion();
        }
    }

    // 🔹 거리 기반 폭발
    private void CheckProximityExplosion()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);
        if (distanceToPlayer <= explosionRange)
        {
            Explode(transform.position);
        }
    }

    // 🔹 타이머 기반 폭발
    private void TimedExplosionUpdate()
    {
        timer += Time.deltaTime;

        if (timer >= explosionDelay - blinkStartTime && blinkTween == null)
        {
            StartBlinking();
        }

        if (timer >= explosionDelay)
        {
            StopBlinking();
            Explode(transform.position);
        }
    }

    // 🔹 플레이어 접촉 후 폭발 시퀀스
    private void TriggeredExplosionUpdate()
    {
        timer += Time.deltaTime;

        if (timer >= explosionDelay - blinkStartTime && blinkTween == null)
        {
            StartBlinking();
        }

        if (timer >= explosionDelay)
        {
            StopBlinking();
            Explode(transform.position);
        }
    }

    // 🔸 DOTween 깜빡임 시작
    private void StartBlinking()
    {
        if (spriter == null) return;

        blinkTween = spriter.DOFade(0f, blinkDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutQuad);
    }

    // 🔸 깜빡임 중지
    private void StopBlinking()
    {
        if (blinkTween != null && blinkTween.IsActive())
        {
            blinkTween.Kill();
            spriter.color = new Color(spriter.color.r, spriter.color.g, spriter.color.b, 1f);
        }
    }

    // 💥 실제 폭발 처리
    private void Explode(Vector3 position)
    {
        if (!isLive || isExploding) return;
        isExploding = true;
        isLive = false;

        StopBlinking();

        // 이펙트 생성
        if (explosionEffectPrefab != null)
        {
            GameObject effect = Instantiate(explosionEffectPrefab, position, Quaternion.identity);
            Destroy(effect, 0.5f);
        }

        // 💡 플레이어가 범위 내에 있을 때만 데미지 적용
        if (player != null)
        {
            float distance = Vector2.Distance(transform.position, player.transform.position);
            if (distance <= explosionRange)
            {
                int damage = GameManager.Instance.explosionEnemyStats.attack;
                GameManager.Instance.playerDamaged.TakeDamage(damage);
            }
        }

        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isLive || isExploding) return;

        if (collision.CompareTag("Player"))
        {
            if (useTimedExplosion)
            {
                // 닿는 순간 폭발까지 남은 시간을 깜빡임 구간만 남기기
                timer = explosionDelay - blinkStartTime;

                if (blinkTween == null)
                    StartBlinking();
            }
            else
            {
                isTriggeredByPlayer = true;
                timer = 0f;
            }
        }
    }

    private void OnDestroy()
    {
        StopBlinking();
    }

    // 🔹 폭발 범위 시각화
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // 반투명 구
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, explosionRange);

        // 테두리 강조
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRange);
    }
}
