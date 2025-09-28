using DG.Tweening;
using UnityEngine;
using System.Collections; // Coroutine을 위해 명시적으로 추가

public class TurretEnemy_PlayerTracking : EnemyBase
{
    private bool isLive = true;
    private SpriteRenderer spriter;
    private EnemyAnimation enemyAnimation;

    [Header("공격 애니메이션 준비 시간")]
    [Tooltip("AttackStart 애니메이션이 시작되어 발사까지 걸리는 시간")]
    public float attackPrepareDuration = 0.5f; // 기본값 설정

    [Header("발사 쿨다운 설정 (순환)")]
    public float[] fireIntervals = { 1f, 3f, 2f };
    private int fireIndex = 0;
    private float lastFireTime;

    [Header("첫 발사 딜레이")]
    public float firstFireDelay = 2f;

    [Header("탄환 설정")]
    public GameObject bulletPrefab;
    public float bulletSpeed = 1.5f;
    public float bulletLifetime = 3f;

    [Header("LineRenderer 설정")]
    public bool showLineRenderer = true;
    private LineRenderer lineRenderer;
    private bool isPreparingToFire = false;

    // *주의: EnemyBase, GameManager 관련 필드/코드는 주석 상태를 유지합니다.

    void Start()
    {
        spriter = GetComponent<SpriteRenderer>();
        enemyAnimation = GetComponent<EnemyAnimation>();

        // LineRenderer 세팅
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.enabled = showLineRenderer;

        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.red;
        lineRenderer.endColor = Color.red;

        lineRenderer.sortingOrder = 2;
        lineRenderer.sortingLayerName = "Default";

        // 첫 발사 딜레이 적용
        lastFireTime = Time.time - fireIntervals[0] + firstFireDelay;

        // 초기 애니메이션 상태 설정
        if (enemyAnimation != null)
        {
            enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);
        }
    }

    void Update()
    {
        if (!isLive) return;

        // -------------------------------
        // Crystal 레이어 존재 여부 체크 (기존 로직 유지)
        int crystalLayer = LayerMask.NameToLayer("Crystal");
        bool crystalExists = false;

        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
        {
            if (obj.layer == crystalLayer)
            {
                crystalExists = true;
                break;
            }
        }

        if (crystalExists)
        {
            if (gameObject.tag == "Enemy")
                gameObject.tag = "Untagged";
        }
        else
        {
            if (gameObject.tag != "Enemy")
                gameObject.tag = "Enemy";
        }
        // -------------------------------

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            if (lineRenderer != null)
                lineRenderer.enabled = false;
            if (enemyAnimation != null && !isPreparingToFire)
            {
                enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);
            }
            return;
        }

        if (lineRenderer != null)
            lineRenderer.enabled = showLineRenderer;

        Vector2 toPlayer = player.transform.position - transform.position;
        Vector2 dir = toPlayer.normalized;

        // 좌우 반전
        if (Mathf.Abs(toPlayer.x) > 0.01f)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (toPlayer.x < 0 ? -1 : 1);
            transform.localScale = scale;
        }

        if (showLineRenderer && lineRenderer.enabled)
        {
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, player.transform.position);
        }

        float currentCooldown = fireIntervals[fireIndex % fireIntervals.Length];

        // 발사 쿨다운 체크
        if (Time.time - lastFireTime >= currentCooldown && !isPreparingToFire)
        {
            StartCoroutine(PrepareAndShoot(dir));
        }

        // 발사 준비 중이 아닐 때만 플레이어 방향을 바라보도록 애니메이션 갱신
        if (!isPreparingToFire && enemyAnimation != null)
        {
            // 이 함수가 방향에 맞는 MoveSide, MoveFront, MoveBack 애니메이션을 재생합니다.
            enemyAnimation.PlayDirectionalMoveAnimation(dir);
        }
    }


    private System.Collections.IEnumerator PrepareAndShoot(Vector2 dir)
    {
        isPreparingToFire = true;

        float totalInterval = fireIntervals[fireIndex % fireIntervals.Length];
        // 순수한 대기 시간 = 전체 쿨다운 - 애니메이션 준비 시간
        float waitDuration = Mathf.Max(0f, totalInterval - attackPrepareDuration);

        // 1. 순수한 대기 시간을 기다림
        if (waitDuration > 0)
        {
            yield return new WaitForSeconds(waitDuration);
        }

        // 2. AttackStart 애니메이션 시작 (준비 단계)
        if (enemyAnimation != null)
        {
            enemyAnimation.PlayAnimation(EnemyAnimation.State.Attack);
        }

        // 발사 준비: 본체 색이 하얀색 → 빨강으로 변화
        if (spriter != null)
        {
            spriter.DOKill();
            spriter.DOColor(Color.red, attackPrepareDuration);
        }

        // 🎯 AttackStart 재생 시간만큼 대기
        yield return new WaitForSeconds(attackPrepareDuration);

        // 3. 발사
        Shoot(dir);

        // 발사 후 본체 색 다시 하얀색으로
        if (spriter != null)
        {
            spriter.DOKill();
            spriter.DOColor(Color.white, 0.2f); // 0.2초 동안 서서히 복귀
        }

        // 🚨 수정: 발사 직후 강제로 Idle 상태를 지정하여 애니메이션 상태를 리셋합니다.
        if (enemyAnimation != null)
        {
            enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);
        }

        // 4. 다음 사이클 설정 및 Idle 상태로 즉시 복귀
        lastFireTime = Time.time;
        fireIndex = (fireIndex + 1) % fireIntervals.Length;

        // isPreparingToFire가 false가 되면, 다음 Update 프레임에서
        // enemyAnimation.PlayDirectionalMoveAnimation(dir)이 즉시 호출되어 
        // Move 계열 스프라이트로 전환됩니다.
        isPreparingToFire = false;
    }


    void Shoot(Vector2 dir)
    {
        // *주의: PoolManager.Instance.SpawnFromPool이 프로젝트에 정의되어 있어야 합니다.*
        GameObject bullet = PoolManager.Instance.SpawnFromPool(bulletPrefab.name, transform.position, Quaternion.identity);

        if (bullet != null)
        {
            BulletBehavior bulletBehavior = bullet.GetComponent<BulletBehavior>();
            if (bulletBehavior == null)
                bulletBehavior = bullet.AddComponent<BulletBehavior>();

            bulletBehavior.Initialize(dir.normalized, bulletSpeed, bulletLifetime);
        }
    }

    private void OnDestroy()
    {
        if (lineRenderer != null)
            Destroy(lineRenderer);

        if (spriter != null)
        {
            spriter.DOKill();
        }

        isLive = false;
    }
}