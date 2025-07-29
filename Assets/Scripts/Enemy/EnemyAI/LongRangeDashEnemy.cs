using DG.Tweening;
using System.Collections;
using UnityEngine;

public class LongRangeDashEnemy : EnemyBase
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

    // --- 대쉬 후 멈춤 상태 관련 ---
    public float postDashIdleDuration = 3f;  // 대쉬 후 멈춤 시간
    private bool isIdle = false;
    private float idleTimer = 0f;

    [Header("대시 프리뷰")]
    public GameObject dashPreviewPrefab;
    public float previewDistanceFromEnemy = 0f;
    public float previewBackOffset = 0f;
    private GameObject dashPreviewInstance;

    [Header("벽 체크")]
    public LayerMask wallLayerMask;

    [Header("회피 관련")]
    public float avoidanceRange = 1.5f;
    public LayerMask obstacleMask;

    [Header("대시 중 총알")]
    public GameObject bulletPrefab;
    public float bulletSpeed = 3f;
    public float bulletLifetime = 2f;
    public int bulletsPerSide = 3;
    public float sideBulletAngleStep = 10f;
    public float dashFireCooldown = 0.1f;
    private float lastDashFireTime = 0f;

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

        // 대쉬 후 멈춤 처리
        if (isIdle)
        {
            idleTimer += Time.deltaTime;
            enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);

            if (dashPreviewInstance != null)
                dashPreviewInstance.SetActive(false);

            if (idleTimer >= postDashIdleDuration)
            {
                isIdle = false;
                idleTimer = 0f;
                dashTimer = 0f;  // 대쉬 쿨다운도 초기화
            }
            return;
        }

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        Vector2 currentPos = transform.position;
        Vector2 toPlayer = (player.transform.position - transform.position);
        Vector2 inputVec = toPlayer.normalized;

        // 대시 중이면 DashMove()
        if (isDashing)
        {
            DashMove();
            dashTimeElapsed += Time.deltaTime;
            enemyAnimation.PlayAnimation(EnemyAnimation.State.Move);
            FlipSprite(dashDirection.x);

            if (dashTimeElapsed >= dashDuration)
            {
                EndDash();
                // 대시가 끝나면 멈춤 상태로 전환
                isIdle = true;
                idleTimer = 0f;
            }
            return;
        }

        // 대시 준비 상태
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
                lastDashFireTime = 0f;

                if (dashPreviewInstance != null)
                    dashPreviewInstance.SetActive(false);
            }
            return;
        }

        // 일반 이동 + 회피
        Vector2 avoidanceVec = Vector2.zero;
        RaycastHit2D hit = Physics2D.Raycast(currentPos, inputVec, avoidanceRange, obstacleMask);
        if (hit.collider != null)
        {
            Vector2 normal = hit.normal;
            Vector2 sideStep = Vector2.Perpendicular(normal).normalized;
            avoidanceVec = sideStep * 1.5f;

            Debug.DrawRay(currentPos, sideStep * 2, Color.green);
        }

        Vector2 finalDir = (inputVec + avoidanceVec).normalized;
        currentDirection = Vector2.SmoothDamp(currentDirection, finalDir, ref currentVelocity, smoothTime);
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

        if (dashPreviewInstance != null)
            dashPreviewInstance.SetActive(false);

        // 대시 타이머 체크 (멈춤 상태가 아닐 때만)
        dashTimer += Time.deltaTime;
        if (dashTimer >= dashCooldown)
        {
            isPreparingToDash = true;
            dashDirection = inputVec;
            pauseTimer = 0f;
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
            // 대시가 벽에 부딪혀도 멈춤 상태로 전환
            isIdle = true;
            idleTimer = 0f;
        }
        else
        {
            transform.Translate(moveVec);

            if (Time.time - lastDashFireTime >= dashFireCooldown)
            {
                FireBulletsSideways(dashDirection);
                lastDashFireTime = Time.time;
            }
        }
    }

    private void EndDash()
    {
        isDashing = false;
        dashTimeElapsed = 0f;
        dashTimer = 0f;
        currentDirection = Vector2.zero;
        currentVelocity = Vector2.zero;
    }

    private void FireBulletsSideways(Vector2 centerDirection)
    {
        for (int i = 1; i <= bulletsPerSide; i++)
        {
            float angle = sideBulletAngleStep * i;
            SpawnBullet(Quaternion.Euler(0, 0, angle) * centerDirection);
            SpawnBullet(Quaternion.Euler(0, 0, -angle) * centerDirection);
        }
    }

    private void SpawnBullet(Vector2 direction)
    {
        GameObject bullet = PoolManager.Instance.SpawnFromPool(bulletPrefab.name, transform.position, Quaternion.identity);
        if (bullet != null)
        {
            BulletBehavior bulletBehavior = bullet.GetComponent<BulletBehavior>();
            if (bulletBehavior == null)
                bulletBehavior = bullet.AddComponent<BulletBehavior>();

            bulletBehavior.Initialize(direction.normalized, bulletSpeed, bulletLifetime);
        }
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
            }
        }
    }

    private void FlipSprite(float dirX)
    {
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (dirX < 0 ? -1 : 1);
        transform.localScale = scale;
    }

    void OnDisable()
    {
        if (dashPreviewInstance != null)
            dashPreviewInstance.SetActive(false);

        StopAllCoroutines();
        isPreparingToDash = false;
        isDashing = false;
        dashTimer = 0f;
        pauseTimer = 0f;
        isIdle = false;
        idleTimer = 0f;
    }

    void OnDestroy()
    {
        if (dashPreviewInstance != null)
            Destroy(dashPreviewInstance);
    }
}
