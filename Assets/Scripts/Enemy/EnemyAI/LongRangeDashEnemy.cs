using DG.Tweening;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NavLongRangeDashEnemy : EnemyBase
{
    private bool isLive = true;

    private SpriteRenderer spriter;
    private EnemyAnimation enemyAnimation;
    private NavMeshAgent agent;

    [Header("대시 설정")]
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

    [Header("대시 후 정지")]
    public float postDashIdleDuration = 3f;
    private bool isIdle = false;
    private float idleTimer = 0f;

    [Header("대시 프리뷰")]
    public GameObject dashPreviewPrefab;
    public float previewDistanceFromEnemy = 0f;
    public float previewBackOffset = 0f;
    private GameObject dashPreviewInstance;

    [Header("벽 체크")]
    public LayerMask wallLayerMask;

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

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        Vector2 toPlayer = (player.transform.position - transform.position).normalized;

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
                dashTimer = 0f;
                agent.enabled = true;
            }
            return;
        }

        if (isDashing)
        {
            DashMove();
            dashTimeElapsed += Time.deltaTime;
            enemyAnimation.PlayAnimation(EnemyAnimation.State.Move);
            FlipSprite(dashDirection.x);

            if (dashTimeElapsed >= dashDuration)
                EndDash();
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
                lastDashFireTime = 0f;
                agent.enabled = false;

                if (dashPreviewInstance != null)
                    dashPreviewInstance.SetActive(false);
            }
            return;
        }

        // 일반 이동
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
        isIdle = true;
        idleTimer = 0f;
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
                GameManager.Instance.playerStats.currentHP = 0;
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

        if (agent != null)
            agent.enabled = true;
    }

    void OnDestroy()
    {
        if (dashPreviewInstance != null)
            Destroy(dashPreviewInstance);
    }
}
