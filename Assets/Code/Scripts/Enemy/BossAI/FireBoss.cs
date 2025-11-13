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
    private bool isTelegraphing = false;                 // 전조(빨간 파동) 디바운스
    private bool skillPhaseRunning = false;              // 전조→스킬 한 사이클 보장
    private int currentSkillIndex;
    private int previousSkillIndex = -1;

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

    // 파이어볼 패턴의 ‘무한 지속’ 방지 옵션 (공격 안 맞아도 끝내기)
    [Header("파이어볼 패턴 종료 안전장치")]
    [Min(1)] public int fireballMaxCycles = 3;           // 360+타겟 반복 최대 라운드
    [Min(0.5f)] public float fireballSkillTimeout = 10f; // 최대 지속 시간(초)

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

    // ▼▼ 원형 불기둥(스킬2) 확대/가속 튜닝 ▼▼
    [Header("원 스킬 튜닝")]
    [Min(0.1f)] public float circleRadiusMultiplier = 1.25f;
    [Min(0.1f)] public float circleSpeedMultiplier = 1.6f;
    public float pillarBaseScale = 9f;
    public float pillarRiseYOffset = 0.25f;
    public float pillarDensity = 3.5f;

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
        if (isSkillPlaying || isTelegraphing || skillPhaseRunning) return;

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

        if (skillTimer >= skillInterval)
        {
            skillTimer = 0f;

            // 바로 이전 스킬만 회피 (3종이면 통계적으로 모두 등장)
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
        GameManager.Instance.audioManager.PlayBossSwordSound(2f);

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
        if (isSkillPlaying || isTelegraphing || skillPhaseRunning) return;
        if (redWaveCoroutine != null) return;

        isSkillPlaying = true;
        isTelegraphing = true;
        skillPhaseRunning = true;           // 사이클 시작
        if (agent != null) agent.isStopped = true;

        redWaveCoroutine = StartCoroutine(RedScreenWaveEffectAndSkill());
    }

    private IEnumerator RedScreenWaveEffectAndSkill()
    {
        // 전조(기존 연출 유지: 여러 링 "짜르르륵")
        yield return StartCoroutine(RedScreenWaveEffect());
        isTelegraphing = false; // 전조 끝

        // ⬇ 반드시 스킬 1회 실행을 보장 (패턴 누락 방지)
        yield return StartCoroutine(StartSkillByIndex(currentSkillIndex));

        // 정리
        redWaveCoroutine = null;
        skillPhaseRunning = false;
    }

    // 전조 후 선택된 스킬을 직렬로 '확실히' 1회 실행
    private IEnumerator StartSkillByIndex(int idx)
    {
        switch (idx)
        {
            case 0: yield return StartCoroutine(FireballSkill()); break;
            case 1: yield return StartCoroutine(FirePillarCircleSkill()); break;
            case 2: yield return StartCoroutine(DoubleSwordSkill()); break;
            default: yield return StartCoroutine(SkillEndDelay()); break;
        }
    }

    // ────────── 스킬 1: 파이어볼 360 + 타겟 반복 ──────────
    private IEnumerator FireballSkill()
    {
        float startTime = Time.time;
        int cycles = 0;

        yield return new WaitForSeconds(1f);
        enemyAnimation?.PlayAnimation(BossAnimation.State.Skill1Fireball);

        bossHitCount = 0;

        if (skill1Prefab != null && activeSkill1Object == null)
            activeSkill1Object = Instantiate(skill1Prefab, transform.position + Vector3.up * 1f, Quaternion.identity);

        // 공격을 못 맞춰도 무한 지속 방지: cycle/timeout 어느 하나에 도달하면 종료
        while (bossHitCount < 6 && cycles < fireballMaxCycles && (Time.time - startTime) < fireballSkillTimeout)
        {
            // 보스가 이동했을 수 있으니 매 사이클 기준점 갱신
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
            GameManager.Instance.audioManager.PlayBossSkill1Sound(2f);

            // 2) 플레이어 타겟 경고 + 발사
            yield return StartCoroutine(FireballWarningToPlayer(origin));
            GameManager.Instance.audioManager.PlayBossSkill1Sound(2f);

            cycles++;
            yield return new WaitForSeconds(fireballRepeatInterval);
        }

        // 정리
        if (activeSkill1Object != null)
        {
            Destroy(activeSkill1Object);
            activeSkill1Object = null;
        }

        fireballCoroutine = null;
        yield return StartCoroutine(SkillEndDelay());
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
        float angleStep = Mathf.Max(1, 360f / Mathf.Max(1, count)); // 안전

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

        // 발사 직전 기준점 재샘플(전조 중 이동 보정)
        Vector2 fireOrigin = transform.position;

        for (int i = 0; i < count; i++)
        {
            float angle = i * angleStep - 90f;

            if (fireball360Prefab != null)
            {
                GameObject fireball = Instantiate(fireball360Prefab, fireOrigin, Quaternion.Euler(0f, 0f, angle));
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

    // ────────── 스킬 2: 불기둥 원형 (확대 & 고속 버전) ──────────
    private IEnumerator FirePillarCircleSkill()
    {
        yield return new WaitForSeconds(1f / circleSpeedMultiplier);

        Vector3 center = transform.position + skillCenterOffset;
        enemyAnimation?.PlayAnimationAndPauseLastFrame(BossAnimation.State.Skill2Circle);
        GameManager.Instance.audioManager.PlayBossSkill3Sound(2f);

        float skillAnimDur = Mathf.Max(0.03f, enemyAnimation.GetNonLoopDuration(BossAnimation.State.Skill2Circle) / circleSpeedMultiplier);
        yield return new WaitForSeconds(skillAnimDur);

        float[] baseRadii = new float[3] { 3f, 5f, 8f };
        for (int i = 0; i < baseRadii.Length; i++)
            baseRadii[i] *= circleRadiusMultiplier;

        float warnDelay = Mathf.Max(0.15f, warningDelay / circleSpeedMultiplier);
        float riseTime = 0.65f / circleSpeedMultiplier;
        float settleTime = 0.18f / circleSpeedMultiplier;
        float holdTime = 0.18f / circleSpeedMultiplier;
        float shrinkTime = 0.35f / circleSpeedMultiplier;
        float spawnStep = Mathf.Max(0.005f, 0.02f / circleSpeedMultiplier);

        for (int i = 0; i < baseRadii.Length; i++)
        {
            float radius = baseRadii[i];

            int pillarCount = Mathf.RoundToInt(Mathf.Max(1f, radius * pillarDensity));
            float angleStep = 360f / pillarCount;

            var pillars = new List<GameObject>();

            // 1) 경고 원 표시
            var warnings = new List<GameObject>();
            for (int j = 0; j < pillarCount; j++)
            {
                float angleRad = j * angleStep * Mathf.Deg2Rad;
                Vector3 warnPos = center + new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0f) * radius;

                if (warningCirclePrefabs.Length > 0 && warningCirclePrefabs[0] != null)
                {
                    GameObject warning = Instantiate(warningCirclePrefabs[0], warnPos, Quaternion.identity);
                    warnings.Add(warning);
                    activeSkillObjects.Add(warning);
                }
            }

            yield return new WaitForSeconds(warnDelay);

            foreach (var w in warnings)
            {
                if (w != null)
                {
                    activeSkillObjects.Remove(w);
                    Destroy(w);
                }
            }

            // 2) 불기둥 소환
            for (int j = 0; j < pillarCount; j++)
            {
                float angleRad = j * angleStep * Mathf.Deg2Rad;
                Vector3 targetPos = center + new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0f) * radius;

                if (damageCirclePrefabs.Length > 0 && damageCirclePrefabs[0] != null)
                {
                    GameObject pillar = Instantiate(damageCirclePrefabs[0], targetPos, Quaternion.identity);

                    float s = pillarBaseScale;
                    pillar.transform.localScale = new Vector3(s, 0f, s);

                    Sequence seq = DOTween.Sequence();
                    seq.Append(pillar.transform.DOMoveY(targetPos.y + pillarRiseYOffset, riseTime).SetEase(Ease.OutCubic));
                    seq.Join(pillar.transform.DOScaleY(s * 1.1f, riseTime).SetEase(Ease.OutBack));
                    seq.Append(pillar.transform.DOMoveY(targetPos.y, settleTime).SetEase(Ease.InOutSine));
                    seq.Join(pillar.transform.DOScaleY(s, settleTime));

                    activeSkillObjects.Add(pillar);
                    pillars.Add(pillar);
                }

                if (j % 2 == 1)
                    GameManager.Instance.audioManager.PlayBossSkill3FireSound(2f);

                yield return new WaitForSeconds(spawnStep);
            }

            // 3) 유지
            yield return new WaitForSeconds(holdTime);

            // 4) 수축 소멸
            foreach (var p in pillars)
            {
                if (p != null)
                {
                    Sequence seq = DOTween.Sequence();
                    seq.Append(p.transform.DOScale(Vector3.zero, shrinkTime).SetEase(Ease.InBack));
                    seq.OnComplete(() =>
                    {
                        if (p != null)
                        {
                            activeSkillObjects.Remove(p);
                            Destroy(p);
                        }
                    });
                }
            }

            yield return new WaitForSeconds(0.2f / circleSpeedMultiplier);
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

            GameManager.Instance.audioManager.PlayBossSkill2Sound(2f);

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
        {
            if (w != null) Destroy(w);
        }

        if (agent != null) agent.isStopped = true;

        enemyAnimation?.PlayAnimation(BossAnimation.State.Idle);

        StopAllCoroutines();

        fireballCoroutine = null;
        isSkillPlaying = false;
        isTelegraphing = false;
        skillPhaseRunning = false;
        redWaveCoroutine = null;
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

        // 히트로 인해 강제 종료 로직이 도는 경우는 기존과 동일
        if (bossHitCount >= 5)
        {
            if (fireballCoroutine != null && currentSkillIndex == 0)
            {
                int remainingLoops = 6 - bossHitCount;
                for (int i = 0; i < remainingLoops; i++)
                    GameManager.Instance.audioManager.PlayBossSkill1Sound(2f);

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