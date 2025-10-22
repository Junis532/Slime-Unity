using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(BossAnimation))]
public class FireBoss : EnemyBase
{
    private bool isLive = true;
    private SpriteRenderer spriter;
    private BossAnimation enemyAnimation;
    private NavMeshAgent agent;
    private Transform playerTransform;

    [Header("패턴 타이밍")]
    public float skillInterval = 4f;
    private float skillTimer = 0f;
    private bool isSkillPlaying = false;
    private int currentSkillIndex;

    [Header("파이어볼 원형 탄막")]
    public GameObject fireball360Prefab; // 기존 skill1Prefab과 구분

    [Header("파이어볼 360 & 타겟 발사 설정")]
    public GameObject fireballPrefab;
    public GameObject fireballWarningPrefab;
    public int fireballCount360 = 12;
    public float fireballSpawnRadius = 1.5f;
    public float warningDuration = 1f;
    public float fireballRepeatInterval = 1.5f;
    private int bossHitCount = 0;
    private bool playerHit = false;
    private Coroutine fireballCoroutine; // 🔹 스킬 1 코루틴 참조


    [Header("스킬 1 오브젝트")]
    public GameObject skill1Prefab; // y+1에 생성할 프리팹
    private GameObject activeSkill1Object; // 현재 생성된 오브젝트 참조

    [Header("검 스킬")]
    public GameObject swordPrefab;
    public float swordSpawnDistance = 1f;
    public GameObject swordRangePrefab;
    public float swordRangeDistance = 1.5f;

    [Header("범위/원 스킬")]
    public GameObject[] warningCirclePrefabs = new GameObject[3];
    public GameObject[] damageCirclePrefabs = new GameObject[3];
    public GameObject[] damageCircleEffectPrefabs = new GameObject[3];
    public float[] damageCircleEffectDurations = new float[3] { 1f, 1f, 1f };
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
            case 0:
                fireballCoroutine = StartCoroutine(FireballSkill());
                break;
            case 1:
                StartCoroutine(WarningCircleSkill());
                break;
            case 2:
                StartCoroutine(DoubleSwordSkill());
                break;
        }
    }

    // ────────── 스킬 1: 파이어볼 360 + 타겟 반복 ──────────
    private IEnumerator FireballSkill()
    {
        enemyAnimation?.PlayAnimation(BossAnimation.State.Skill1Fireball);
        Vector2 origin = transform.position;

        bossHitCount = 0;
        playerHit = false;

        // 🔥 스킬 1 오브젝트 생성 (한 번만)
        if (skill1Prefab != null && activeSkill1Object == null)
        {
            activeSkill1Object = Instantiate(skill1Prefab, transform.position + Vector3.up * 1f, Quaternion.identity);
        }

        while (!playerHit && bossHitCount < 6)
        {
            if (activeSkill1Object != null)
                activeSkill1Object.transform.position = transform.position + Vector3.up * 1f;

            yield return StartCoroutine(FireballWarningAndCircle(origin, fireballCount360));
            yield return StartCoroutine(FireballWarningToPlayer(origin));
            yield return new WaitForSeconds(fireballRepeatInterval);
        }

        yield return StartCoroutine(SkillEndDelay());

        if (activeSkill1Object != null)
        {
            Destroy(activeSkill1Object);
            activeSkill1Object = null;
        }

        fireballCoroutine = null;
    }

    private IEnumerator FireballWarningToPlayer(Vector2 origin)
    {
        if (playerTransform == null) yield break;

        Vector2 dir = (playerTransform.position - transform.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        GameObject warning = null;
        if (fireballWarningPrefab != null)
        {
            Vector2 warnPos = origin + dir * fireballSpawnRadius;
            warning = Instantiate(fireballWarningPrefab, warnPos, Quaternion.Euler(0f, 0f, angle));
            activeSkillObjects.Add(warning);
        }

        float elapsed = 0f;
        while (elapsed < warningDuration)
        {
            if (warning != null)
            {
                Vector2 warnPos = origin + dir * fireballSpawnRadius;
                warning.transform.position = warnPos;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (warning != null)
        {
            activeSkillObjects.Remove(warning);
            Destroy(warning);
        }

        FireInDirection(origin, angle - 90f);
    }

    private IEnumerator FireballWarningAndCircle(Vector2 origin, int count)
    {
        List<GameObject> warnings = new List<GameObject>();
        float angleStep = 360f / count;

        for (int i = 0; i < count; i++)
        {
            float angle = i * angleStep;
            Vector2 pos = origin + new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * fireballSpawnRadius;

            if (fireballWarningPrefab != null)
            {
                GameObject w = Instantiate(fireballWarningPrefab, pos, Quaternion.Euler(0, 0, angle));
                warnings.Add(w);
                activeSkillObjects.Add(w);
            }
        }

        float elapsed = 0f;
        while (elapsed < warningDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        foreach (var w in warnings)
        {
            if (w != null)
            {
                activeSkillObjects.Remove(w);
                Destroy(w);
            }
        }

        // 🔹 여기서 원형 탄막 발사
        for (int i = 0; i < count; i++)
        {
            float angle = i * angleStep - 90f;

            if (fireball360Prefab != null)
            {
                GameObject fireball = Instantiate(fireball360Prefab, origin, Quaternion.Euler(0f, 0f, angle));
                Vector2 direction = new Vector2(Mathf.Cos((angle + 90f) * Mathf.Deg2Rad), Mathf.Sin((angle + 90f) * Mathf.Deg2Rad));
                fireball.GetComponent<BossFireballProjectile>()?.Init(direction);
                activeSkillObjects.Add(fireball);
            }
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

            if (prevDamage != null)
            {
                Destroy(prevDamage);
                prevDamage = null;
            }

            if (warningCirclePrefabs[i] != null)
            {
                GameObject warning = Instantiate(warningCirclePrefabs[i], center, Quaternion.identity);
                activeSkillObjects.Add(warning);
                yield return new WaitForSeconds(warningCircleDuration);
                Destroy(warning);
            }

            if (damageCirclePrefabs[i] != null)
            {
                GameObject damage = Instantiate(damageCirclePrefabs[i], center, Quaternion.identity);
                activeSkillObjects.Add(damage);
                prevDamage = damage;

                Collider2D col = damage.GetComponent<Collider2D>();
                if (col != null)
                    StartCoroutine(DisableColliderAfterTime(col, 0.1f));

                StartCoroutine(HandleDamageEffect(i, center, damage));
                yield return new WaitForSeconds(damageCircleDuration);

                if (damage != null)
                    Destroy(damage);
            }
        }

        if (prevDamage != null)
            Destroy(prevDamage);

        yield return StartCoroutine(SkillEndDelay());
    }

    private IEnumerator HandleDamageEffect(int index, Vector3 center, GameObject damage)
    {
        if (damageCircleEffectPrefabs != null &&
            index < damageCircleEffectPrefabs.Length &&
            damageCircleEffectPrefabs[index] != null)
        {
            float duration = damageCircleEffectDurations[index];
            GameObject fx = Instantiate(damageCircleEffectPrefabs[index], center, Quaternion.identity);
            yield return new WaitForSeconds(duration);
            if (fx != null) Destroy(fx);
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
                yield return new WaitForSeconds(0.5f);
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

    public void OnPlayerHit()
    {
        playerHit = true;
    }

    public void OnBossTakeDamage()
    {
        bossHitCount++;

        // ────────── 스킬1 자식 색 변경 ──────────
        if (activeSkill1Object != null)
        {
            string childName = Mathf.Clamp(bossHitCount, 1, 5).ToString();
            Transform hitChild = activeSkill1Object.transform.Find(childName);
            if (hitChild != null)
            {
                SpriteRenderer sr = hitChild.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    Sequence seq = DOTween.Sequence();
                    seq.Append(sr.DOColor(Color.cyan, 0.3f)); // 0.3초 동안 하늘색
                    seq.Join(hitChild.DOScale(0.5f, 0.15f).SetLoops(2, LoopType.Yoyo)); // 커졌다가 원래 크기로
                }
            }
        }
        if (bossHitCount >= 5)
        {
            // ────────── 스킬1만 종료 ──────────
            if (fireballCoroutine != null && currentSkillIndex == 0)
            {
                StopCoroutine(fireballCoroutine);
                fireballCoroutine = null;

                if (activeSkill1Object != null)
                {
                    Destroy(activeSkill1Object);
                    activeSkill1Object = null;
                }

                ClearAllSkillObjects();

                StartCoroutine(SkillEndDelay());
                bossHitCount = 0;
            }
        }
   
    }
}
