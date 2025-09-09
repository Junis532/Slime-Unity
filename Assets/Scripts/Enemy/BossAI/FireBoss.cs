using DG.Tweening;
using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
public class FireBoss : EnemyBase
{
    // ────────── 기본 데이터 ──────────
    private bool isLive = true;
    private SpriteRenderer spriter;
    private EnemyAnimation enemyAnimation;
    private NavMeshAgent agent;

    // ────────── 스킬/타이밍 ──────────
    [Header("패턴 타이밍")]
    public float skillInterval = 4f;
    private float skillTimer = 0f;
    private bool isSkillPlaying = false;
    private int currentSkillIndex;

    // ────────── 시각 효과 ──────────
    [Header("범위 표시 프리팹")]
    public GameObject dashPreviewPrefab;
    private GameObject dashPreviewInstance;

    // ────────── 스킬 관련 ──────────
    [Header("파이어볼")]
    public GameObject fireballPrefab;
    public int numberOfFireballs = 36;
    public GameObject fireballWarningPrefab;
    public float warningDuration = 1f;
    public float fireballSpawnRadius = 1.5f;

    [Header("경고/데미지 원")]
    public GameObject[] warningCirclePrefabs = new GameObject[3];
    public GameObject[] damageCirclePrefabs = new GameObject[3];
    public float[] circleScales = new float[3] { 10.0f, 7.5f, 5.0f };
    public Vector3 skillCenterOffset = Vector3.zero;
    public float warningDelay = 1f;

    [Header("검 스킬")]
    public GameObject swordPrefab;
    public float swordSpawnDistance = 1f;
    public GameObject swordRangePrefab;
    public float swordRangeDistance = 1.5f;

    // ────────── 초기화 ──────────
    void Start()
    {
        spriter = GetComponent<SpriteRenderer>();
        enemyAnimation = GetComponent<EnemyAnimation>();
        agent = GetComponent<NavMeshAgent>();

        if (dashPreviewPrefab != null)
        {
            dashPreviewInstance = Instantiate(dashPreviewPrefab, transform.position, Quaternion.identity);
            dashPreviewInstance.SetActive(false);
        }

        originalSpeed = GameManager.Instance.boss1Stats.speed;
        speed = originalSpeed;

        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.speed = speed;
    }

    // ────────── 메인 루프 ──────────
    void Update()
    {
        if (!isLive) return;

        if (isSkillPlaying)
        {
            agent.SetDestination(transform.position);
            return;
        }

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        agent.SetDestination(player.transform.position);

        skillTimer += Time.deltaTime;
        if (skillTimer >= skillInterval)
        {
            skillTimer = 0f;
            currentSkillIndex = Random.Range(0, 3); // 현재 1가지 스킬
            UseRandomSkill();
        }

        Vector2 dir = agent.velocity;
        if (dir.magnitude > 0.1f)
        {
            enemyAnimation.PlayAnimation(EnemyAnimation.State.Move);
            FlipSprite(dir.x);
        }
        else
        {
            enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);
        }
    }

    private void UseRandomSkill()
    {
        isSkillPlaying = true;
        agent.isStopped = true;

        switch (currentSkillIndex)
        {
            case 0:
                StartCoroutine(SkillExplosionCoroutine());
                break;
            case 1:
                StartCoroutine(SkillWarningSequencePattern());
                break;
            case 2:
                StartCoroutine(SkillDoubleAttackPattern());
                break;
            case 3:
                
                break;
        }
    }

    private IEnumerator SkillWarningSequencePattern()
    {
        yield return StartCoroutine(SkillWarningSequenceCoroutine());
        yield return StartCoroutine(SkillEndDelay());
    }

    private IEnumerator SkillExplosionCoroutine()
    {
        if (fireballPrefab == null)
        {
            StartCoroutine(SkillEndDelay());
            yield break;
        }

        Vector2 origin = transform.position;
        yield return StartCoroutine(FireballWarningAndBurst(origin, 1, 360f, 0f));
        StartCoroutine(SkillEndDelay());
    }

    private IEnumerator FireballWarningAndBurst(Vector2 origin, int count, float angleStep, float angleOffset)
    {
        List<GameObject> warnings = new List<GameObject>();
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) yield break;

        Vector2 directionToPlayer = (player.transform.position - transform.position).normalized;
        Vector2 warnPos = origin + directionToPlayer * fireballSpawnRadius;

        if (fireballWarningPrefab != null)
        {
            GameObject warn = Instantiate(fireballWarningPrefab, warnPos, Quaternion.identity);
            warnings.Add(warn);
        }

        float elapsed = 0f;
        while (elapsed < warningDuration)
        {
            if (warnings.Count == 0 || warnings[0] == null) break;

            directionToPlayer = (player.transform.position - transform.position).normalized;
            warnPos = (Vector2)transform.position + directionToPlayer * fireballSpawnRadius;
            warnings[0].transform.position = warnPos;

            float angleDegrees = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg;
            warnings[0].transform.rotation = Quaternion.Euler(0f, 0f, angleDegrees);

            elapsed += Time.deltaTime;
            yield return null;
        }

        foreach (var warn in warnings)
        {
            if (warn != null) Destroy(warn);
        }

        FireInDirection(origin, Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg - 90f);
    }

    private void FireInDirection(Vector2 origin, float angle)
    {
        GameObject fireball = Instantiate(fireballPrefab, origin, Quaternion.Euler(0f, 0f, angle));
        Vector2 direction = new Vector2(Mathf.Cos((angle + 90f) * Mathf.Deg2Rad), Mathf.Sin((angle + 90f) * Mathf.Deg2Rad));
        fireball.GetComponent<BossFireballProjectile>()?.Init(direction);
    }

    private IEnumerator SkillWarningSequenceCoroutine()
    {
        Vector3 center = transform.position + skillCenterOffset;
        GameObject prevDamage = null;

        for (int i = 0; i < 3; i++)
        {
            if (prevDamage != null)
            {
                Destroy(prevDamage);
                prevDamage = null;

                GameObject warning = Instantiate(warningCirclePrefabs[i], center, Quaternion.identity);
                warning.transform.localScale = Vector3.one * circleScales[i];

                yield return new WaitForSeconds(warningDelay);
                Destroy(warning);
            }
            else
            {
                GameObject warning = Instantiate(warningCirclePrefabs[i], center, Quaternion.identity);
                warning.transform.localScale = Vector3.one * circleScales[i];

                yield return new WaitForSeconds(warningDelay);
                Destroy(warning);
            }

            GameObject damage = Instantiate(damageCirclePrefabs[i], center, Quaternion.identity);
            damage.transform.localScale = Vector3.one * circleScales[i];
            prevDamage = damage;

            yield return new WaitForSeconds(0.3f);
        }

        if (prevDamage != null) Destroy(prevDamage);
    }

    private IEnumerator SkillDoubleAttackPattern()
    {
        isSkillPlaying = true;
        agent.isStopped = true;

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            isSkillPlaying = false;
            yield break;
        }

        // ── 0. 첫 번째 회전 범위 프리팹 생성 ──
        GameObject rangeInstance = null;
        if (swordRangePrefab != null)
        {
            Vector3 dirToPlayer = (player.transform.position - transform.position).normalized;
            Vector3 rangePos = transform.position + dirToPlayer * swordRangeDistance;
            rangeInstance = Instantiate(swordRangePrefab, rangePos, Quaternion.identity);
        }

        // ── 1. 대쉬 (NavMesh 안전 보정) ──
        Vector2 offset = Random.insideUnitCircle.normalized * 2f;
        Vector3 dashTarget = player.transform.position + (Vector3)offset;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(dashTarget, out hit, 2f, NavMesh.AllAreas))
            dashTarget = hit.position;
        else
            dashTarget = transform.position; // 유효 위치 없으면 제자리

        float dashTime = 0.3f;
        transform.DOMove(dashTarget, dashTime).SetEase(Ease.OutQuad);

        // 대쉬가 완료될 때까지 기다립니다.
        yield return new WaitForSeconds(dashTime);

        // ── 2. 범위 프리팹 초 회전 표시 ──
        float elapsed = 0f;
        float warningDuration = 1.5f;
        while (elapsed < warningDuration)
        {
            if (rangeInstance != null && player != null)
            {
                Vector3 dir = (player.transform.position - transform.position).normalized;
                rangeInstance.transform.position = transform.position + dir * swordRangeDistance;

                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                rangeInstance.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (rangeInstance != null) Destroy(rangeInstance);

        // ── 3. 첫 번째 검 휘두르기 ──
        Vector3 dir1 = (player.transform.position - transform.position).normalized;
        float baseAngle1 = Mathf.Atan2(dir1.y, dir1.x) * Mathf.Rad2Deg;
        float swordAngleStart1 = baseAngle1 - 90f - 60f;
        float swordAngleEnd1 = baseAngle1 - 90f + 60f;

        Vector3 swordPos1 = transform.position + dir1 * swordSpawnDistance;
        GameObject sword1 = Instantiate(swordPrefab, swordPos1, Quaternion.Euler(0, 0, swordAngleStart1), null);
        sword1.transform.DORotate(new Vector3(0, 0, swordAngleEnd1), 0.5f).SetEase(Ease.OutQuad)
            .OnComplete(() => Destroy(sword1));

        yield return new WaitForSeconds(0.5f);

        // ── 4. 돌진 (NavMesh 안전 보정) ──
        // 이 부분은 이미 돌진으로 구현되어 있어 변경하지 않습니다.
        dashTarget = player.transform.position;
        if (NavMesh.SamplePosition(dashTarget, out hit, 2f, NavMesh.AllAreas))
            dashTarget = hit.position;

        dashTime = 0.4f;
        transform.DOMove(dashTarget, dashTime).SetEase(Ease.OutQuad);
        yield return new WaitForSeconds(dashTime * 0.5f);

        // ── 5. 두 번째 회전 범위 프리팹 생성 후 초 표시 ──
        if (swordRangePrefab != null)
        {
            rangeInstance = Instantiate(swordRangePrefab, transform.position, Quaternion.identity);
            elapsed = 0f;
            warningDuration = 1.5f;

            while (elapsed < warningDuration)
            {
                if (rangeInstance != null && player != null)
                {
                    Vector3 dir = (player.transform.position - transform.position).normalized;
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    rangeInstance.transform.rotation = Quaternion.Euler(0f, 0f, angle);
                    rangeInstance.transform.position = transform.position + dir * swordRangeDistance;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(rangeInstance);
        }

        // ── 6. 두 번째 검 휘두르기 ──
        Vector3 dir2 = (player.transform.position - transform.position).normalized;
        float baseAngle2 = Mathf.Atan2(dir2.y, dir2.x) * Mathf.Rad2Deg;
        float swordAngleStart2 = baseAngle2 - 90f - 60f;
        float swordAngleEnd2 = baseAngle2 - 90f + 60f;

        Vector3 swordPos2 = transform.position + dir2 * swordSpawnDistance;
        GameObject sword2 = Instantiate(swordPrefab, swordPos2, Quaternion.Euler(0, 0, swordAngleStart2), null);
        sword2.transform.DORotate(new Vector3(0, 0, swordAngleEnd2), 0.5f).SetEase(Ease.OutQuad)
            .OnComplete(() => Destroy(sword2));

        yield return new WaitForSeconds(0.6f);

        // ── 7. 스킬 종료 ──
        yield return StartCoroutine(SkillEndDelay());
    }

    private void SkillDash()
    {
        StartCoroutine(SkillEndDelay());
    }

    private IEnumerator SkillEndDelay()
    {
        yield return new WaitForSeconds(1f);
        isSkillPlaying = false;
        agent.isStopped = false;
    }

    private void FlipSprite(float dirX)
    {
        Vector3 s = transform.localScale;
        s.x = Mathf.Abs(s.x) * (dirX < 0 ? -1 : 1);
        transform.localScale = s;
    }

    public override void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
        if (agent != null)
            agent.speed = newSpeed;
    }

    void OnDisable()
    {
        if (dashPreviewInstance != null)
            dashPreviewInstance.SetActive(false);
    }

    void OnDestroy()
    {
        if (dashPreviewInstance != null)
            Destroy(dashPreviewInstance);
    }
}
