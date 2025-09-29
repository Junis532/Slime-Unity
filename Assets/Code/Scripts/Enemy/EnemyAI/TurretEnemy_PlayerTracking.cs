using DG.Tweening;
using UnityEngine;
using System.Collections; // Coroutine을 위해 명시적으로 추가

// EnemyBase를 상속받는다고 가정 (주석 처리된 원본 유지)
// public class TurretEnemy_PlayerTracking : EnemyBase
public class TurretEnemy_PlayerTracking : MonoBehaviour
{
    private bool isLive = true;
    private SpriteRenderer spriter;
    // TurretEnemyAnimation 컴포넌트를 사용합니다.
    private TurretEnemyAnimation enemyAnimation;
    private Coroutine attackRoutine; // 중복 발사 방지를 위한 코루틴 참조

    [Header("공격 애니메이션 준비 시간")]
    [Tooltip("ShootPrepare 애니메이션 시작부터 발사까지 걸리는 시간")]
    // 이 값은 더 이상 사용되지 않지만, 필드는 유지합니다.
    public float attackPrepareDuration = 0.5f;

    [Header("발사 쿨다운 설정 (순환)")]
    // 이 값이 '발사 간의 총 시간'이자 '준비 시간'으로 사용됩니다.
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
        enemyAnimation = GetComponent<TurretEnemyAnimation>();
        if (!enemyAnimation) Debug.LogError("TurretEnemyAnimation 컴포넌트를 지정하세요.");

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
        if (fireIntervals.Length > 0)
        {
            lastFireTime = Time.time - fireIntervals[0] + firstFireDelay;
        }

        // 초기 애니메이션 상태 설정
        if (enemyAnimation != null)
        {
            enemyAnimation.PlayAnimation(TurretEnemyAnimation.State.Idle);
        }
    }

    void Update()
    {
        if (!isLive) return;

        // -------------------------------
        // Crystal 레이어 존재 여부 체크 (기존 로직 유지)
        int crystalLayer = LayerMask.NameToLayer("Crystal");
        bool crystalExists = false;

        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
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
            if (enemyAnimation != null)
            {
                enemyAnimation.PlayAnimation(TurretEnemyAnimation.State.Idle);
            }
            return;
        }

        Vector2 toPlayer = player.transform.position - transform.position;
        Vector2 dir = toPlayer.normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg; // 각도 계산

        // 발사 준비 중이든 아니든 항상 플레이어 방향으로 좌우 반전을 갱신합니다.
        if (Mathf.Abs(toPlayer.x) > 0.01f)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (toPlayer.x < 0 ? -1 : 1);
            transform.localScale = scale;
        }

        // 발사 준비 중이 아닐 때만 Idle 애니메이션을 재생하며 추적합니다.
        if (!isPreparingToFire && enemyAnimation != null)
        {
            enemyAnimation.PlayAnimation(TurretEnemyAnimation.State.Idle, angle);
        }


        if (showLineRenderer && lineRenderer != null)
        {
            lineRenderer.enabled = showLineRenderer;
            lineRenderer.SetPosition(0, transform.position);
            // 라인 렌더러는 항상 현재 플레이어 위치를 추적합니다.
            lineRenderer.SetPosition(1, player.transform.position);
        }
        else if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }


        if (fireIntervals.Length == 0) return;
        // fireIntervals는 총 쿨다운 시간으로 사용됩니다.
        float currentCooldown = fireIntervals[fireIndex % fireIntervals.Length];

        // 발사 쿨다운 체크
        if (Time.time - lastFireTime >= currentCooldown && !isPreparingToFire)
        {
            if (attackRoutine != null) StopCoroutine(attackRoutine);
            // PrepareAndShoot 호출 시 dir과 angle은 사용되지 않으므로, 기본값을 넘겨줍니다.
            attackRoutine = StartCoroutine(PrepareAndShoot());
        }
    }


    // dir, angle 인수를 제거하고 코루틴 내부에서 발사 직전에 현재 위치를 계산하도록 수정
    private System.Collections.IEnumerator PrepareAndShoot()
    {
        isPreparingToFire = true;

        // prepTime을 fireIntervals의 현재 값으로 설정합니다.
        float totalPrepTime = fireIntervals[fireIndex % fireIntervals.Length];

        // 1. 발사 준비 애니메이션 시작 및 색상 변화 설정

        // 🎯 PrepareState를 얻기 위해 현재 플레이어 방향을 한 번 계산합니다.
        GameObject player = GameObject.FindWithTag("Player");
        Vector2 initialDir = Vector2.right; // 기본값
        float initialAngle = 0; // 기본값

        if (player != null)
        {
            Vector2 toPlayer = player.transform.position - transform.position;
            initialDir = toPlayer.normalized;
            initialAngle = Mathf.Atan2(initialDir.y, initialDir.x) * Mathf.Rad2Deg;
        }

        TurretEnemyAnimation.State prepareState = GetPrepareState(initialAngle);

        if (enemyAnimation != null)
        {
            enemyAnimation.PlayAnimation(prepareState);
        }

        // 본체 색이 하얀색 → 빨강으로 변화 (totalPrepTime 동안)
        if (spriter != null)
        {
            spriter.DOKill();
            spriter.DOColor(Color.red, totalPrepTime);
        }

        // 2. 공격 준비 시간 대기 (지연 시간)
        if (totalPrepTime > 0)
        {
            yield return new WaitForSeconds(totalPrepTime);
        }

        // 3. 발사 직전, 플레이어의 최종 위치를 다시 계산하여 발사 방향을 갱신합니다.
        player = GameObject.FindWithTag("Player");
        Vector2 finalDir = Vector2.right; // 기본값

        if (player != null)
        {
            Vector2 toPlayer = player.transform.position - transform.position;
            finalDir = toPlayer.normalized;

            // finalAngle은 애니메이션 복귀에 필요하지 않으므로 생략 가능
        }

        // 🎯 최종 계산된 방향으로 발사
        Shoot(finalDir);

        // 4. 발사 후 본체 색 다시 하얀색으로 빠르게 복구
        if (spriter != null)
        {
            spriter.DOKill();
            spriter.DOColor(Color.white, 0.1f);
        }

        // 5. 즉시 Idle 상태로 복귀
        if (enemyAnimation != null)
        {
            enemyAnimation.PlayAnimation(TurretEnemyAnimation.State.Idle);
        }

        // 6. 다음 사이클 설정
        lastFireTime = Time.time;
        fireIndex = (fireIndex + 1) % fireIntervals.Length;

        // 준비 플래그 해제.
        isPreparingToFire = false;
        attackRoutine = null;
    }

    // ================= 애니메이션 헬퍼 함수 =================

    /// <summary> 각도에 따라 알맞은 ShootPrepare 상태를 반환합니다. (90도/270도 부근은 Front) </summary>
    private TurretEnemyAnimation.State GetPrepareState(float angle)
    {
        // 각도를 0~360 범위로 정규화
        angle = (angle % 360 + 360) % 360;
        float verticalTolerance = 25f; // 정면/후면 애니메이션을 사용할 각도 범위

        // 90도(위) 부근 또는 270도(아래) 부근
        if ((angle >= 90f - verticalTolerance && angle <= 90f + verticalTolerance) ||
            (angle >= 270f - verticalTolerance && angle <= 270f + verticalTolerance))
        {
            return TurretEnemyAnimation.State.FrontShootPrepare;
        }
        // 측면
        return TurretEnemyAnimation.State.ShootPrepare;
    }

    /// <summary> ShootPost를 사용하지 않으므로, 이 함수는 Idle을 반환합니다. </summary>
    private TurretEnemyAnimation.State GetPostState(float angle)
    {
        return TurretEnemyAnimation.State.Idle;
    }

    // ================= 발사 로직 =================

    void Shoot(Vector2 dir)
    {
        if (!bulletPrefab) return;
        GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();

        // Rigidbody2D.linearVelocity는 Unity 2021+에서 Rigidbody2D.velocity로 대체됨
        if (rb) rb.linearVelocity = dir.normalized * bulletSpeed;
        Destroy(bullet, bulletLifetime);
    }

    private void OnDestroy()
    {
        if (lineRenderer != null)
            Destroy(lineRenderer);

        if (spriter != null)
        {
            spriter.DOKill();
        }

        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
        }

        isLive = false;
    }
}