using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;


// EnemyBase 클래스는 정의되지 않았지만, 컴파일을 위해 필요하다고 가정합니다.
// EnemyAnimation 클래스가 PlayAnimation, PlayDirectionalMoveAnimation 메서드를 가진다고 가정합니다.
// BossFireballProjectile 클래스가 존재하고 Init(Vector2) 메서드를 가진다고 가정합니다.


[RequireComponent(typeof(NavMeshAgent))]
public class FireBoss : EnemyBase
{

    private bool isLive = true;
    private SpriteRenderer spriter;
    private EnemyAnimation enemyAnimation;
    private NavMeshAgent agent;


    private Transform playerTransform; // 💡 NEW: 플레이어 트랜스폼 캐싱


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
        enemyAnimation = GetComponent<EnemyAnimation>();
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        // EnemyBase에서 상속받은 speed 사용
        agent.speed = speed;


        // 💡 NEW: 플레이어 트랜스폼 캐싱 (성능 개선)
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }


        // 시작 시 Idle 애니메이션으로 설정
        if (enemyAnimation != null)
        {
            enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);
        }
    }


    void Update()
    {
        if (!isLive) return;

        // 2. 스킬 타이머 처리
        if (isSkillPlaying)
        {
            // 스킬 중에는 플레이어를 바라보도록 반전 유지
            if (playerTransform != null)
                FlipSprite((playerTransform.position - transform.position).x);

            // 스킬 중에는 다른 모든 로직(이동/애니메이션/타이머)을 중단
            return;
        }

        // 1. 이동 및 애니메이션 처리 (스킬 미사용 중일 때)
        if (enemyAnimation != null && playerTransform != null)
        {
            // 💡 1. 플레이어 추적 명령
            agent.SetDestination(playerTransform.position);

            Vector2 moveDir;

            // NavMeshAgent가 실제 이동 중인지 확인합니다.
            bool isActuallyMoving = agent.isStopped == false && agent.velocity.sqrMagnitude > 0.01f;

            if (isActuallyMoving)
            {
                // 실제 이동 중: 실제 이동 방향에 따른 애니메이션 재생
                moveDir = agent.velocity.normalized;
                enemyAnimation.PlayDirectionalMoveAnimation(moveDir);
                FlipSprite(moveDir.x);
            }
            else // 멈춰있거나 목적지에 도착했을 때 (skillInterval 상태)
            {
                // 💡 FIX: Idle 대신 플레이어를 바라보는 Move 애니메이션을 강제 재생 (제자리 걸음 연출)
                Vector2 dirToPlayer = (playerTransform.position - transform.position).normalized;

                // 플레이어를 바라보는 방향으로의 Move 애니메이션을 요청합니다. 
                enemyAnimation.PlayDirectionalMoveAnimation(dirToPlayer);
                FlipSprite(dirToPlayer.x);
            }
        }
        else if (playerTransform == null)
        {
            // 플레이어가 없을 때 Idle 상태 유지
            enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);
        }

        // 3. 스킬 타이머 실행
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
        // 스킬 중에는 이동을 정지합니다.
        if (agent != null) agent.isStopped = true;

        switch (currentSkillIndex)
        {
            case 0:
                StartCoroutine(FireballSkill());
                break;
            case 1:
                StartCoroutine(WarningCircleSkill());
                break;
            case 2:
                StartCoroutine(DoubleSwordSkill());
                break;
        }
    }


    // ────────── 스킬 1: 파이어볼 ──────────
    private IEnumerator FireballSkill()
    {
        if (enemyAnimation != null)
        {
            enemyAnimation.PlayAnimation(EnemyAnimation.State.Skill1Fireball);
        }

        Vector2 origin = transform.position;
        // FindWithTag 대신 캐싱된 playerTransform을 사용하는 것이 성능상 더 좋습니다.
        GameObject player = playerTransform != null ? playerTransform.gameObject : GameObject.FindWithTag("Player");

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


        // 💡 BossFireballProjectile 클래스의 Init 호출
        fireball.GetComponent<BossFireballProjectile>()?.Init(direction);


        activeSkillObjects.Add(fireball);
    }


    // ────────── 스킬 2: 범위 원 ──────────
    private IEnumerator WarningCircleSkill()
    {
        if (enemyAnimation != null)
        {
            enemyAnimation.PlayAnimation(EnemyAnimation.State.Skill2Circle);
        }


        Vector3 center = transform.position + skillCenterOffset;
        GameObject prevDamage = null;


        for (int i = 0; i < 3; i++)
        {
            if (prevDamage != null)
            {
                Destroy(prevDamage);
                prevDamage = null;
            }


            GameObject warning = Instantiate(warningCirclePrefabs[i], center, Quaternion.identity);
            activeSkillObjects.Add(warning);


            yield return new WaitForSeconds(warningDelay);
            Destroy(warning);


            GameObject damage = Instantiate(damageCirclePrefabs[i], center, Quaternion.identity);
            activeSkillObjects.Add(damage);
            prevDamage = damage;


            yield return new WaitForSeconds(0.6f);
        }


        if (prevDamage != null) Destroy(prevDamage);
        yield return StartCoroutine(SkillEndDelay());
    }


    // ────────── 스킬 3: 검 스킬 (Lerp 기반 이동) ──────────
    private IEnumerator DoubleSwordSkill()
    {
        if (enemyAnimation != null)
        {
            // 대시 애니메이션 재생.
            enemyAnimation.PlayAnimation(EnemyAnimation.State.Skill3Dash);
        }


        GameObject player = playerTransform != null ? playerTransform.gameObject : GameObject.FindWithTag("Player");
        if (player == null)
        {
            yield return StartCoroutine(SkillEndDelay());
            yield break;
        }


        Vector3 originalPos = transform.position;


        for (int j = 0; j < 2; j++)
        {
            float sideOffset = 2.5f;
            float targetX = player.transform.position.x + (Random.value > 0.5f ? sideOffset : -sideOffset);
            Vector3 sideTarget = new Vector3(targetX, player.transform.position.y, transform.position.z);


            // 이동 Lerp
            float dashTime = j == 0 ? 0.2f : 0.25f;
            float elapsed = 0f;
            Vector3 startPos = transform.position;
            while (elapsed < dashTime)
            {
                transform.position = Vector3.Lerp(startPos, sideTarget, elapsed / dashTime);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.position = sideTarget;


            Vector3 dir = (player.transform.position - transform.position).normalized;


            float minDistanceFromPlayer = 1.5f;
            float swordForwardOffset = swordSpawnDistance + 1.0f;
            Vector3 swordPos = transform.position + dir * swordForwardOffset;


            float distanceToPlayer = Vector3.Distance(swordPos, player.transform.position);
            if (distanceToPlayer < minDistanceFromPlayer)
                swordPos += dir * (minDistanceFromPlayer - distanceToPlayer + 0.2f);


            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            Quaternion rot = Quaternion.Euler(0f, 0f, angle);


            if (swordRangePrefab != null)
            {
                GameObject range = Instantiate(swordRangePrefab, swordPos, rot);
                range.transform.localScale = Vector3.one * swordRangeDistance;
                Destroy(range, 0.25f);
                yield return new WaitForSeconds(0.25f);
            }


            GameObject sword = Instantiate(swordPrefab, swordPos, rot);
            activeSkillObjects.Add(sword);
            Destroy(sword, 0.5f);


            yield return new WaitForSeconds(0.35f);
        }


        // 원래 자리로 복귀
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
        // 1초 대기 (애니메이션이 끝나는 시간과 맞추기 위함)
        yield return new WaitForSeconds(1f);


        isSkillPlaying = false;


        // NavMeshAgent 정지 상태 해제
        if (agent != null)
        {
            agent.isStopped = false;
        }
    }


    public void ClearAllSkillObjects()
    {
        foreach (var obj in activeSkillObjects)
        {
            if (obj != null) Destroy(obj);
        }
        activeSkillObjects.Clear();
    }


    public void SetDead()
    {
        isLive = false;
        ClearAllSkillObjects();
        // 사망 시 NavMeshAgent 정지 및 Idle 애니메이션
        if (agent != null) agent.isStopped = true;
        // 💡 FIX: 사망 시 Move가 아닌 Idle 애니메이션을 재생합니다.
        if (enemyAnimation != null) enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);
    }


    private void FlipSprite(float dirX)
    {
        // 💡 개선: 방향이 실제로 바뀌었을 때만 Scale을 변경하여 불필요한 연산 방지
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