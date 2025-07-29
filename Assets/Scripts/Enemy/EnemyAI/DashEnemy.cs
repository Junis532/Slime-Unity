using DG.Tweening;
using System.Collections;
using UnityEngine;

public class DashEnemy : EnemyBase
{
    private bool isLive = true;

    private SpriteRenderer spriter;
    private EnemyAnimation enemyAnimation;

    private Vector2 currentVelocity;
    private Vector2 currentDirection;

    public float smoothTime = 0.1f;

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

    [Header("회피 관련")]
    public float avoidanceRange = 1.5f;
    public LayerMask obstacleMask;

    [Header("대시 후 정지 관련")]
    private bool isCooldownStopped = false;
    private float cooldownStopTimer = 0f;
    public float cooldownStopDuration = 3f;

    void Start()
    {
        spriter = GetComponent<SpriteRenderer>();
        enemyAnimation = GetComponent<EnemyAnimation>();

        originalSpeed = GameManager.Instance.dashEnemyStats.speed;
        speed = originalSpeed;

        if (dashPreviewPrefab != null)
        {
            dashPreviewInstance = Instantiate(dashPreviewPrefab, transform.position, Quaternion.identity);
            dashPreviewInstance.SetActive(false);
        }
    }

    void Update()
    {
        if (!isLive) return;

        // 대시 후 정지 상태 처리
        if (isCooldownStopped)
        {
            cooldownStopTimer += Time.deltaTime;
            enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);

            if (dashPreviewInstance != null)
                dashPreviewInstance.SetActive(false);

            if (cooldownStopTimer >= cooldownStopDuration)
            {
                isCooldownStopped = false;
                cooldownStopTimer = 0f;
                dashTimer = 0f;
            }

            return;
        }

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        Vector2 currentPos = transform.position;
        Vector2 dirVec = (player.transform.position - transform.position);
        Vector2 inputVec = dirVec.normalized;

        // 대시 중
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

        // 대시 준비 중
        if (isPreparingToDash)
        {
            pauseTimer += Time.deltaTime;
            enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);

            if (dashPreviewInstance != null)
            {
                dashPreviewInstance.SetActive(true);

                Vector3 direction = new Vector3(dashDirection.x, dashDirection.y, 0f).normalized;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
                dashPreviewInstance.transform.rotation = Quaternion.Euler(0f, 0f, angle);

                Vector3 basePos = transform.position + direction * previewDistanceFromEnemy;
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
            }

            return;
        }

        // 일반 이동 + 회피
        Vector2 avoidanceVec = Vector2.zero;
        RaycastHit2D hitAvoid = Physics2D.Raycast(currentPos, inputVec, avoidanceRange, obstacleMask);
        if (hitAvoid.collider != null)
        {
            Vector2 hitNormal = hitAvoid.normal;
            Vector2 sideStep = Vector2.Perpendicular(hitNormal);
            avoidanceVec = sideStep.normalized * 1.5f;
            Debug.DrawRay(currentPos, sideStep * 2, Color.green);
        }

        Vector2 finalMoveDir = (inputVec + avoidanceVec).normalized;
        currentDirection = Vector2.SmoothDamp(currentDirection, finalMoveDir, ref currentVelocity, smoothTime);
        Vector2 nextVec = currentDirection * speed * Time.deltaTime;
        transform.Translate(nextVec);

        if (currentDirection.magnitude > 0.01f)
        {
            enemyAnimation.PlayAnimation(EnemyAnimation.State.Move);
            FlipSprite(currentDirection.x);
        }
        else
        {
            enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);
        }

        if (dashPreviewInstance != null && !isPreparingToDash)
            dashPreviewInstance.SetActive(false);

        // 대시 타이머
        dashTimer += Time.deltaTime;
        if (dashTimer >= dashCooldown)
        {
            isPreparingToDash = true;
            pauseTimer = 0f;
            dashDirection = inputVec;
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
        currentDirection = Vector2.zero;
        currentVelocity = Vector2.zero;

        // 3초 정지 시작
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
                // 플레이어 죽음 처리
            }
        }
    }

    public void Knockback(Vector2 force)
    {
        if (isPreparingToDash)
        {
            Debug.Log("대쉬 준비 중이라 넉백 무시");
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
        {
            dashPreviewInstance.SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (dashPreviewInstance != null)
        {
            Destroy(dashPreviewInstance);
        }
    }
}
