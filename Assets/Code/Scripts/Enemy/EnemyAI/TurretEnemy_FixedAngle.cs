using DG.Tweening;
using UnityEngine;
using System.Collections;

public class TurretEnemy_FixedAngle : EnemyBase
{
    private bool isLive = true;
    private SpriteRenderer spriter;
    private EnemyAnimation enemyAnimation;

    [Header("발사 범위 / 라인 표시")]
    public float fireRange = 5f;

    [Header("첫 발사 딜레이")]
    public float firstFireDelay = 0f; // 모두 동일 추천 (메트로놈 맞추기 쉽다)

    [Header("프리-와인드(발사 전 예열 연출)")]
    [Tooltip("발사 직전부터 색상/효과로 예열하는 시간(초)")]
    public float preWindUp = 0.15f;

    [Header("탄환 설정")]
    public GameObject bulletPrefab;
    public float bulletSpeed = 1.5f;
    public float bulletLifetime = 3f;

    [Header("LineRenderer 설정")]
    public bool showLineRenderer = true;
    private LineRenderer lineRenderer;

    [Header("고정 발사 각도 (도 단위)")]
    [Range(0f, 360f)]
    public float fixedAngle = 0f;

    [Header("Phase 스케줄(사이클 내 발사 시각들, 초 단위)")]
    public float cycleLength = 1.2f;     // 전체 사이클 길이 (예: 0.2 간격 × 6샷 = 1.2)
    public float[] firePhases = { 0f };  // 각 터렛마다 다르게 세팅

    // 내부 상태
    private int phaseIdx = 0;
    private double cycleBase;            // 현재 사이클 시작 절대 시각 (Time.timeAsDouble)
    private double nextFireAt;           // 다음 발사 절대 시각
    private bool isPrepping = false;

    void Start()
    {
        spriter = GetComponent<SpriteRenderer>();
        enemyAnimation = GetComponent<EnemyAnimation>();

        // (선택) 네 프로젝트에 이런 참조가 있다면 유지
        // originalSpeed = GameManager.Instance.longRangeEnemyStats.speed;
        // speed = originalSpeed;

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

        // phase 정렬(보호)
        if (firePhases == null || firePhases.Length == 0)
            firePhases = new float[] { 0f };
        System.Array.Sort(firePhases);

        // 절대 시각 스케줄 시작
        cycleBase = Time.timeAsDouble + firstFireDelay;
        phaseIdx = 0;
        nextFireAt = cycleBase + firePhases[phaseIdx];

        StartCoroutine(PhaseScheduleLoop());
    }

    void Update()
    {
        if (!isLive) return;

        // 라인 표시
        float rad = fixedAngle * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;

        if (lineRenderer != null)
            lineRenderer.enabled = showLineRenderer;

        if (showLineRenderer && lineRenderer.enabled)
        {
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, (Vector2)transform.position + dir * fireRange);
        }

        // (선택) 아이들 애니
        if (enemyAnimation != null)
            enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);
    }

    private IEnumerator PhaseScheduleLoop()
    {
        while (isLive)
        {
            // 이번 샷의 방향(고정 각도)
            float rad = fixedAngle * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;

            // 1) 프리-와인드 시작 시각까지 대기
            float prep = Mathf.Max(0f, preWindUp);
            double prepStart = nextFireAt - prep;
            double now = Time.timeAsDouble;
            if (prepStart > now)
                yield return new WaitForSeconds((float)(prepStart - now));

            // 2) 프리-와인드 연출
            isPrepping = true;
            if (spriter != null && prep > 0f)
            {
                spriter.DOKill();
                spriter.color = Color.white;
                spriter.DOColor(Color.red, prep).SetEase(Ease.Linear);
            }

            // 3) 정확히 발사 시각까지 대기
            now = Time.timeAsDouble;
            if (nextFireAt > now)
                yield return new WaitForSeconds((float)(nextFireAt - now));

            // 4) 발사
            Shoot(dir);

            // 5) 원복
            if (spriter != null)
            {
                spriter.DOKill();
                spriter.DOColor(Color.white, 0.1f);
            }
            isPrepping = false;

            // 6) 다음 phase로 이동 (절대 시각 스케줄: 드리프트 없음)
            phaseIdx++;
            if (phaseIdx >= firePhases.Length)
            {
                phaseIdx = 0;
                cycleBase += cycleLength; // 다음 사이클 시작
            }
            nextFireAt = cycleBase + firePhases[phaseIdx];

            yield return null;
        }
    }

    void Shoot(Vector2 dir)
    {
        GameObject bullet = PoolManager.Instance.SpawnFromPool(
            bulletPrefab != null ? bulletPrefab.name : "Bullet",
            transform.position,
            Quaternion.identity
        );

        if (bullet != null)
        {
            BulletBehavior bulletBehavior = bullet.GetComponent<BulletBehavior>();
            if (bulletBehavior == null)
                bulletBehavior = bullet.AddComponent<BulletBehavior>();

            bulletBehavior.Initialize(dir.normalized, bulletSpeed, bulletLifetime);
        }
    }

    // 외부에서 사이클을 리셋하고 싶을 때 호출 가능 (선택)
    public void ResetCycle(double delay = 0.0)
    {
        cycleBase = Time.timeAsDouble + delay;
        phaseIdx = 0;
        nextFireAt = cycleBase + firePhases[phaseIdx];
    }

    private void OnDestroy()
    {
        if (lineRenderer != null)
            Destroy(lineRenderer);
        isLive = false;
    }
}
