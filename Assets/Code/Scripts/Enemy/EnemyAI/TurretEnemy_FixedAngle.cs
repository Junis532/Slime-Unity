using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

[RequireComponent(typeof(SpriteRenderer))]
public class TurretEnemy_FixedAngle : MonoBehaviour
{
    [Header("🎯 스프라이트 / 애니메이션")]
    public TurretEnemyAnimation turretAnim;

    private bool isLive = true;
    private SpriteRenderer spriter;

    [Header("발사 범위 / 라인 표시")]
    public float fireRange = 5f;

    [Header("첫 발사 딜레이")]
    public float firstFireDelay = 0f;

    [Header("프리-와인드(발사 전 예열 연출)")]
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

    [Header("Phase 스케줄")]
    public float cycleLength = 1.2f;
    public float[] firePhases = { 0f };

    private int phaseIdx = 0;
    private double cycleBase;
    private double nextFireAt;
    private bool isPrepping = false;

    void Awake()
    {
        spriter = GetComponent<SpriteRenderer>();
        if (!turretAnim) turretAnim = GetComponent<TurretEnemyAnimation>();
        if (!turretAnim) Debug.LogError("TurretEnemyAnimation을 지정하세요.");

        // LineRenderer 초기화
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
    }

    void Start()
    {
        if (firePhases == null || firePhases.Length == 0)
            firePhases = new float[] { 0f };
        System.Array.Sort(firePhases);

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

        if (showLineRenderer && lineRenderer != null)
        {
            lineRenderer.enabled = true;
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, (Vector2)transform.position + dir * fireRange);
        }

        // Idle 애니메이션 각도 기반 적용
        if (turretAnim != null)
            turretAnim.PlayAnimation(TurretEnemyAnimation.State.Idle, fixedAngle);
    }

    private IEnumerator PhaseScheduleLoop()
    {
        while (isLive)
        {
            float rad = fixedAngle * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;

            // 1) 프리-와인드 시작 시각까지 대기
            double prepStart = nextFireAt - preWindUp;
            double now = Time.timeAsDouble;
            if (prepStart > now)
                yield return new WaitForSeconds((float)(prepStart - now));

            // 2) 프리-와인드 연출
            isPrepping = true;
            if (spriter != null && preWindUp > 0f)
            {
                spriter.DOKill();
                spriter.color = Color.white;
                spriter.DOColor(Color.red, preWindUp).SetEase(Ease.Linear);
            }

            // 3) 발사 시각까지 대기
            now = Time.timeAsDouble;
            if (nextFireAt > now)
                yield return new WaitForSeconds((float)(nextFireAt - now));

            // 4) 발사
            Shoot(dir);

            // 5) 색상 원복
            if (spriter != null)
            {
                spriter.DOKill();
                spriter.DOColor(Color.white, 0.1f);
            }
            isPrepping = false;

            // 6) 다음 phase
            phaseIdx++;
            if (phaseIdx >= firePhases.Length)
            {
                phaseIdx = 0;
                cycleBase += cycleLength;
            }
            nextFireAt = cycleBase + firePhases[phaseIdx];

            yield return null;
        }
    }

    void Shoot(Vector2 dir)
    {
        if (!bulletPrefab || !PoolManager.Instance) return;

        GameObject bullet = PoolManager.Instance.SpawnFromPool(
            bulletPrefab.name,
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

    public void ResetCycle(double delay = 0.0)
    {
        cycleBase = Time.timeAsDouble + delay;
        phaseIdx = 0;
        nextFireAt = cycleBase + firePhases[phaseIdx];
    }

    private void OnDestroy()
    {
        if (lineRenderer != null) Destroy(lineRenderer);
        isLive = false;
    }
}
