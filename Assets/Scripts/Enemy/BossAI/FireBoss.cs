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
    private bool isSkillPlaying = false;    // 스킬 중엔 행동 금지
    private int currentSkillIndex;

    // ────────── 시각 효과 ──────────
    [Header("범위 표시 프리팹")]
    public GameObject dashPreviewPrefab;
    private GameObject dashPreviewInstance;

    // ────────── 예시 스킬(포션) ──────────
    [Header("포션 관련")]
    public GameObject potionPrefab;
    public float potionLifetime = 2f;

    [Header("파이어볼 관련")]
    public GameObject fireballPrefab;
    public int numberOfFireballs = 36;

    [Header("파이어볼 경고 프리팹")]
    public GameObject fireballWarningPrefab;
    public float warningDuration = 1f;
    public float fireballSpawnRadius = 1.5f;

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

        // NavMeshAgent 설정
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
            // 스킬 중에는 이동을 멈춤
            agent.SetDestination(transform.position);
            return;
        }

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        // NavMeshAgent를 사용한 이동
        agent.SetDestination(player.transform.position);

        // 스킬 타이머
        skillTimer += Time.deltaTime;
        if (skillTimer >= skillInterval)
        {
            skillTimer = 0f;
            currentSkillIndex = Random.Range(0, 1); // 현재 1가지 스킬만
            UseRandomSkill();
        }

        // 애니메이션 및 스프라이트 뒤집기
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

    // ────────── 랜덤 스킬 ──────────
    private void UseRandomSkill()
    {
        isSkillPlaying = true;
        // 스킬 사용 중 이동 정지
        agent.isStopped = true;

        switch (currentSkillIndex)
        {
            case 0:
                StartCoroutine(SkillExplosionCoroutine());
                break;
            case 1:
                SkillPotion();
                break;
            case 2:
                SkillDash();
                break;
        }
    }

    private void SkillPotion()
    {
        if (potionPrefab != null)
        {
            Instantiate(potionPrefab, transform.position, Quaternion.identity);
        }
        StartCoroutine(SkillEndDelay());
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
        if (player == null)
            yield break;

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
            if (warn != null)
                Destroy(warn);
        }

        // 파이어볼 발사
        FireInDirection(origin, Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg - 90f);
    }

    private void FireInDirection(Vector2 origin, float angle)
    {
        GameObject fireball = Instantiate(fireballPrefab, origin, Quaternion.Euler(0f, 0f, angle));

        Vector2 direction = new Vector2(Mathf.Cos((angle + 90f) * Mathf.Deg2Rad), Mathf.Sin((angle + 90f) * Mathf.Deg2Rad)); // 90도 보정
        fireball.GetComponent<BossFireballProjectile>()?.Init(direction);
    }

    private void SkillDash()
    {
        StartCoroutine(SkillEndDelay());
    }

    private IEnumerator SkillEndDelay()
    {
        yield return new WaitForSeconds(1f);
        isSkillPlaying = false;
        // 스킬 종료 후 이동 재개
        agent.isStopped = false;
    }

    // ────────── 유틸 ──────────
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