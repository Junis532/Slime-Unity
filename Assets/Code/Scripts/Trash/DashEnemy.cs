//using DG.Tweening;
//using System.Collections;
//using UnityEngine;
//using UnityEngine.AI;

//[RequireComponent(typeof(NavMeshAgent))]
//public class DashEnemy : EnemyBase
//{
//    private bool isLive = true;

//    private SpriteRenderer spriter;
//    private EnemyAnimation enemyAnimation;
//    private NavMeshAgent agent;

//    public float dashSpeed = 20f;
//    public float dashCooldown = 3f;
//    public float pauseBeforeDash = 0.3f;
//    public float dashDuration = 0.3f;

//    private float dashTimer = 0f;
//    private float dashTimeElapsed = 0f;
//    private float pauseTimer = 0f;

//    private bool isPreparingToDash = false;
//    private bool isDashing = false;
//    private Vector2 dashDirection;

//    [Header("대시 프리뷰 스프라이트")]
//    public GameObject dashPreviewPrefab;
//    public float previewDistanceFromEnemy = 0f;
//    public float previewBackOffset = 0f;

//    private GameObject dashPreviewInstance;

//    [Header("벽 레이어 마스크")]
//    public LayerMask wallLayerMask;

//    [Header("대시 후 정지 관련")]
//    private bool isCooldownStopped = false;
//    private float cooldownStopTimer = 0f;
//    private float currentCooldownStopDuration = 1f; // 기본 1초
//    public float baseCooldownStopDuration = 1f;     // 기본 정지 시간
//    public float wallHitExtraDuration = 2f;         // 벽 충돌 시 추가 시간

//    void Start()
//    {
//        spriter = GetComponent<SpriteRenderer>();
//        enemyAnimation = GetComponent<EnemyAnimation>();
//        agent = GetComponent<NavMeshAgent>();

//        originalSpeed = GameManager.Instance.dashEnemyStats.speed;
//        speed = originalSpeed;

//        agent.updateRotation = false;
//        agent.updateUpAxis = false;
//        agent.speed = speed;

//        if (dashPreviewPrefab != null)
//        {
//            dashPreviewInstance = Instantiate(dashPreviewPrefab, transform.position, Quaternion.identity);
//            dashPreviewInstance.SetActive(false);
//        }
//    }

//    void Update()
//    {
//        if (!isLive) return;

//        // 쿨다운 정지 상태 처리
//        if (isCooldownStopped)
//        {
//            cooldownStopTimer += Time.deltaTime;
//            enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);
//            if (dashPreviewInstance != null) dashPreviewInstance.SetActive(false);

//            if (cooldownStopTimer >= currentCooldownStopDuration)
//            {
//                isCooldownStopped = false;
//                cooldownStopTimer = 0f;
//                dashTimer = 0f;
//                agent.enabled = true; // 다시 이동 가능
//            }
//            return;
//        }

//        GameObject player = GameObject.FindWithTag("Player");
//        if (player == null) return;

//        Vector2 toPlayer = (player.transform.position - transform.position).normalized;

//        if (isDashing)
//        {
//            DashMove();
//            dashTimeElapsed += Time.deltaTime;

//            enemyAnimation.PlayAnimation(EnemyAnimation.State.Move);
//            FlipSprite(dashDirection.x);

//            if (dashTimeElapsed >= dashDuration)
//            {
//                EndDash(false); // 일반 대시 종료
//            }
//            return;
//        }

//        if (isPreparingToDash)
//        {
//            pauseTimer += Time.deltaTime;
//            enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);

//            if (dashPreviewInstance != null)
//            {
//                dashPreviewInstance.SetActive(true);
//                Vector3 dir = new Vector3(dashDirection.x, dashDirection.y, 0f).normalized;
//                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
//                dashPreviewInstance.transform.rotation = Quaternion.Euler(0f, 0f, angle);

//                Vector3 basePos = transform.position + dir * previewDistanceFromEnemy;
//                Vector3 offset = -dashPreviewInstance.transform.up * previewBackOffset;
//                dashPreviewInstance.transform.position = basePos + offset;
//            }

//            if (pauseTimer >= pauseBeforeDash)
//            {
//                isPreparingToDash = false;
//                isDashing = true;
//                pauseTimer = 0f;

//                if (dashPreviewInstance != null)
//                    dashPreviewInstance.SetActive(false);

//                agent.enabled = false; // NavMeshAgent 비활성화
//            }
//            return;
//        }

//        // 일반 NavMesh 이동
//        if (agent.enabled)
//        {
//            agent.SetDestination(player.transform.position);

//            Vector2 dir = agent.velocity;
//            if (dir.magnitude > 0.1f)
//            {
//                enemyAnimation.PlayAnimation(EnemyAnimation.State.Move);
//                FlipSprite(dir.x);
//            }
//            else
//            {
//                enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);
//            }
//        }

//        // 대시 타이머 증가
//        dashTimer += Time.deltaTime;
//        if (dashTimer >= dashCooldown)
//        {
//            isPreparingToDash = true;
//            pauseTimer = 0f;
//            dashDirection = toPlayer;
//            return;
//        }
//    }

//    private void DashMove()
//    {
//        Vector2 moveVec = dashDirection * dashSpeed * Time.deltaTime;
//        RaycastHit2D hit = Physics2D.Raycast(transform.position, dashDirection, moveVec.magnitude, wallLayerMask);

//        if (hit.collider != null) // 벽 충돌
//        {
//            transform.position = hit.point - dashDirection.normalized * 0.01f;
//            EndDash(true); // 벽에 부딪힌 종료 → 추가 정지 시간
//        }
//        else
//        {
//            transform.Translate(moveVec);
//        }
//    }

//    /// <param name="hitWall">true면 벽 충돌, false면 일반 종료</param>
//    private void EndDash(bool hitWall)
//    {
//        isDashing = false;
//        dashTimeElapsed = 0f;

//        // 벽 충돌이면 추가 시간 적용
//        currentCooldownStopDuration = baseCooldownStopDuration + (hitWall ? wallHitExtraDuration : 0f);

//        isCooldownStopped = true;
//        cooldownStopTimer = 0f;
//    }

//    private void OnTriggerEnter2D(Collider2D collision)
//    {
//        if (collision.CompareTag("Player"))
//        {
//            if (GameManager.Instance.joystickDirectionIndicator.IsUsingSkill)
//            {
//                Debug.Log("스킬 사용 중이라 몬스터 데미지 무시");
//                return;
//            }

//            int damage = GameManager.Instance.dashEnemyStats.attack;
//            GameManager.Instance.playerDamaged.TakeDamage(damage);
//        }
//    }

//    public void Knockback(Vector2 force)
//    {
//        if (isPreparingToDash)
//        {
//            Debug.Log("대시 준비 중이라 넉백 무시");
//            return;
//        }

//        transform.position += (Vector3)force;
//    }

//    private void FlipSprite(float directionX)
//    {
//        Vector3 scale = transform.localScale;
//        scale.x = Mathf.Abs(scale.x) * (directionX < 0 ? -1 : 1);
//        transform.localScale = scale;
//    }

//    void OnDisable()
//    {
//        if (dashPreviewInstance != null)
//            dashPreviewInstance.SetActive(false);
//    }

//    void OnDestroy()
//    {
//        if (dashPreviewInstance != null)
//            Destroy(dashPreviewInstance);
//    }
//}
