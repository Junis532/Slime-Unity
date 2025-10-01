using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(BossAnimation))] // BossAnimation 사용
public class FireBoss : EnemyBase
{
    private bool isLive = true;
    private SpriteRenderer spriter;
    private BossAnimation enemyAnimation;   // BossAnimation
    private NavMeshAgent agent;

    private Transform playerTransform; // 플레이어 캐싱

    [Header("패턴 타이밍")]
    public float skillInterval = 4f;
    private float skillTimer = 0f;
    private bool isSkillPlaying = false;
    private int currentSkillIndex;

    [Header("파이어볼")]
    public GameObject fireballPrefab;
    public GameObject fireballWarningPrefab;
    public int numberOfFireballs = 36;
    public float fireballSpawnRadius = 1.5f;
    public float warningDuration = 1f;

    [Header("검 스킬")]
    public GameObject swordPrefab;
    public float swordSpawnDistance = 1f;
    public GameObject swordRangePrefab;
    public float swordRangeDistance = 1.5f;

    [Header("범위/원 스킬")]
    public GameObject[] warningCirclePrefabs = new GameObject[3];
    public GameObject[] damageCirclePrefabs = new GameObject[3];
    public float[] circleScales = new float[3] { 10f, 7.5f, 5f };
    public Vector3 skillCenterOffset = Vector3.zero;
    public float warningDelay = 1f;

    private List<GameObject> activeSkillObjects = new List<GameObject>();

    void Start()
    {
        spriter = GetComponent<SpriteRenderer>();
        enemyAnimation = GetComponent<BossAnimation>();
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.speed = speed;

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null) playerTransform = player.transform;

        // 시작 Idle
        enemyAnimation?.PlayAnimation(BossAnimation.State.Idle);
    }

    void Update()
    {
        if (!isLive) return;

        // 스킬 중엔 이동/애니메이션 갱신 중단(스킬만 재생)
        if (isSkillPlaying)
        {
            if (playerTransform != null)
                FlipSprite((playerTransform.position - transform.position).x);
            return;
        }

        // 이동 & 방향 애니메이션 (스킬 외 구간)
        if (enemyAnimation != null && playerTransform != null)
        {
            agent.SetDestination(playerTransform.position);

            bool isActuallyMoving = agent.isStopped == false && agent.velocity.sqrMagnitude > 0.01f;

            if (isActuallyMoving)
            {
                Vector2 moveDir = agent.velocity.normalized;
                enemyAnimation.PlayDirectionalMoveAnimation(moveDir);
                FlipSprite(moveDir.x);
            }
            else
            {
                Vector2 dirToPlayer = (playerTransform.position - transform.position).normalized;
                enemyAnimation.PlayDirectionalMoveAnimation(dirToPlayer);
                FlipSprite(dirToPlayer.x);
            }
        }
        else if (playerTransform == null)
        {
            enemyAnimation.PlayAnimation(BossAnimation.State.Idle);
        }

        // 스킬 타이머
        skillTimer += Time.deltaTime;
        if (skillTimer >= skillInterval)
        {
            skillTimer = 0f;
            currentSkillIndex = Random.Range(0, 3);
            UseRandomSkill();
        }
    }

    private void UseRandomSkill()
    {
        isSkillPlaying = true;
        if (agent != null) agent.isStopped = true;

        switch (currentSkillIndex)
        {
            case 0: StartCoroutine(FireballSkill()); break;
            case 1: StartCoroutine(WarningCircleSkill()); break;
            case 2: StartCoroutine(DoubleSwordSkill()); break; // 대시 2회(준비→대시(루프)→베기)
        }
    }

    // ────────── 스킬 1: 파이어볼 ──────────
    private IEnumerator FireballSkill()
    {
        enemyAnimation?.PlayAnimation(BossAnimation.State.Skill1Fireball);

        Vector2 origin = transform.position;
        yield return StartCoroutine(FireballWarningAndBurst(origin));
        yield return StartCoroutine(SkillEndDelay());
    }

    private IEnumerator FireballWarningAndBurst(Vector2 origin)
    {
        GameObject player = playerTransform != null ? playerTransform.gameObject : GameObject.FindWithTag("Player");
        if (player == null) yield break;

        Vector2 directionToPlayer = (player.transform.position - transform.position).normalized;
        Vector2 warnPos = origin + directionToPlayer * fireballSpawnRadius;

        GameObject warning = null;
        if (fireballWarningPrefab != null)
        {
            warning = Instantiate(fireballWarningPrefab, warnPos, Quaternion.identity);
            activeSkillObjects.Add(warning);
        }

        float elapsed = 0f;
        while (elapsed < warningDuration)
        {
            if (warning == null) break;
            directionToPlayer = (player.transform.position - transform.position).normalized;
            warnPos = (Vector2)transform.position + directionToPlayer * fireballSpawnRadius;
            warning.transform.position = warnPos;

            float angleDegrees = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg;
            warning.transform.rotation = Quaternion.Euler(0f, 0f, angleDegrees);

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (warning != null) Destroy(warning);
        FireInDirection(origin, Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg - 90f);
    }

    private void FireInDirection(Vector2 origin, float angle)
    {
        GameObject fireball = Instantiate(fireballPrefab, origin, Quaternion.Euler(0f, 0f, angle));
        Vector2 direction = new Vector2(Mathf.Cos((angle + 90f) * Mathf.Deg2Rad), Mathf.Sin((angle + 90f) * Mathf.Deg2Rad));
        fireball.GetComponent<BossFireballProjectile>()?.Init(direction);
        activeSkillObjects.Add(fireball);
    }

    // ────────── 스킬 2: 범위 원 ──────────
    [SerializeField] private float warningCircleDuration = 0.5f; // 경고 원 유지 시간
    [SerializeField] private float damageCircleDuration = 1.0f;  // 데미지 원 유지 시간

    private IEnumerator WarningCircleSkill()
    {
        Vector3 center = transform.position + skillCenterOffset;
        GameObject prevDamage = null;

        for (int i = 0; i < 3; i++)
        {
            // 🔹 애니메이션 각 원마다 재생
            enemyAnimation?.PlayAnimation(BossAnimation.State.Skill2Circle);

            // 🔹 이전 데미지 원 제거
            if (prevDamage != null)
            {
                Destroy(prevDamage);
                prevDamage = null;
            }

            // 🔹 경고 원 생성
            if (warningCirclePrefabs[i] != null)
            {
                GameObject warning = Instantiate(warningCirclePrefabs[i], center, Quaternion.identity);
                activeSkillObjects.Add(warning);

                yield return new WaitForSeconds(warningCircleDuration);
                Destroy(warning);
            }

            // 🔹 데미지 원 생성
            if (damageCirclePrefabs[i] != null)
            {
                GameObject damage = Instantiate(damageCirclePrefabs[i], center, Quaternion.identity);
                activeSkillObjects.Add(damage);
                prevDamage = damage;

                // 🔹 데미지 원의 Collider 꺼지게
                Collider2D col = damage.GetComponent<Collider2D>();
                if (col != null)
                {
                    StartCoroutine(DisableColliderAfterTime(col, damageCircleDuration));
                }
            }

            // 🔹 데미지 원 지속 시간만큼 대기
            yield return new WaitForSeconds(damageCircleDuration);
        }

        // 🔹 마지막 원 제거
        if (prevDamage != null)
            Destroy(prevDamage);

        yield return StartCoroutine(SkillEndDelay());
    }

    // 콜라이더 일정 시간 후 비활성화
    private IEnumerator DisableColliderAfterTime(Collider2D col, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (col != null)
            col.enabled = false;
    }


    // ────────── 스킬 3: 대시 2회 (각각 준비→대시(루프)→베기) ──────────
    private IEnumerator DoubleSwordSkill()
    {
        GameObject player = playerTransform != null ? playerTransform.gameObject : GameObject.FindWithTag("Player");
        if (player == null)
        {
            yield return StartCoroutine(SkillEndDelay());
            yield break;
        }

        Vector3 originalPos = transform.position;

        for (int j = 0; j < 2; j++)
        {
            // (1) 대시 준비 모션
            enemyAnimation.PlayAnimation(BossAnimation.State.Skill3DashStart);
            float dashStartDur = Mathf.Max(0.05f, enemyAnimation.GetNonLoopDuration(BossAnimation.State.Skill3DashStart));
            yield return new WaitForSeconds(dashStartDur);

            // 목표 지점(플레이어 좌/우)
            float sideOffset = 2.5f;
            float targetX = player.transform.position.x + (Random.value > 0.5f ? sideOffset : -sideOffset);
            Vector3 sideTarget = new Vector3(targetX, player.transform.position.y, transform.position.z);

            // (2) 대시 모션(루프) + 실제 이동
            enemyAnimation.PlaySkill3DashLoop();
            float dashTime = j == 0 ? 0.20f : 0.25f; // 두 번째 대시는 살짝 느리게
            float elapsed = 0f;
            Vector3 startPos = transform.position;
            while (elapsed < dashTime)
            {
                transform.position = Vector3.Lerp(startPos, sideTarget, elapsed / dashTime);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.position = sideTarget;

            // 🔹 첫 번째 대시 후 잠깐 대기
            if (j == 0)
            {
                yield return new WaitForSeconds(1f);
            }

            // (3) 베기 모션 + 히트박스/이펙트
            enemyAnimation.PlayAnimation(BossAnimation.State.Skill3Slash);

            Vector3 dir = (player.transform.position - transform.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            Quaternion rot = Quaternion.Euler(0f, 0f, angle);
            Vector3 slashPos = transform.position + dir * (swordSpawnDistance + 1.0f);

            if (swordRangePrefab != null)
            {
                GameObject range = Instantiate(swordRangePrefab, slashPos, rot);
                range.transform.localScale = Vector3.one * swordRangeDistance;
                Destroy(range, 0.25f);
                yield return new WaitForSeconds(0.25f);
            }

            GameObject sword = Instantiate(swordPrefab, slashPos, rot);
            activeSkillObjects.Add(sword);
            Destroy(sword, 0.5f);

            float slashDur = Mathf.Max(0.05f, enemyAnimation.GetNonLoopDuration(BossAnimation.State.Skill3Slash));
            yield return new WaitForSeconds(slashDur);

            // 🔹 각 Slash 직후 Idle로 복귀
            enemyAnimation.PlayAnimation(BossAnimation.State.Idle);
        }

        // 원래 위치로 복귀
        float returnTime = 0.4f;
        float returnElapsed = 0f;
        Vector3 returnStart = transform.position;
        Vector3 originalWorldPos = originalPos;
        while (returnElapsed < returnTime)
        {
            transform.position = Vector3.Lerp(returnStart, originalWorldPos, returnElapsed / returnTime);
            returnElapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = originalWorldPos;

        yield return StartCoroutine(SkillEndDelay());
    }

    // ────────── 스킬 종료 및 상태 복귀 ──────────
    private IEnumerator SkillEndDelay()
    {
        // 각 스킬 연출 여유시간(필요 시 조절)
        yield return new WaitForSeconds(1f);

        isSkillPlaying = false;
        if (agent != null) agent.isStopped = false;
    }

    public void ClearAllSkillObjects()
    {
        foreach (var obj in activeSkillObjects)
            if (obj != null) Destroy(obj);
        activeSkillObjects.Clear();
    }

    public void SetDead()
    {
        isLive = false;
        ClearAllSkillObjects();
        if (agent != null) agent.isStopped = true;
        enemyAnimation?.PlayAnimation(BossAnimation.State.Idle);
    }

    private void FlipSprite(float dirX)
    {
        if (Mathf.Abs(dirX) > 0.01f)
        {
            float targetSign = dirX < 0 ? -1 : 1;
            float currentSign = Mathf.Sign(transform.localScale.x);
            if (!Mathf.Approximately(targetSign, currentSign))
            {
                Vector3 scale = transform.localScale;
                scale.x = Mathf.Abs(scale.x) * targetSign;
                transform.localScale = scale;
            }
        }
    }
}
