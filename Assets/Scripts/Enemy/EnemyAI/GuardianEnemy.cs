using DG.Tweening;
using System.Collections;
using UnityEngine;

public class GuardianEnemy : EnemyBase
{
    private bool isLive = true;

    private SpriteRenderer spriter;
    private EnemyAnimation enemyAnimation;

    private Vector2 currentVelocity;
    private Vector2 currentDirection;

    public float smoothTime = 0.1f;
    public float fireRange = 5f;
    private GameObject player;
    private LineRenderer laserLineRenderer;

    [Header("레이저 선 설정")]
    public Color laserColor = Color.red;
    public float laserWidth = 0.1f;

    // 장애물 회피
    [Header("회피 관련")]
    public float avoidanceRange = 1.5f;
    public LayerMask obstacleMask;

    // 데미지 루틴
    private bool isDamaging = false;

    // 행동/멈춤 주기용
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

        player = GameObject.FindWithTag("Player");

        // 레이저 세팅
        laserLineRenderer = gameObject.AddComponent<LineRenderer>();
        laserLineRenderer.positionCount = 2;
        laserLineRenderer.enabled = false;
        laserLineRenderer.startWidth = laserWidth;
        laserLineRenderer.endWidth = laserWidth;

        Material laserMat = new Material(Shader.Find("Unlit/GlowLine"));
        laserMat.SetColor("_Color", laserColor);
        laserMat.SetColor("_EmissionColor", laserColor * 5f);
        laserLineRenderer.material = laserMat;

        laserLineRenderer.startColor = laserColor;
        laserLineRenderer.endColor = laserColor;
        laserLineRenderer.sortingLayerName = "Default";
        laserLineRenderer.sortingOrder = 10;
    }

    void Update()
    {
        if (!isLive || player == null) return;

        actionTimer += Time.deltaTime;

        if (isIdle)
        {
            if (actionTimer >= idleDuration)
            {
                isIdle = false;
                actionTimer = 0f;
            }

            enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);
            laserLineRenderer.enabled = false;
            // 멈춤 상태이므로 데미지 코루틴도 중지
            if (isDamaging)
            {
                isDamaging = false;
                StopAllCoroutines();
            }
            return;
        }
        else
        {
            if (actionTimer >= moveDuration)
            {
                isIdle = true;
                actionTimer = 0f;
                enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);
                laserLineRenderer.enabled = false;
                if (isDamaging)
                {
                    isDamaging = false;
                    StopAllCoroutines();
                }
                return;
            }
        }

        // ----------------- 이동 및 회피 -----------------
        Vector2 currentPos = transform.position;
        Vector2 dirToPlayer = ((Vector2)player.transform.position - currentPos).normalized;

        RaycastHit2D hit = Physics2D.Raycast(currentPos, dirToPlayer, avoidanceRange, obstacleMask);
        Vector2 avoidanceVector = Vector2.zero;

        if (hit.collider != null)
        {
            Vector2 hitNormal = hit.normal;
            Vector2 sideStep = Vector2.Perpendicular(hitNormal);
            avoidanceVector = sideStep.normalized * 1.5f;

            Debug.DrawRay(currentPos, sideStep * 2, Color.green);
        }

        Vector2 finalDir = (dirToPlayer + avoidanceVector).normalized;

        currentDirection = Vector2.SmoothDamp(currentDirection, finalDir, ref currentVelocity, smoothTime);
        Vector2 nextVec = currentDirection * speed * Time.deltaTime;
        transform.Translate(nextVec);

        // ----------------- 회전 및 애니메이션 -----------------
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

        // ----------------- 레이저 공격 -----------------
        float distance = Vector2.Distance(player.transform.position, transform.position);

        if (distance <= fireRange)
        {
            if (!laserLineRenderer.enabled)
                laserLineRenderer.enabled = true;

            Vector3 startPos = transform.position;
            Vector3 endPos = player.transform.position;
            startPos.z = -1f;
            endPos.z = -1f;

            laserLineRenderer.SetPosition(0, startPos);
            laserLineRenderer.SetPosition(1, endPos);

            if (!isDamaging)
            {
                isDamaging = true;
                StartCoroutine(DealDamageRoutine());
            }
        }
        else
        {
            if (laserLineRenderer.enabled)
                laserLineRenderer.enabled = false;

            if (isDamaging)
            {
                isDamaging = false;
                StopAllCoroutines();
            }
        }
    }

    private IEnumerator DealDamageRoutine()
    {
        while (isDamaging)
        {
            if (player == null) yield break;
            yield return new WaitForSeconds(1f);

            var playerStats = GameManager.Instance.playerStats;
            playerStats.currentHP -= 1;
            GameManager.Instance.playerDamaged.PlayDamageEffect();

            if (playerStats.currentHP <= 0)
            {
                playerStats.currentHP = 0;
                // 사망 처리
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, avoidanceRange);
    }
}
