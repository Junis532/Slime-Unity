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

    [Header("Bullet 설정")]
    public GameObject bulletPrefab;                 // 기존 Bullet
    public GameObject secondaryBulletPrefab;        // 속도 바꿀 Bullet
    public float bulletSpeed = 1.5f;               // 초기 속도
    public float bulletLifetime = 3f;

    [Header("두 번째 Bullet 속도 변경")]
    public float secondaryDelay = 1f;              // 몇 초 후 속도 변경
    public float secondarySpeed = 2f;              // 바뀔 속도

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
    private bool isShooting = false;

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

        float rad = fixedAngle * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;

        if (showLineRenderer && lineRenderer != null)
        {
            lineRenderer.enabled = true;
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, (Vector2)transform.position + dir * fireRange);
        }

        if (turretAnim != null && !isPrepping && !isShooting)
            turretAnim.PlayAnimation(TurretEnemyAnimation.State.Idle, fixedAngle);
    }

    private IEnumerator PhaseScheduleLoop()
    {
        while (isLive)
        {
            float rad = fixedAngle * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;

            double prepStart = nextFireAt - preWindUp;
            double now = Time.timeAsDouble;

            if (prepStart > now)
            {
                yield return new WaitForSeconds((float)(prepStart - now));
            }

            // 발사 준비
            isPrepping = true;
            TurretEnemyAnimation.State prepareState = GetPrepareState(fixedAngle);
            if (turretAnim != null)
                turretAnim.PlayAnimation(prepareState);

            if (spriter != null && preWindUp > 0f)
            {
                spriter.DOKill();
                spriter.color = Color.white;
                spriter.DOColor(Color.red, preWindUp).SetEase(Ease.Linear);
            }

            now = Time.timeAsDouble;
            if (nextFireAt > now)
                yield return new WaitForSeconds((float)(nextFireAt - now));

            // 발사
            isPrepping = false;
            isShooting = true;
            Shoot(dir);

            TurretEnemyAnimation.State postState = GetPostState(fixedAngle);
            float postDuration = 0f;
            if (turretAnim != null)
            {
                turretAnim.PlayAnimation(postState);
                postDuration = turretAnim.GetNonLoopDuration(postState);
            }

            if (spriter != null)
            {
                spriter.DOKill();
                spriter.DOColor(Color.white, 0.1f);
            }

            if (postDuration > 0f)
                yield return new WaitForSeconds(postDuration);

            isShooting = false;

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
        GameObject bulletToShoot = null;

        // 랜덤으로 선택
        if (bulletPrefab && secondaryBulletPrefab)
            bulletToShoot = (Random.value < 0.5f) ? bulletPrefab : secondaryBulletPrefab;
        else if (bulletPrefab)
            bulletToShoot = bulletPrefab;
        else if (secondaryBulletPrefab)
            bulletToShoot = secondaryBulletPrefab;
        else
            return;

        GameObject bullet = Instantiate(bulletToShoot, transform.position, Quaternion.identity);
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb)
            rb.linearVelocity = dir.normalized * bulletSpeed;

        // 두 번째 Bullet이면 일정 시간 후 속도 변경
        if (bulletToShoot == secondaryBulletPrefab && rb != null && secondaryDelay > 0f)
            StartCoroutine(ChangeBulletSpeed(rb, secondaryDelay, secondarySpeed));

        Destroy(bullet, bulletLifetime);
    }

    private IEnumerator ChangeBulletSpeed(Rigidbody2D rb, float delay, float newSpeed)
    {
        yield return new WaitForSeconds(delay);
        if (rb != null)
            rb.linearVelocity = rb.linearVelocity.normalized * newSpeed;
    }

    private TurretEnemyAnimation.State GetPrepareState(float angle)
    {
        angle = (angle % 360 + 360) % 360;
        float verticalTolerance = 25f;
        if ((angle >= 90f - verticalTolerance && angle <= 90f + verticalTolerance) ||
            (angle >= 270f - verticalTolerance && angle <= 270f + verticalTolerance))
            return TurretEnemyAnimation.State.FrontShootPrepare;
        return TurretEnemyAnimation.State.ShootPrepare;
    }

    private TurretEnemyAnimation.State GetPostState(float angle)
    {
        angle = (angle % 360 + 360) % 360;
        float verticalTolerance = 25f;
        if ((angle >= 90f - verticalTolerance && angle <= 90f + verticalTolerance) ||
            (angle >= 270f - verticalTolerance && angle <= 270f + verticalTolerance))
            return TurretEnemyAnimation.State.FrontShootPost;
        return TurretEnemyAnimation.State.ShootPost;
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
