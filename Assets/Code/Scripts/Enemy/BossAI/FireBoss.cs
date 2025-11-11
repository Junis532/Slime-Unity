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
    private float skillTimer = 0f;
    private bool isSkillPlaying = false;
    private int currentSkillIndex;
    private int previousSkillIndex = -1;

    // ▶ 파이어볼 스킬 진행중/인터럽트 중복방지 플래그
    private bool isInFireballSkill = false;
    private bool isInterrupting = false;

    [Header("패턴 타이밍")]
    public float skillInterval = 4f;

    [Header("파이어볼 원형 탄막")]
    public GameObject fireball360Prefab;

    [Header("파이어볼 프리팹")]
    public GameObject fireballPrefab;

    [Header("파이어볼 경고 프리팹")]
    public GameObject fireballWarningPrefab;

    [Header("파이어볼 경고 거리")]
    public float fireballWarningDistance;

    [Header("파이어볼 부채꼴 각도")]
    public int fireballCount360 = 12;

    [Header("파이어볼 소환 거리")]
    public float fireballSpawnRadius = 1.5f;

    [Header("파이어볼 경고 시간")]
    public float warningDuration = 1f;

    [Header("파이어볼 반복 소환 시간")]
    public float fireballRepeatInterval = 1.5f;

    private int bossHitCount = 0;

    private Coroutine fireballCoroutine;

    [Header("스킬 1 오브젝트")]
    public GameObject skill1Prefab;

    private GameObject activeSkill1Object;

    [Header("검 스킬 프리팹")]
    public GameObject swordPrefab;

    [Header("검 소환 거리")]
    public float swordSpawnDistance = 1f;

    [Header("검 경고 표시 프리팹")]
    public GameObject swordRangePrefab;

    [Header("검 경고 표시 소환 거리")]
    public float swordRangeDistance = 1.5f;

    [Header("원 스킬 경고 프리팹")]
    public GameObject[] warningCirclePrefabs = new GameObject[3];

    [Header("원 스킬 프리팹")]
    public GameObject[] damageCirclePrefabs = new GameObject[3];

    [Header("원 스킬 이펙트 시간")]
    public float[] damageCircleEffectDurations = new float[3] { 1f, 1f, 1f };

    public Vector3 skillCenterOffset = Vector3.zero;

    [Header("원 스킬 경고 시간")]
    public float warningDelay = 1f;

    private List<GameObject> activeSkillObjects = new List<GameObject>();

    private Coroutine redWaveCoroutine;

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
        if (isSkillPlaying) return;

        if (enemyAnimation != null && playerTransform != null)
        {
            agent.SetDestination(playerTransform.position);
            bool isActuallyMoving = agent.isStopped == false && agent.velocity.sqrMagnitude > 0.01f;

            Vector2 moveDir = isActuallyMoving ? agent.velocity.normalized : (playerTransform.position - transform.position).normalized;
            enemyAnimation.PlayDirectionalMoveAnimation(moveDir);
            FlipSprite(moveDir.x);
        }
        else if (playerTransform == null)
        {
            enemyAnimation.PlayAnimation(BossAnimation.State.Idle);
        }

        skillTimer += Time.deltaTime;

        if (skillTimer >= skillInterval && !isSkillPlaying)
        {
            skillTimer = 0f;

            do
            {
                currentSkillIndex = Random.Range(0, 3);
            } while (currentSkillIndex == previousSkillIndex);

            previousSkillIndex = currentSkillIndex;

            UseRandomSkill();
        }
    }

    private IEnumerator RedScreenWaveEffect(float duration = 0.5f, float maxScale = 3f, int ringCount = 3, float delayBetweenRings = 0.1f)
    {
        AudioManager.Instance?.PlayBossSwordSound(2f);

        StartCoroutine(RedScreenFlash(duration));

        for (int i = 0; i < ringCount; i++)
        {
            StartCoroutine(SingleRingEffect(duration, maxScale));
            yield return new WaitForSeconds(delayBetweenRings);
        }

        yield return new WaitForSeconds(duration + delayBetweenRings * ringCount);
    }

    private IEnumerator RedScreenFlash(float duration = 0.3f)
    {
        GameObject redScreen = new GameObject("RedScreenFlash");
        redScreen.transform.position = Camera.main.transform.position + Vector3.forward * 1f;

        float camHeight = 2f * Camera.main.orthographicSize * 100f;
        float camWidth = camHeight * Camera.main.aspect;
        redScreen.transform.localScale = new Vector3(camWidth, camHeight, 1f);

        SpriteRenderer sr = redScreen.AddComponent<SpriteRenderer>();
        sr.sprite = TextureToSprite(Texture2D.whiteTexture);
        sr.color = new Color(1f, 0f, 0f, 0f);
        sr.sortingOrder = 500;

        Sequence seq = DOTween.Sequence();
        seq.Append(sr.DOFade(0.6f, duration / 2f));
        seq.Append(sr.DOFade(0f, duration / 2f));
        seq.OnComplete(() => Destroy(redScreen));

        yield return seq.WaitForCompletion();
    }

    private Sprite TextureToSprite(Texture2D tex)
    {
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
    }

    private IEnumerator SingleRingEffect(float duration, float maxScale)
    {
        GameObject redEffect = new GameObject("RedScreenWave");
        redEffect.transform.position = transform.position;
        redEffect.transform.localScale = Vector3.zero;

        SpriteRenderer sr = redEffect.AddComponent<SpriteRenderer>();
        sr.sprite = CreateRingSprite(256, Color.red, 8);
        sr.color = new Color(1f, 0f, 0f, 0.6f);
        sr.sortingOrder = 100;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            redEffect.transform.localScale = Vector3.one * Mathf.Lerp(0.5f, maxScale, t);
            sr.color = new Color(1f, 0f, 0f, Mathf.Lerp(0.6f, 0f, t));

            yield return null;
        }

        Destroy(redEffect);
    }

    private Sprite CreateRingSprite(int size, Color color, int ringThickness)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        float rOuter = size / 2f;
        float rInner = rOuter - ringThickness;
        Vector2 center = new Vector2(rOuter, rOuter);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 pos = new Vector2(x, y);
                float dist = Vector2.Distance(pos, center);

                if (dist <= rOuter && dist >= rInner)
                    tex.SetPixel(x, y, color);
                else
                    tex.SetPixel(x, y, new Color(0, 0, 0, 0));
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private void UseRandomSkill()
    {
        if (isSkillPlaying) return;
        isSkillPlaying = true;
        if (agent != null) agent.isStopped = true;

        if (redWaveCoroutine == null)
            redWaveCoroutine = StartCoroutine(RedScreenWaveEffectAndSkill());
    }

    private IEnumerator RedScreenWaveEffectAndSkill()
    {
        yield return StartCoroutine(RedScreenWaveEffect());

        switch (currentSkillIndex)
        {
            case 0:
                fireballCoroutine = StartCoroutine(FireballSkill());
                break;
            case 1:
                StartCoroutine(FirePillarCircleSkill());
                break;
            case 2:
                StartCoroutine(DoubleSwordSkill());
                break;
        }

        redWaveCoroutine = null;
    }

    // ────────── 스킬 1: 파이어볼 360 + 타겟 반복 ──────────
    private IEnumerator FireballSkill()
    {
        isInFireballSkill = true;      // ▶ 인터럽트 대상 시작
        bossHitCount = 0;              // ▶ 카운터 리셋

        yield return new WaitForSeconds(1f);
        enemyAnimation?.PlayAnimation(BossAnimation.State.Skill1Fireball);

        if (skill1Prefab != null && activeSkill1Object == null)
            activeSkill1Object = Instantiate(skill1Prefab, transform.position + Vector3.up * 1f, Quaternion.identity);

        while (bossHitCount < 6)
        {
            // 매 사이클마다 보스 현재 위치 기준으로 경고/발사 (이탈 방지)
            Vector2 origin = transform.position;

            if (playerTransform != null)
            {
                Vector2 dirToPlayer = (playerTransform.position - transform.position).normalized;
                FlipSprite(dirToPlayer.x);
            }

            if (activeSkill1Object != null)
                activeSkill1Object.transform.position = transform.position + Vector3.up * 1f;

            enemyAnimation?.PlayAnimation(BossAnimation.State.Skill1Fireball);

            // 1) 360도 경고 + 발사
            yield return StartCoroutine(FireballWarningAndCircle(origin, fireballCount360));
            AudioManager.Instance?.PlayBossSkill1Sound(2f);

            // 2) 타겟 경고 + 발사
            yield return StartCoroutine(FireballWarningToPlayer(origin));
            AudioManager.Instance?.PlayBossSkill1Sound(2f);

            // 라운드 간 텀
            yield return new WaitForSeconds(fireballRepeatInterval);
        }

        // 여기 도달 = bossHitCount >= 6 (루프 탈출)
        yield return StartCoroutine(SkillEndDelay());

        if (activeSkill1Object != null)
        {
            Destroy(activeSkill1Object);
            activeSkill1Object = null;
        }

        fireballCoroutine = null;
        isInFireballSkill = false;     // ▶ 정상 종료
    }

    private IEnumerator FireballWarningToPlayer(Vector2 origin)
    {
        if (playerTransform == null) yield break;

        Vector2 dir = (playerTransform.position - transform.position).normalized;
        Vector2 warnPos = origin + dir * fireballSpawnRadius;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        GameObject warning = null;
        if (fireballWarningPrefab != null)
        {
            warning = Instantiate(fireballWarningPrefab, warnPos, Quaternion.Euler(0f, 0f, angle));
            activeSkillObjects.Add(warning);
        }

        float elapsed = 0f;
        while (elapsed < warningDuration)
        {
            if (warning != null)
            {
                warnPos = origin + dir * fireballSpawnRadius;
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
        float angleStep = Mathf.Max(1, 360f / Mathf.Max(1, count));

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

        // 360도 발사
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

    // ────────── 스킬 2: 불기둥 원형 ──────────
    private IEnumerator FirePillarCircleSkill()
    {
        yield return new WaitForSeconds(1f);
        Vector3 center = transform.position + skillCenterOffset;
        enemyAnimation?.PlayAnimationAndPauseLastFrame(BossAnimation.State.Skill2Circle);
        AudioManager.Instance?.PlayBossSkill3Sound(2f);

        float skillAnimDur = Mathf.Max(0.05f, enemyAnimation.GetNonLoopDuration(BossAnimation.State.Skill2Circle));
        yield return new WaitForSeconds(skillAnimDur);

        float[] radii = new float[3] { 3f, 5f, 8f };

        for (int i = 0; i < radii.Length; i++)
        {
            float radius = radii[i];
            int pillarCount = Mathf.RoundToInt(radius * 3f);
            float angleStep = 360f / Mathf.Max(1, pillarCount);

            List<GameObject> pillars = new List<GameObject>();

            // 1) 경고
            List<GameObject> warnings = new List<GameObject>();
            for (int j = 0; j < pillarCount; j++)
            {
                float angle = j * angleStep * Mathf.Deg2Rad;
                Vector3 warnPos = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;

                if (warningCirclePrefabs.Length > 0 && warningCirclePrefabs[0] != null)
                {
                    GameObject warning = Instantiate(warningCirclePrefabs[0], warnPos, Quaternion.identity);
                    warnings.Add(warning);
                    activeSkillObjects.Add(warning);
                }
            }

            yield return new WaitForSeconds(warningDelay);

            foreach (var w in warnings)
            {
                if (w != null)
                {
                    activeSkillObjects.Remove(w);
                    Destroy(w);
                }
            }

            // 2) 소환
            for (int j = 0; j < pillarCount; j++)
            {
                float angle = j * angleStep * Mathf.Deg2Rad;
                Vector3 targetPos = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;

                if (damageCirclePrefabs.Length > 0 && damageCirclePrefabs[0] != null)
                {
                    GameObject pillar = Instantiate(damageCirclePrefabs[0], targetPos, Quaternion.identity);

                    float pillarScale = 7f;
                    pillar.transform.localScale = new Vector3(pillarScale, 0f, pillarScale);

                    Sequence seq = DOTween.Sequence();
                    seq.Append(pillar.transform.DOMoveY(targetPos.y + 0.2f, 0.65f).SetEase(Ease.OutCubic));
                    seq.Join(pillar.transform.DOScaleY(pillarScale * 1.1f, 0.65f).SetEase(Ease.OutBack));
                    seq.Append(pillar.transform.DOMoveY(targetPos.y, 0.2f).SetEase(Ease.InOutSine));
                    seq.Join(pillar.transform.DOScaleY(pillarScale, 0.2f));

                    activeSkillObjects.Add(pillar);
                    pillars.Add(pillar);
                }

                if (j % 3 == 1)
                    AudioManager.Instance?.PlayBossSkill3FireSound(2f);

                yield return new WaitForSeconds(0.02f);
            }

            yield return new WaitForSeconds(0.3f);

            foreach (var p in pillars)
            {
                if (p != null)
                {
                    Sequence seq = DOTween.Sequence();
                    seq.Append(p.transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack));
                    seq.OnComplete(() => {
                        if (p != null)
                        {
                            activeSkillObjects.Remove(p);
                            Destroy(p);
                        }
                    });
                }
            }

            yield return new WaitForSeconds(0.3f);
        }

        enemyAnimation.PlayAnimation(BossAnimation.State.Idle);
        yield return StartCoroutine(SkillEndDelay());
    }

    // ────────── 스킬 3: 대시 2회 ──────────
    private IEnumerator DoubleSwordSkill()
    {
        yield return new WaitForSeconds(1f);
        GameObject player = playerTransform != null ? playerTransform.gameObject : GameObject.FindWithTag("Player");
        if (player == null)
        {
            yield return StartCoroutine(SkillEndDelay());
            yield break;
        }

        Vector3 originalPos = transform.position;
        float playerDirX = Mathf.Sign(player.transform.position.x - transform.position.x);
        FlipSprite(-playerDirX);

        for (int j = 0; j < 2; j++)
        {
            float sideOffset = 2.5f;
            bool goRight = Random.value > 0.5f;

            float dashDirX = goRight ? 1f : -1f;
            FlipSprite(-dashDirX);

            float targetX = player.transform.position.x + (goRight ? sideOffset : -sideOffset);
            Vector3 sideTarget = new Vector3(targetX, player.transform.position.y, transform.position.z);

            enemyAnimation.PlayAnimation(BossAnimation.State.Skill3DashStart);
            float dashStartDur = Mathf.Max(0.05f, enemyAnimation.GetNonLoopDuration(BossAnimation.State.Skill3DashStart));
            yield return new WaitForSeconds(dashStartDur);

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

            Vector3 slashDir = new Vector3(-dashDirX, 0f, 0f);
            Vector3 slashPos = transform.position + slashDir * (swordSpawnDistance + 1.0f);
            float angle = -dashDirX > 0 ? 0f : 180f;
            Quaternion rot = Quaternion.Euler(0f, 0f, angle);

            AudioManager.Instance?.PlayBossSkill2Sound(2f);

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

        if (activeSkill1Object != null)
        {
            Destroy(activeSkill1Object);
            activeSkill1Object = null;
        }

        GameObject[] warnings = GameObject.FindGameObjectsWithTag("FireballWarning");
        foreach (var w in warnings)
            if (w != null) Destroy(w);

        if (agent != null) agent.isStopped = true;

        enemyAnimation?.PlayAnimation(BossAnimation.State.Idle);

        StopAllCoroutines();

        fireballCoroutine = null;
        isSkillPlaying = false;
        isInFireballSkill = false;
        isInterrupting = false;
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

    public void OnBossTakeDamage()
    {
        bossHitCount++;

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
                    seq.Append(sr.DOColor(Color.cyan, 0.3f));
                    seq.Join(hitChild.DOScale(0.5f, 0.15f).SetLoops(2, LoopType.Yoyo));
                }
            }
        }

        // ▶ 파이어볼 스킬 진행중일 때만 인터럽트, 중복 방지
        if (!isInFireballSkill || isInterrupting) return;

        if (bossHitCount >= 5)
        {
            isInterrupting = true; // 중복 트리거 방지

            if (fireballCoroutine != null)
            {
                StopCoroutine(fireballCoroutine);
                fireballCoroutine = null;
            }

            // 연출 오브젝트/발사체/경고 정리
            if (activeSkill1Object != null)
            {
                Destroy(activeSkill1Object);
                activeSkill1Object = null;
            }
            ClearAllSkillObjects();

            // 상태 리셋
            bossHitCount = 0;
            isInFireballSkill = false;

            // 스킬 종료 루틴 호출(쿨타임/이동 재개)
            StartCoroutine(SkillEndDelay());

            // 약간의 완충 후 인터럽트 플래그 해제
            StartCoroutine(ResetInterruptFlagSoon());
        }
    }

    private IEnumerator ResetInterruptFlagSoon()
    {
        yield return null;            // 다음 프레임까지 보장
        isInterrupting = false;
    }
}
