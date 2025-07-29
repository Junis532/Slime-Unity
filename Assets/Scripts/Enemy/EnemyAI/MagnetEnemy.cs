using UnityEngine;

public class MagnetEnemy : EnemyBase
{
    private bool isLive = true;

    private SpriteRenderer spriter;
    private EnemyAnimation enemyAnimation;

    private Vector2 currentVelocity;
    private Vector2 currentDirection;

    public float smoothTime = 0.1f;
    public float detectionRange = 5f;

    [Header("시각적 범위 표시")]
    public GameObject rangeVisualPrefab;
    private GameObject rangeVisualInstance;

    [Header("속도 설정")]
    public float followSpeed = 2f;    // 적이 플레이어를 따라가는 속도
    public float pullForce = 1.5f;    // 플레이어를 끌어당기는 힘

    [Header("회피 관련")]
    public float avoidanceRange = 2f;        // 장애물 감지 범위
    public LayerMask obstacleMask;           // 장애물 레이어 지정

    [Header("행동/멈춤 주기")]
    public float moveDuration = 4f;  // 움직이는 시간
    public float idleDuration = 3f;  // 멈추는 시간

    private float actionTimer = 0f;
    private bool isIdle = false;


    void Start()
    {
        spriter = GetComponent<SpriteRenderer>();
        enemyAnimation = GetComponent<EnemyAnimation>();

        originalSpeed = followSpeed;
        speed = originalSpeed;

        if (rangeVisualPrefab != null)
        {
            rangeVisualInstance = Instantiate(rangeVisualPrefab, transform.position, Quaternion.identity, transform);
            rangeVisualInstance.transform.localScale = Vector3.one * detectionRange * 2f;
        }
    }

    void Update()
    {
        if (!isLive) return;

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        // 🟡 움직임/멈춤 주기 처리
        actionTimer += Time.deltaTime;

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

        Vector2 currentPos = transform.position;
        Vector2 dirToPlayer = ((Vector2)player.transform.position - currentPos);
        float distance = dirToPlayer.magnitude;

        // 좌우 반전
        if (dirToPlayer.x != 0)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (dirToPlayer.x < 0 ? -1 : 1);
            transform.localScale = scale;
        }

        // 장애물 회피 검사
        Vector2 dirNormalized = dirToPlayer.normalized;
        RaycastHit2D hit = Physics2D.Raycast(currentPos, dirNormalized, avoidanceRange, obstacleMask);

        Vector2 avoidanceVector = Vector2.zero;
        if (hit.collider != null)
        {
            Vector2 hitNormal = hit.normal;
            Vector2 sideStep = Vector2.Perpendicular(hitNormal);
            avoidanceVector = sideStep.normalized * 1.5f;
            Debug.DrawRay(currentPos, sideStep * 2f, Color.green);
        }

        Vector2 finalDir = (dirNormalized + avoidanceVector).normalized;

        // 플레이어 끌어당김
        if (distance <= detectionRange)
        {
            Vector3 pullDir = (transform.position - player.transform.position).normalized;
            player.transform.position += pullDir * pullForce * Time.deltaTime;
        }

        // 이동
        currentDirection = Vector2.SmoothDamp(currentDirection, finalDir, ref currentVelocity, smoothTime);
        Vector2 moveVec = currentDirection * followSpeed * Time.deltaTime;
        transform.Translate(moveVec);

        if (currentDirection.magnitude > 0.01f)
        {
            enemyAnimation.PlayAnimation(EnemyAnimation.State.Move);
        }
        else
        {
            enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isLive) return;

        if (collision.CompareTag("Player"))
        {
            int damage = GameManager.Instance.enemyStats.attack;
            GameManager.Instance.playerStats.currentHP -= damage;

            if (GameManager.Instance.playerDamaged != null)
                GameManager.Instance.playerDamaged.PlayDamageEffect();

            if (GameManager.Instance.playerStats.currentHP <= 0)
            {
                GameManager.Instance.playerStats.currentHP = 0;
                // 사망 처리
            }
        }
    }

    private void OnDestroy()
    {
        isLive = false;

        if (rangeVisualInstance != null)
        {
            Destroy(rangeVisualInstance);
        }
    }
}
