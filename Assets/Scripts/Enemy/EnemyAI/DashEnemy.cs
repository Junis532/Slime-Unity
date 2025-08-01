using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class DashEnemy : EnemyBase
{
    private bool isLive = true;

    private SpriteRenderer spriter;
    private EnemyAnimation enemyAnimation;

    private NavMeshAgent agent;

    public float dashSpeed = 20f;
    public float dashCooldown = 3f;
    public float pauseBeforeDash = 0.3f;
    public float dashDuration = 0.3f;

    private float dashTimer = 0f;
    private float dashTimeElapsed = 0f;
    private float pauseTimer = 0f;

    private bool isPreparingToDash = false;
    private bool isDashing = false;
    private Vector2 dashDirection;

    [Header("대시 프리뷰 스프라이트")]
    public GameObject dashPreviewPrefab;
    public float previewDistanceFromEnemy = 0f;
    public float previewBackOffset = 0f;

    private GameObject dashPreviewInstance;

    [Header("벽 레이어 마스크")]
    public LayerMask wallLayerMask;

    [Header("대시 후 정지 관련")]
    private bool isCooldownStopped = false;
    private float cooldownStopTimer = 0f;
    public float cooldownStopDuration = 3f;

    void Start()
    {
        spriter = GetComponent<SpriteRenderer>();
        enemyAnimation = GetComponent<EnemyAnimation>();
        agent = GetComponent<NavMeshAgent>();

        originalSpeed = GameManager.Instance.dashEnemyStats.speed;
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

        // 쿨다운 정지 처리
        if (isCooldownStopped)
        {
            cooldownStopTimer += Time.deltaTime;
            enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);
            if (dashPreviewInstance != null) dashPreviewInstance.SetActive(false);

            if (cooldownStopTimer >= cooldownStopDuration)
            {
                isCooldownStopped = false;
                cooldownStopTimer = 0f;
                dashTimer = 0f;
                agent.enabled = true;
            }
            return;
        }

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        Vector2 toPlayer = (player.transform.position - transform.position).normalized;

        if (isDashing)
        {
            DashMove();
            dashTimeElapsed += Time.deltaTime;

            enemyAnimation.PlayAnimation(EnemyAnimation.State.Move);
            FlipSprite(dashDirection.x);

            if (dashTimeElapsed >= dashDuration)
            {
                EndDash();
            }
            return;
        }

        if (isPreparingToDash)
        {
            pauseTimer += Time.deltaTime;
            enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);

            if (dashPreviewInstance != null)
            {
                dashPreviewInstance.SetActive(true);
                Vector3 dir = new Vector3(dashDirection.x, dashDirection.y, 0f).normalized;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                dashPreviewInstance.transform.rotation = Quaternion.Euler(0f, 0f, angle);

                Vector3 basePos = transform.position + dir * previewDistanceFromEnemy;
                Vector3 offset = -dashPreviewInstance.transform.up * previewBackOffset;
                dashPreviewInstance.transform.position = basePos + offset;
            }

            if (pauseTimer >= pauseBeforeDash)
            {
                isPreparingToDash = false;
                isDashing = true;
                pauseTimer = 0f;

                if (dashPreviewInstance != null)
                    dashPreviewInstance.SetActive(false);

                agent.enabled = false; // NavMeshAgent 비활성화
            }
            return;
        }

        // 일반 NavMesh 이동
        if (agent.enabled)
        {
            agent.SetDestination(player.transform.position);

            Vector2 dir = agent.velocity;
            if (dir.magnitude > 0.1f)
            {
                enemyAnimation.PlayAnimation(EnemyAnimation.State.Move);
                FlipSprite(dir.x);
            }
            else
            {
                enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);
            }
        }

        // 대시 타이머
        dashTimer += Time.deltaTime;
        if (dashTimer >= dashCooldown)
        {
            isPreparingToDash = true;
            pauseTimer = 0f;
            dashDirection = toPlayer;
            return;
        }
    }

    private void DashMove()
    {
        Vector2 moveVec = dashDirection * dashSpeed * Time.deltaTime;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dashDirection, moveVec.magnitude, wallLayerMask);

        if (hit.collider != null)
        {
            transform.position = hit.point - dashDirection.normalized * 0.01f;
            EndDash();
        }
        else
        {
            transform.Translate(moveVec);
        }
    }

    private void EndDash()
    {
        isDashing = false;
        dashTimeElapsed = 0f;

        // 대시 후 쿨다운 정지
        isCooldownStopped = true;
        cooldownStopTimer = 0f;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isLive) return;

        if (collision.CompareTag("Player"))
        {
            int damage = GameManager.Instance.dashEnemyStats.attack;
            GameManager.Instance.playerStats.currentHP -= damage;
            GameManager.Instance.playerDamaged.PlayDamageEffect();

            if (GameManager.Instance.playerStats.currentHP <= 0)
            {
                GameManager.Instance.playerStats.currentHP = 0;
                // 죽음 처리
            }
        }
    }

    public void Knockback(Vector2 force)
    {
        if (isPreparingToDash)
        {
            Debug.Log("대시 준비 중이라 넉백 무시");
            return;
        }

        transform.position += (Vector3)force;
    }

    private void FlipSprite(float directionX)
    {
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (directionX < 0 ? -1 : 1);
        transform.localScale = scale;
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
