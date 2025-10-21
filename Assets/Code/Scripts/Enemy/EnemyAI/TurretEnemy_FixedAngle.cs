using UnityEngine;
using System.Collections;
using DG.Tweening;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(EnemyAnimation))]
public class TurretEnemy_FixedAngle : MonoBehaviour
{
    [Header("🎯 애니메이션 (EnemyAnimation 사용)")]
    public EnemyAnimation enemyAnim;

    private bool isLive = true;
    private SpriteRenderer spriter;

    [Header("발사 범위 / 라인 표시")]
    public float fireRange = 5f;

    [Header("첫 발사 딜레이")]
    public float firstFireDelay = 0f;

    [Header("발사 전 예열 연출")]
    public float preWindUp = 0.15f;

    [Header("Bullet 설정")]
    public GameObject bulletPrefab;
    public GameObject secondaryBulletPrefab;
    public float bulletSpeed = 1.5f;
    public float bulletLifetime = 3f;

    [Header("두 번째 Bullet 속도 변경")]
    public float secondaryDelay = 1f;
    public float secondarySpeed = 2f;

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

    // 정면 판정 허용 각도
    private const float VerticalTolerance = 25f;

    void Awake()
    {
        spriter = GetComponent<SpriteRenderer>();
        if (!enemyAnim) enemyAnim = GetComponent<EnemyAnimation>();
        if (!enemyAnim) Debug.LogError("EnemyAnimation을 지정하세요.");

        // LineRenderer
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

        // 준비/사격 중이 아닐 때만 Idle 유지 (EnemyAnimation이 같은 상태면 무시)
        if (enemyAnim != null && !isPrepping && !isShooting)
            enemyAnim.PlayAnimation(EnemyAnimation.State.Idle);
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
                yield return new WaitForSeconds((float)(prepStart - now));

            // ===== 발사 준비 =====
            isPrepping = true;

            // 각도 기반으로 '정면/측면' 자세를 먼저 설정 → AttackStart가 Front용/Side용 선택될 수 있게 함
            enemyAnim?.PlayDirectionalMoveAnimation(dir);

            // AttackStart 재생 (EnemyAnimation이 이전 Move 상태를 보고 Front/Side 준비 스프라이트를 고름)
            enemyAnim?.PlayAnimation(EnemyAnimation.State.AttackStart);

            if (spriter != null && preWindUp > 0f)
            {
                spriter.DOKill();
                spriter.color = Color.white;
                spriter.DOColor(Color.red, preWindUp).SetEase(Ease.Linear);
            }

            now = Time.timeAsDouble;
            if (nextFireAt > now)
                yield return new WaitForSeconds((float)(nextFireAt - now));

            // ===== 발사 =====
            isPrepping = false;
            isShooting = true;
            Shoot(dir);

            // 후딜 애니: 각도에 따라 FrontAttackEnd 또는 AttackEnd 선택
            bool isFront = IsFrontAngle(fixedAngle);
            var postState = isFront ? EnemyAnimation.State.FrontAttackEnd : EnemyAnimation.State.AttackEnd;

            float postDuration = 0f;
            if (enemyAnim != null)
            {
                enemyAnim.PlayAnimation(postState);
                postDuration = enemyAnim.GetEstimatedDuration(postState);
            }

            spriter?.DOKill();
            spriter?.DOColor(Color.white, 0.1f);

            if (postDuration > 0f)
                yield return new WaitForSeconds(postDuration);

            isShooting = false;

            // 다음 사이클
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

    private bool IsFrontAngle(float ang)
    {
        ang = (ang % 360f + 360f) % 360f;
        return Mathf.Abs(ang - 90f) <= VerticalTolerance ||
               Mathf.Abs(ang - 270f) <= VerticalTolerance;
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
