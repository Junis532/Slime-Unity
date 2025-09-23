using UnityEngine;
using UnityEngine.AI;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
public class NavPotionDashEnemy : EnemyBase
{
    private bool isLive = true;

    private SpriteRenderer spriter;
    private EnemyAnimation enemyAnimation;
    private NavMeshAgent agent;

    [Header("대시 관련")]
    public float dashSpeed = 20f;
    public float dashCooldown = 3f;
    public float pauseBeforeDash = 0.3f;
    public float dashDuration = 0.3f;

    private float dashTimer = 0f;
    private float dashTimeElapsed = 0f;
    private float pauseTimer = 0f;
    private bool isPreparingToDash = false;
    private bool isDashing = false;
    private bool isPausingAfterDash = false;
    private Vector2 dashDirection;
    private Vector2 dashEndPosition;

    [Header("대시 프리뷰")]
    public GameObject dashPreviewPrefab;
    public float previewDistanceFromEnemy = 0f;
    public float previewBackOffset = 0f;
    private GameObject dashPreviewInstance;

    [Header("포션 경고 및 생성")]
    public GameObject potionWarningPrefab;
    public GameObject potionDamagePrefab;
    public float potionWarningOffset = 0f;
    public float potionWarningDuration = 1f;
    public float potionLifetime = 2f;
    public float potionSpreadRadius = 1f;
    public int numberOfPotionsToSpawn = 6;

    private List<Vector3> warningPositions = new();
    private List<GameObject> warningInstances = new();

    [Header("레이어")]
    public LayerMask wallLayerMask;

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

        if (isPausingAfterDash)
            return;

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

                if (dashPreviewInstance != null)
                    dashPreviewInstance.SetActive(false);

                agent.enabled = false;
            }
            return;
        }

        // 일반 NavMeshAgent 이동
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
        }
    }

    private void EndDash()
    {
        isDashing = false;
        dashTimeElapsed = 0f;
        dashTimer = 0f;

        dashEndPosition = transform.position;

        StartCoroutine(ShowPotionWarningsThenSpawn());

        agent.enabled = true;
    }

    private IEnumerator ShowPotionWarningsThenSpawn()
    {
        isPausingAfterDash = true;

        SpawnPotionWarnings(dashEndPosition);

        yield return new WaitForSeconds(potionWarningDuration);

        SpawnPotionsAtWarnings();
        ClearPotionWarnings();

        isPausingAfterDash = false;
    }

    private void SpawnPotionWarnings(Vector2 center)
    {
        ClearPotionWarnings();

        float angleStep = 360f / numberOfPotionsToSpawn;

        for (int i = 0; i < numberOfPotionsToSpawn; i++)
        {
            float angle = i * angleStep;
            Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            Vector3 pos = center + dir * potionSpreadRadius + new Vector2(0f, potionWarningOffset);

            GameObject warning = Instantiate(potionWarningPrefab, pos, Quaternion.identity);
            warning.SetActive(true);

            warningInstances.Add(warning);
            warningPositions.Add(pos);
        }
    }

    private void SpawnPotionsAtWarnings()
    {
        foreach (Vector3 pos in warningPositions)
        {
            GameObject potion = Instantiate(potionDamagePrefab, pos, Quaternion.identity);
            if (potion.TryGetComponent(out PotionBehavior pb))
                pb.StartLifetime(potionLifetime);
        }
    }

    private void ClearPotionWarnings()
    {
        foreach (var go in warningInstances)
        {
            if (go != null)
                Destroy(go);
        }
        warningInstances.Clear();
        warningPositions.Clear();
    }

    public void Knockback(Vector2 force)
    {
        if (isPreparingToDash || isDashing || isPausingAfterDash)
        {
            Debug.Log("대시 상태 중이라 넉백 무시");
            return;
        }

        transform.position += (Vector3)force;
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

        ClearPotionWarnings();
        StopAllCoroutines();

        isPreparingToDash = false;
        isDashing = false;
        isPausingAfterDash = false;
    }

    void OnDestroy()
    {
        if (dashPreviewInstance != null)
            Destroy(dashPreviewInstance);

        ClearPotionWarnings();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isLive) return;

        if (collision.CompareTag("Player"))
        {
            if (GameManager.Instance.joystickDirectionIndicator.IsUsingSkill)
            {
                Debug.Log("스킬 사용 중이라 몬스터 데미지 무시");
                return;
            }

            // ✅ 이제는 PlayerDamaged 쪽에 위임
            int damage = GameManager.Instance.potionEnemyStats.attack;
            GameManager.Instance.playerDamaged.TakeDamage(damage);
        }
    }

}
