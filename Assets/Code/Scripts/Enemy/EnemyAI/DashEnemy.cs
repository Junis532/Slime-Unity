using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody2D))]
public class DashEnemy : EnemyBase
{
    private bool isLive = true;
    private SpriteRenderer spriter;
    private EnemyAnimation enemyAnimation;
    private GameObject player;
    private NavMeshAgent navMesh;
    private Rigidbody2D rb;

    [Header("대시 관련")]
    public float dashSpeed = 25f;
    public float dashDuration = 0.25f;
    public float waitAfterDash = 1.0f;

    private bool isDashing = false;

    [Header("잔상 관련")]
    public GameObject afterImagePrefab;
    public float afterImageSpawnInterval = 0.05f;
    public float afterImageFadeDuration = 0.3f;
    public float afterImageLifeTime = 0.5f;
    public int maxAfterImageCount = 10;

    private Coroutine afterImageCoroutine;
    private readonly List<GameObject> afterImages = new();

    [Header("각도 이동 설정")]
    public bool AIEnabled = true;
    public bool useAngleMove = false;
    public float moveAngle = 0f;
    private Vector2 moveDirection;
    public float angleMoveSpeed = 5f;

    [Header("데미지 설정")]
    public int attackDamage = 50;
    public bool useGameManagerDamage = false;

    [Header("대시 경고")]
    public GameObject warningPrefab;
    public float warningDuration = 0.5f;

    [Header("충돌 레이어")]
    public LayerMask obstacleLayer;
    public LayerMask playerLayer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // Enemy끼리 충돌 무시
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        Physics2D.IgnoreLayerCollision(enemyLayer, enemyLayer, true);
    }

    private void Start()
    {
        navMesh = GetComponent<NavMeshAgent>();
        spriter = GetComponent<SpriteRenderer>();
        enemyAnimation = GetComponent<EnemyAnimation>();
        player = GameObject.FindWithTag("Player");

        navMesh.updateRotation = false;
        navMesh.updateUpAxis = false;
        navMesh.speed = speed;

        if (useAngleMove)
            SetMoveDirection();

        if (player != null)
            StartCoroutine(DashLoop());
    }

    private void Update()
    {
        if (!isLive || !AIEnabled) return;

        if (!CanMove)
        {
            if (navMesh.hasPath)
                navMesh.ResetPath();
            enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);
            return;
        }

        if (useAngleMove && !isDashing)
            AngleMove();
    }

    private void SetMoveDirection()
    {
        float rad = moveAngle * Mathf.Deg2Rad;
        moveDirection = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
    }

    private void AngleMove()
    {
        Vector3 nextPos = transform.position + (Vector3)moveDirection * angleMoveSpeed * Time.deltaTime;
        transform.position = nextPos;

        if (moveDirection.x != 0)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (moveDirection.x < 0 ? -1 : 1);
            transform.localScale = scale;
        }

        enemyAnimation.PlayAnimation(EnemyAnimation.State.Move);
    }
    private IEnumerator DashLoop()
    {
        while (isLive)
        {
            yield return new WaitForSeconds(waitAfterDash);

            if (!CanMove || !AIEnabled || player == null)
            {
                enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);
                continue;
            }

            if (warningPrefab != null)
                yield return StartCoroutine(ShowWarning());

            StartDash();

            // NavMesh 경로 계산
            navMesh.SetDestination(player.transform.position);
            NavMeshPath path = new NavMeshPath();
            navMesh.CalculatePath(player.transform.position, path);

            float elapsed = 0f;
            int cornerIndex = 0;

            while (elapsed < dashDuration && cornerIndex < path.corners.Length - 1)
            {
                if (!CanMove) break;

                Vector3 start = path.corners[cornerIndex];
                Vector3 end = path.corners[cornerIndex + 1];
                Vector3 dir = (end - start).normalized;

                // 각 코너까지 이동
                transform.position += dir * dashSpeed * Time.deltaTime;

                // 코너 도착 시 다음 코너로
                if (Vector3.Distance(transform.position, end) < 0.1f)
                    cornerIndex++;

                if (dir.x != 0)
                    FlipSprite(dir.x);

                elapsed += Time.deltaTime;
                yield return null;
            }

            EndDash();
        }
    }

    private IEnumerator ShowWarning()
    {
        GameObject warning = Instantiate(warningPrefab, transform.position, Quaternion.identity);
        warning.transform.SetParent(transform);

        SpriteRenderer sr = warning.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0);
            sr.DOFade(0.5f, 0.2f).SetLoops(-1, LoopType.Yoyo);
        }

        float elapsed = 0f;
        while (elapsed < warningDuration)
        {
            if (player != null)
            {
                Vector3 dir = (player.transform.position - transform.position).normalized;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                warning.transform.rotation = Quaternion.Euler(0, 0, angle);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(warning);
    }

    private void StartDash()
    {
        isDashing = true;
        enemyAnimation.PlayAnimation(EnemyAnimation.State.Move);

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        int myLayer = gameObject.layer;
        Physics2D.IgnoreLayerCollision(myLayer, enemyLayer, true);

        if (afterImageCoroutine != null)
            StopCoroutine(afterImageCoroutine);
        afterImageCoroutine = StartCoroutine(SpawnAfterImages());
    }

    private void EndDash()
    {
        isDashing = false;

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        int myLayer = gameObject.layer;
        Physics2D.IgnoreLayerCollision(myLayer, enemyLayer, false);

        if (afterImageCoroutine != null)
        {
            StopCoroutine(afterImageCoroutine);
            afterImageCoroutine = null;
        }

        enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);
    }

    private void FlipSprite(float dirX)
    {
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (dirX < 0 ? -1 : 1);
        transform.localScale = scale;
    }

    private IEnumerator SpawnAfterImages()
    {
        while (isDashing)
        {
            CreateAfterImage();
            yield return new WaitForSeconds(afterImageSpawnInterval);
        }
    }

    private void CreateAfterImage()
    {
        GameObject afterImage = new GameObject("AfterImage");
        afterImage.transform.position = transform.position;
        afterImage.transform.rotation = transform.rotation;
        afterImage.transform.localScale = transform.localScale;

        SpriteRenderer sr = afterImage.AddComponent<SpriteRenderer>();
        SpriteRenderer enemySR = GetComponent<SpriteRenderer>();

        if (enemySR != null)
        {
            sr.sprite = enemySR.sprite;
            sr.flipX = enemySR.flipX;
            Color c = enemySR.color;
            c.a = 0.5f;
            sr.color = c;

            sr.sortingLayerID = enemySR.sortingLayerID;
            sr.sortingOrder = enemySR.sortingOrder - 1;
        }

        afterImages.Add(afterImage);
        if (afterImages.Count > maxAfterImageCount)
        {
            Destroy(afterImages[0]);
            afterImages.RemoveAt(0);
        }

        sr.DOFade(0f, afterImageFadeDuration)
          .SetDelay(afterImageLifeTime - afterImageFadeDuration)
          .OnComplete(() =>
          {
              afterImages.Remove(afterImage);
              Destroy(afterImage);
          });
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isLive) return;

        // 플레이어 충돌
        if (((1 << collision.gameObject.layer) & playerLayer) != 0)
        {
            enemyAnimation.PlayAnimation(EnemyAnimation.State.Attack);

            if (GameManager.Instance.joystickDirectionIndicator != null &&
                GameManager.Instance.joystickDirectionIndicator.IsUsingSkill)
                return;

            int damage = useGameManagerDamage ? GameManager.Instance.enemyStats.attack : attackDamage;
            Vector3 enemyPosition = transform.position;
            GameManager.Instance.playerDamaged.TakeDamage(damage, enemyPosition);
        }

        // 장애물 충돌
        if (useAngleMove && ((1 << collision.gameObject.layer) & obstacleLayer) != 0)
        {
            moveDirection = -moveDirection;
        }
    }
}