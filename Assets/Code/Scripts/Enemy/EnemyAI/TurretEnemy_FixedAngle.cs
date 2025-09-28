using DG.Tweening;
using UnityEngine;
using System.Collections; // Coroutine을 위해 명시적으로 추가

public class TurretEnemy_FixedAngle : EnemyBase
{
    private bool isLive = true;
    private SpriteRenderer spriter;
    private EnemyAnimation enemyAnimation;

    [Header("공격 애니메이션 준비 시간")]
    [Tooltip("AttackStart 애니메이션이 시작되어 발사까지 걸리는 시간")]
    public float attackPrepareDuration = 2.0f;

    [Header("발사 범위 / 라인 표시")]
    public float fireRange = 5f;

    [Header("발사 간격 (순환)")]
    public float[] fireIntervals = { 1f, 3f, 2f };
    private int fireIndex = 0;
    private float lastFireTime; // 다음 발사 대기 시간 계산의 기준

    [Header("첫 발사 딜레이")]
    public float firstFireDelay = 2f; // 첫 발사는 2초 뒤 실행

    [Header("탄환 설정")]
    public GameObject bulletPrefab;
    public float bulletSpeed = 1.5f;
    public float bulletLifetime = 3f;

    [Header("LineRenderer 설정")]
    public bool showLineRenderer = true;
    private LineRenderer lineRenderer;
    private bool isPreparingToFire = false;

    [Header("고정 발사 각도 (도 단위)")]
    [Range(0f, 360f)]
    public float fixedAngle = 0f;

    [Header("사이클 쿨다운(리스트 한 바퀴 후 쉬는 시간)")]
    public float cycleCooldown = 2f; // NEW: 한 사이클 후 적용할 쿨다운 시간
    private bool cycleCooldownPending = false; // NEW: 다음 발사 전 쿨다운을 '한 번만' 적용

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

        // 시작 시 첫 발사 시간 설정
        // Time.time - fireIntervals[0] + firstFireDelay;
        // fireIntervals[0]만큼 미리 경과된 것처럼 설정 후 firstFireDelay만큼 대기 시간을 추가하여 첫 발사 딜레이 구현
        lastFireTime = Time.time - fireIntervals[0] + firstFireDelay;
    }

    void Update()
    {
        if (!isLive) return;

        // --- Crystal 레이어 체크 로직 ---
        // ⚠️ 주의: Object.FindObjectsByType를 Update()에서 매 프레임 호출하는 것은 성능에 매우 안 좋습니다.
        // Crystal의 상태는 별도의 Manager 스크립트에서 관리하고 참조하는 것이 좋습니다.
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
        // ------------------------------------

        // 고정 각도 및 좌우 반전 로직
        float rad = fixedAngle * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;

        // 🟢 수정된 좌우 반전 로직: 방향이 바뀔 때만 스케일 변경
        if (Mathf.Abs(dir.x) > 0.01f)
        {
            float targetSign = dir.x < 0 ? -1 : 1;
            float currentSign = Mathf.Sign(transform.localScale.x);

            // 현재 스케일의 부호(방향)와 목표 스케일의 부호가 다를 때만 스케일 변경
            if (!Mathf.Approximately(targetSign, currentSign))
            {
                var s = transform.localScale;
                s.x = Mathf.Abs(s.x) * targetSign;
                transform.localScale = s;
            }
        }
        // ------------------------------------

        // LineRenderer 로직 유지
        if (lineRenderer != null)
            lineRenderer.enabled = showLineRenderer;

        if (showLineRenderer && lineRenderer.enabled)
        {
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, (Vector2)transform.position + dir * fireRange);
        }

        // 발사 쿨다운 체크
        float currentCooldown = fireIntervals[fireIndex % fireIntervals.Length];
        float effectiveCooldown = cycleCooldownPending ? cycleCooldown : currentCooldown;

        // 다음 발사 시간이 되었고, 현재 발사 준비 중이 아니라면 코루틴 시작
        if (Time.time - lastFireTime >= effectiveCooldown && !isPreparingToFire)
        {
            // cycleCooldownPending이 적용 중이었다면 해제
            if (cycleCooldownPending) cycleCooldownPending = false;

            // 코루틴 시작 (내부에서 lastFireTime 업데이트 및 발사 준비 시작)
            StartCoroutine(PrepareAndShoot(dir));
        }

        // 발사 준비 중이 아닐 때, 그리고 공격 애니메이션(Start/End)이 진행 중이 아닐 때만 Move/Idle로 전환
        if (!isPreparingToFire)
        {
            if (enemyAnimation.currentState != EnemyAnimation.State.AttackStart &&
                enemyAnimation.currentState != EnemyAnimation.State.AttackEnd &&
                enemyAnimation.currentState != EnemyAnimation.State.FrontAttackEnd)
            {
                // 이 함수가 90/270도일 때 MoveBack/MoveFront 상태를 설정합니다.
                enemyAnimation.PlayDirectionalMoveAnimation(dir);
            }
        }
    }

    private System.Collections.IEnumerator PrepareAndShoot(Vector2 dir)
    {
        isPreparingToFire = true;

        float totalInterval = fireIntervals[fireIndex % fireIntervals.Length]; // 다음 발사 간격
        float waitDuration = Mathf.Max(0f, totalInterval - attackPrepareDuration);

        // 1. AttackStart 애니메이션 시작 '전에' 순수한 대기 시간을 기다림. (이 부분이 발사 간격을 맞춥니다)
        if (waitDuration > 0)
        {
            yield return new WaitForSeconds(waitDuration);
        }

        // 🚨 1. AttackStart 애니메이션 시작
        enemyAnimation.PlayAnimation(EnemyAnimation.State.Attack);

        // 본체 색이 흰색 → 빨강으로 변함
        if (spriter != null)
        {
            spriter.DOKill();
            spriter.DOColor(Color.red, attackPrepareDuration);
        }

        // 🎯 AttackStart 재생 및 발사 준비 대기
        yield return new WaitForSeconds(attackPrepareDuration);

        // 🚨 2. 발사 및 AttackEnd로 전환 (발사 시점)
        Shoot(dir);

        // AttackEnd 애니메이션 시작을 PlayAnimation으로 직접 호출
        // 🚨 수정된 로직: fixedAngle이 90도 또는 270도일 때 FrontAttackEnd 사용
        if (Mathf.Approximately(fixedAngle, 90f) || Mathf.Approximately(fixedAngle, 270f))
        {
            enemyAnimation.PlayAnimation(EnemyAnimation.State.FrontAttackEnd);
        }
        else
        {
            enemyAnimation.PlayAnimation(EnemyAnimation.State.AttackEnd);
        }

        // 발사 후 본체 색 다시 흰색으로 복귀
        if (spriter != null)
        {
            spriter.DOKill();
            spriter.DOColor(Color.white, 0.2f);
        }

        // 🚨 AttackEnd/FrontAttackEnd 애니메이션이 끝날 때까지 대기
        yield return new WaitUntil(() =>
            enemyAnimation.currentState != EnemyAnimation.State.AttackEnd &&
            enemyAnimation.currentState != EnemyAnimation.State.FrontAttackEnd
        );

        // 🚨 3. 다음 사이클 로직 실행
        lastFireTime = Time.time;

        // 다음 쿨다운으로 이동 및 사이클 쿨다운 예약 로직 유지
        int prevIndex = fireIndex;
        fireIndex = (fireIndex + 1) % fireIntervals.Length;

        if (prevIndex == fireIntervals.Length - 1)
        {
            cycleCooldownPending = true;
        }

        isPreparingToFire = false;
    }

    void Shoot(Vector2 dir)
    {
        // 탄환 발사 로직 유지
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

    public void ForceCycleCooldown()
    {
        cycleCooldownPending = true;
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