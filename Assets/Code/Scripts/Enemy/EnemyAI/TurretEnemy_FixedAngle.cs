using DG.Tweening;
using UnityEngine;

public class TurretEnemy_FixedAngle : EnemyBase
{
    private bool isLive = true;
    private SpriteRenderer spriter;
    private EnemyAnimation enemyAnimation;

    [Header("발사 범위 / 라인 표시")]
    public float fireRange = 5f;

    [Header("발사 간격 (순환)")]
    public float[] fireIntervals = { 1f, 3f, 2f }; // 각 탄마다 발사 대기 시간
    private int fireIndex = 0;
    private float lastFireTime;

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

    // ==== 사이클 쿨다운 추가 ====
    [Header("사이클 쿨다운(리스트 한 바퀴 후 쉬는 시간)")]
    public float cycleCooldown = 2f;                  // NEW: 한 사이클 후 적용할 쿨다운 시간
    private bool cycleCooldownPending = false;        // NEW: 다음 발사 전 쿨다운을 '한 번만' 적용

    void Start()
    {
        spriter = GetComponent<SpriteRenderer>();
        enemyAnimation = GetComponent<EnemyAnimation>();

        originalSpeed = GameManager.Instance.longRangeEnemyStats.speed;
        speed = originalSpeed;

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

        // 시작 시 첫 발사 시간 = 현재 시간 + firstFireDelay
        lastFireTime = Time.time - fireIntervals[0] + firstFireDelay;
    }

    void Update()
    {
        if (!isLive) return;

        // -------------------------------
        // 매 프레임 Crystal 레이어 존재 여부 체크
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

        // 존재하면 Enemy 태그 제거, 없으면 Enemy 태그 설정
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

        // 기존 고정 각도 발사/라인 렌더링 로직
        float rad = fixedAngle * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;

        if (lineRenderer != null)
            lineRenderer.enabled = showLineRenderer;

        if (showLineRenderer && lineRenderer.enabled)
        {
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, (Vector2)transform.position + dir * fireRange);
        }

        float currentCooldown = fireIntervals[fireIndex % fireIntervals.Length];
        float effectiveCooldown = cycleCooldownPending ? cycleCooldown : currentCooldown;

        if (Time.time - lastFireTime >= effectiveCooldown && !isPreparingToFire)
        {
            if (cycleCooldownPending) cycleCooldownPending = false;
            StartCoroutine(PrepareAndShoot(dir));
        }

        enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);
    }

    private System.Collections.IEnumerator PrepareAndShoot(Vector2 dir)
    {
        isPreparingToFire = true;

        float duration = 1f; // 발사 준비 시간

        // 본체 색이 흰색 → 빨강으로 변함
        if (spriter != null)
        {
            spriter.DOColor(Color.red, duration);
        }

        // 발사 준비 대기
        yield return new WaitForSeconds(duration);

        // 발사
        Shoot(dir);
        lastFireTime = Time.time;

        // 다음 쿨다운으로 이동
        int prevIndex = fireIndex;
        fireIndex = (fireIndex + 1) % fireIntervals.Length;

        // 한 사이클 끝냈으면 cycleCooldown 예약
        if (prevIndex == fireIntervals.Length - 1)
        {
            cycleCooldownPending = true;
        }

        // 발사 후 본체 색 다시 흰색으로 복귀
        if (spriter != null)
        {
            spriter.DOColor(Color.white, 0.2f);
        }

        // 발사 후 잠시 대기
        yield return new WaitForSeconds(0.3f);

        isPreparingToFire = false;
    }

    void Shoot(Vector2 dir)
    {
        GameObject bullet = PoolManager.Instance.SpawnFromPool(bulletPrefab.name, transform.position, Quaternion.identity);

        if (bullet != null)
        {
            BulletBehavior bulletBehavior = bullet.GetComponent<BulletBehavior>();
            if (bulletBehavior == null)
                bulletBehavior = bullet.AddComponent<BulletBehavior>();

            bulletBehavior.Initialize(dir.normalized, bulletSpeed, bulletLifetime);
        }
    }

    // 외부에서 강제로 사이클 쿨다운을 바로 다음 발사 전에 적용하고 싶을 때 호출
    public void ForceCycleCooldown()
    {
        cycleCooldownPending = true;
    }

    private void OnDestroy()
    {
        if (lineRenderer != null)
            Destroy(lineRenderer);

        isLive = false;
    }
}
