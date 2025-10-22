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
    public GameObject[] damageCircleEffectPrefabs = new GameObject[3]; // ✅ 원별 이펙트 프리팹
    public float[] damageCircleEffectDurations = new float[3] { 1f, 1f, 1f }; // ✅ 원별 이펙트 유지 시간
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

        if (isSkillPlaying)
        {
            if (playerTransform != null)
                FlipSprite((playerTransform.position - transform.position).x);
            return;
        }

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
            case 2: StartCoroutine(DoubleSwordSkill()); break;
        }
    }

    // ────────── 스킬 1: 파이어볼 (부채꼴 3발 × 3회 반복) ──────────
    private IEnumerator FireballSkill()
    {
        enemyAnimation?.PlayAnimation(BossAnimation.State.Skill1Fireball);

        Vector2 origin = transform.position;

        // 반복마다 발사 개수 지정
        int[] shotsPerWave = { 3, 4, 5 };

        for (int i = 0; i < shotsPerWave.Length; i++)
        {
            yield return StartCoroutine(FireballWarningAndBurstFan(origin, shotsPerWave[i]));
            yield return new WaitForSeconds(0.4f); // 각 발사 사이 텀
        }

        yield return StartCoroutine(SkillEndDelay());
    }

    // 발사 개수를 인자로 받도록 수정
    private IEnumerator FireballWarningAndBurstFan(Vector2 origin, int shotCount)
    {
        GameObject player = playerTransform != null ? playerTransform.gameObject : GameObject.FindWithTag("Player");
        if (player == null) yield break;

        Vector2 directionToPlayer = (player.transform.position - transform.position).normalized;

        // 부채꼴 범위: 예를 들어 총 ±30도
        float totalSpread = 90f;
        float angleStep = shotCount > 1 ? totalSpread / (shotCount - 1) : 0f;
        float baseAngle = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg - totalSpread / 2f;

        // 1. 경고 표시
        List<GameObject> warnings = new List<GameObject>();
        for (int i = 0; i < shotCount; i++)
        {
            float currentAngle = baseAngle + i * angleStep;
            Vector2 dir = new Vector2(Mathf.Cos(currentAngle * Mathf.Deg2Rad), Mathf.Sin(currentAngle * Mathf.Deg2Rad));
            Vector2 warnPos = origin + dir * fireballSpawnRadius;

            if (fireballWarningPrefab != null)
            {
                GameObject warning = Instantiate(fireballWarningPrefab, warnPos, Quaternion.Euler(0f, 0f, currentAngle));
                warnings.Add(warning);
                activeSkillObjects.Add(warning);
            }
        }

        // 2. 경고 지속 시간
        float elapsed = 0f;
        while (elapsed < warningDuration)
        {
            directionToPlayer = (player.transform.position - transform.position).normalized;
            baseAngle = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg - totalSpread / 2f;

            for (int i = 0; i < warnings.Count; i++)
            {
                if (warnings[i] == null) continue;
                float currentAngle = baseAngle + i * angleStep;
                Vector2 dir = new Vector2(Mathf.Cos(currentAngle * Mathf.Deg2Rad), Mathf.Sin(currentAngle * Mathf.Deg2Rad));
                warnings[i].transform.position = (Vector2)origin + dir * fireballSpawnRadius;
                warnings[i].transform.rotation = Quaternion.Euler(0f, 0f, currentAngle);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 3. 경고 제거
        foreach (var w in warnings)
        {
            if (w != null)
            {
                activeSkillObjects.Remove(w);
                Destroy(w);
            }
        }

        // 4. 발사
        for (int i = 0; i < shotCount; i++)
        {
            float launchAngle = baseAngle + i * angleStep - 90f; // FireInDirection 로직에 맞춤
            FireInDirection(origin, launchAngle);
        }
    }


    private void FireInDirection(Vector2 origin, float angle)
    {
        GameObject fireball = Instantiate(fireballPrefab, origin, Quaternion.Euler(0f, 0f, angle));
        Vector2 direction = new Vector2(Mathf.Cos((angle + 90f) * Mathf.Deg2Rad), Mathf.Sin((angle + 90f) * Mathf.Deg2Rad));
        fireball.GetComponent<BossFireballProjectile>()?.Init(direction);
        activeSkillObjects.Add(fireball);
    }


    // ────────── 스킬 2: 범위 원 ──────────
    [SerializeField] private float warningCircleDuration = 0.5f;
    [SerializeField] private float damageCircleDuration = 0.5f;

    private IEnumerator WarningCircleSkill()
    {
        Vector3 center = transform.position + skillCenterOffset;
        GameObject prevDamage = null;

        for (int i = 0; i < 3; i++)
        {
            enemyAnimation?.PlayAnimation(BossAnimation.State.Skill2Circle);

            // 🔹 이전 데미지 오브젝트 정리
            if (prevDamage != null)
            {
                Destroy(prevDamage);
                prevDamage = null;
            }

            // 🔹 경고 표시
            if (warningCirclePrefabs[i] != null)
            {
                GameObject warning = Instantiate(warningCirclePrefabs[i], center, Quaternion.identity);
                activeSkillObjects.Add(warning);
                yield return new WaitForSeconds(warningCircleDuration);
                Destroy(warning);
            }

            // 🔹 데미지 서클 생성
            if (damageCirclePrefabs[i] != null)
            {
                GameObject damage = Instantiate(damageCirclePrefabs[i], center, Quaternion.identity);
                activeSkillObjects.Add(damage);
                prevDamage = damage;

                // 일정 시간 후 데미지 판정 종료
                Collider2D col = damage.GetComponent<Collider2D>();
                if (col != null)
                    StartCoroutine(DisableColliderAfterTime(col, 0.1f)); // 즉시 판정 후 비활성화

                // 🔹 데미지 지속시간과 별도로 이펙트는 따로 작동
                StartCoroutine(HandleDamageEffect(i, center, damage));

                yield return new WaitForSeconds(damageCircleDuration); // 데미지 판정 지속시간
                if (damage != null)
                    Destroy(damage);
            }
        }

        if (prevDamage != null)
            Destroy(prevDamage);

        yield return StartCoroutine(SkillEndDelay());
    }

    // 이펙트는 데미지와 별도로 작동하는 코루틴
    private IEnumerator HandleDamageEffect(int index, Vector3 center, GameObject damage)
    {
        if (damageCircleEffectPrefabs != null &&
            index < damageCircleEffectPrefabs.Length &&
            damageCircleEffectPrefabs[index] != null)
        {
            float duration = damageCircleEffectDurations[index];

            // 프리팹 스케일 그대로 사용
            GameObject fx = Instantiate(damageCircleEffectPrefabs[index], center, Quaternion.identity);

            yield return new WaitForSeconds(duration);

            if (fx != null)
                Destroy(fx);
        }
    }



    private IEnumerator DisableColliderAfterTime(Collider2D col, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (col != null)
            col.enabled = false;
    }

    // ────────── 스킬 3: 대시 2회 ──────────
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
            enemyAnimation.PlayAnimation(BossAnimation.State.Skill3DashStart);
            float dashStartDur = Mathf.Max(0.05f, enemyAnimation.GetNonLoopDuration(BossAnimation.State.Skill3DashStart));
            yield return new WaitForSeconds(dashStartDur);

            float sideOffset = 2.5f;
            float targetX = player.transform.position.x + (Random.value > 0.5f ? sideOffset : -sideOffset);
            Vector3 sideTarget = new Vector3(targetX, player.transform.position.y, transform.position.z);

            enemyAnimation.PlaySkill3DashLoop();
            float dashTime = j == 0 ? 0.20f : 0.25f;
            float elapsed = 0f;
            Vector3 startPos = transform.position;
            while (elapsed < dashTime)
            {
                transform.position = Vector3.Lerp(startPos, sideTarget, elapsed / dashTime);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.position = sideTarget;

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

            enemyAnimation.PlayAnimation(BossAnimation.State.Idle);

            if (j == 0)
                yield return new WaitForSeconds(0.7f);
        }

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

    private IEnumerator SkillEndDelay()
    {
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
