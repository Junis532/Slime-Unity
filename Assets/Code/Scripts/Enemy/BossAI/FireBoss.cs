//using DG.Tweening;
//using UnityEngine;
//using UnityEngine.AI;
//using System.Collections;
//using System.Collections.Generic;

//[RequireComponent(typeof(NavMeshAgent))]
//public class FireBoss : EnemyBase
//{
//    private bool isLive = true;
//    private SpriteRenderer spriter;
//    private EnemyAnimation enemyAnimation;
//    private NavMeshAgent agent;

//    [Header("패턴 타이밍")]
//    public float skillInterval = 4f;
//    private float skillTimer = 0f;
//    private bool isSkillPlaying = false;
//    private int currentSkillIndex;

//    [Header("파이어볼")]
//    public GameObject fireballPrefab;
//    public GameObject fireballWarningPrefab;
//    public int numberOfFireballs = 36;
//    public float fireballSpawnRadius = 1.5f;
//    public float warningDuration = 1f;

//    [Header("검 스킬")]
//    public GameObject swordPrefab;
//    public float swordSpawnDistance = 1f;
//    public GameObject swordRangePrefab;
//    public float swordRangeDistance = 1.5f;

//    [Header("범위/원 스킬")]
//    public GameObject[] warningCirclePrefabs = new GameObject[3];
//    public GameObject[] damageCirclePrefabs = new GameObject[3];
//    public float[] circleScales = new float[3] { 10f, 7.5f, 5f };
//    public Vector3 skillCenterOffset = Vector3.zero;
//    public float warningDelay = 1f;

//    [Header("Dotween 잔상")]
//    public GameObject afterImagePrefab;
//    public float afterImageSpawnInterval = 0.05f;
//    public float afterImageFadeDuration = 0.3f;
//    public float afterImageLifeTime = 0.5f;

//    private Tween afterImageTweener;
//    private Sequence moveSequence;

//    private List<GameObject> activeSkillObjects = new List<GameObject>();

//    void Start()
//    {
//        spriter = GetComponent<SpriteRenderer>();
//        enemyAnimation = GetComponent<EnemyAnimation>();
//        agent = GetComponent<NavMeshAgent>();
//        agent.updateRotation = false;
//        agent.updateUpAxis = false;
//        agent.speed = speed;
//    }

//    void Update()
//    {
//        if (!isLive || isSkillPlaying) return;

//        skillTimer += Time.deltaTime;
//        if (skillTimer >= skillInterval)
//        {
//            skillTimer = 0f;
//            currentSkillIndex = Random.Range(0, 3);
//            UseRandomSkill();
//        }
//    }

//    private void UseRandomSkill()
//    {
//        isSkillPlaying = true;
//        agent.isStopped = true;

//        switch (currentSkillIndex)
//        {
//            case 0:
//                StartCoroutine(FireballSkill());
//                break;
//            case 1:
//                StartCoroutine(WarningCircleSkill());
//                break;
//            case 2:
//                StartCoroutine(DoubleSwordSkill());
//                break;
//        }
//    }

//    // ────────── 스킬 1: 파이어볼 ──────────
//    private IEnumerator FireballSkill()
//    {
//        Vector2 origin = transform.position;
//        yield return StartCoroutine(FireballWarningAndBurst(origin));
//        yield return StartCoroutine(SkillEndDelay());
//    }

//    private IEnumerator FireballWarningAndBurst(Vector2 origin)
//    {
//        GameObject player = GameObject.FindWithTag("Player");
//        if (player == null) yield break;

//        Vector2 directionToPlayer = (player.transform.position - transform.position).normalized;
//        Vector2 warnPos = origin + directionToPlayer * fireballSpawnRadius;

//        GameObject warning = null;
//        if (fireballWarningPrefab != null)
//        {
//            warning = Instantiate(fireballWarningPrefab, warnPos, Quaternion.identity);
//            activeSkillObjects.Add(warning);
//        }

//        float elapsed = 0f;
//        while (elapsed < warningDuration)
//        {
//            if (warning == null) break;
//            directionToPlayer = (player.transform.position - transform.position).normalized;
//            warnPos = (Vector2)transform.position + directionToPlayer * fireballSpawnRadius;
//            warning.transform.position = warnPos;

//            float angleDegrees = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg;
//            warning.transform.rotation = Quaternion.Euler(0f, 0f, angleDegrees);

//            elapsed += Time.deltaTime;
//            yield return null;
//        }

//        if (warning != null) Destroy(warning);
//        FireInDirection(origin, Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg - 90f);
//    }

//    private void FireInDirection(Vector2 origin, float angle)
//    {
//        GameObject fireball = Instantiate(fireballPrefab, origin, Quaternion.Euler(0f, 0f, angle));
//        Vector2 direction = new Vector2(Mathf.Cos((angle + 90f) * Mathf.Deg2Rad), Mathf.Sin((angle + 90f) * Mathf.Deg2Rad));
//        fireball.GetComponent<BossFireballProjectile>()?.Init(direction);
//        activeSkillObjects.Add(fireball);
//    }

//    // ────────── 스킬 2: 범위 원 ──────────
//    private IEnumerator WarningCircleSkill()
//    {
//        Vector3 center = transform.position + skillCenterOffset;
//        GameObject prevDamage = null;

//        for (int i = 0; i < 3; i++)
//        {
//            if (prevDamage != null)
//            {
//                Destroy(prevDamage);
//                prevDamage = null;
//            }

//            GameObject warning = Instantiate(warningCirclePrefabs[i], center, Quaternion.identity);
//            // 🔹 크기 배율 제거
//            // warning.transform.localScale = Vector3.one * circleScales[i];
//            activeSkillObjects.Add(warning);

//            yield return new WaitForSeconds(warningDelay);
//            Destroy(warning);

//            GameObject damage = Instantiate(damageCirclePrefabs[i], center, Quaternion.identity);
//            // 🔹 크기 배율 제거
//            // damage.transform.localScale = Vector3.one * circleScales[i];
//            activeSkillObjects.Add(damage);
//            prevDamage = damage;

//            yield return new WaitForSeconds(0.6f);
//        }

//        if (prevDamage != null) Destroy(prevDamage);
//        yield return StartCoroutine(SkillEndDelay());
//    }

//    // ────────── 스킬 3: 검 스킬 (Dotween 잔상 추가) ──────────
//    private IEnumerator DoubleSwordSkill()
//    {
//        GameObject player = GameObject.FindWithTag("Player");
//        if (player == null)
//        {
//            yield return StartCoroutine(SkillEndDelay());
//            yield break;
//        }

//        Vector3 originalPos = transform.position;

//        for (int j = 0; j < 2; j++)
//        {
//            float sideOffset = 2.5f;
//            float targetX = player.transform.position.x + (Random.value > 0.5f ? sideOffset : -sideOffset);
//            Vector3 sideTarget = new Vector3(targetX, player.transform.position.y, transform.position.z);

//            // 🔹 이동 시작 위치와 방향을 고정합니다.
//            Vector3 dashStartPos = transform.position;
//            Vector3 dashDirection = (sideTarget - dashStartPos).normalized;

//            // 🔹 잔상 생성용 시퀀스를 시작합니다.
//            float dashTime = j == 0 ? 0.2f : 0.25f;
//            SpawnAfterImagesWithTween(dashStartPos, dashDirection, dashTime);

//            // 🔹 보스 이동
//            moveSequence = DOTween.Sequence()
//                .Append(transform.DOMove(sideTarget, dashTime).SetEase(Ease.OutQuad));

//            yield return moveSequence.WaitForCompletion();

//            // 🔹 이동이 끝나면 잔상 생성 시퀀스 중지
//            StopAfterImagesWithTween();

//            Vector3 dir = (player.transform.position - transform.position).normalized;
//            FlipSprite(dir.x);

//            float minDistanceFromPlayer = 1.5f;
//            float swordForwardOffset = swordSpawnDistance + 1.0f;
//            Vector3 swordPos = transform.position + dir * swordForwardOffset;

//            float distanceToPlayer = Vector3.Distance(swordPos, player.transform.position);
//            if (distanceToPlayer < minDistanceFromPlayer)
//            {
//                swordPos += dir * (minDistanceFromPlayer - distanceToPlayer + 0.2f);
//            }

//            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
//            Quaternion rot = Quaternion.Euler(0f, 0f, angle);

//            if (swordRangePrefab != null)
//            {
//                GameObject range = Instantiate(swordRangePrefab, swordPos, rot);
//                range.transform.localScale = Vector3.one * swordRangeDistance;
//                Destroy(range, 0.25f);
//                yield return new WaitForSeconds(0.25f);
//            }

//            GameObject sword = Instantiate(swordPrefab, swordPos, rot);
//            activeSkillObjects.Add(sword);
//            Destroy(sword, 0.5f);

//            yield return new WaitForSeconds(0.35f);
//        }

//        // 🔹 원래 자리로 복귀 (잔상 없음)
//        float returnTime = 0.4f;
//        transform.DOMove(originalPos, returnTime).SetEase(Ease.InOutQuad);
//        yield return new WaitForSeconds(returnTime);

//        yield return StartCoroutine(SkillEndDelay());
//    }

//    private IEnumerator SkillEndDelay()
//    {
//        yield return new WaitForSeconds(1f);
//        isSkillPlaying = false;
//        agent.isStopped = false;
//    }

//    public void ClearAllSkillObjects()
//    {
//        moveSequence?.Kill();
//        StopAfterImagesWithTween();

//        foreach (var obj in activeSkillObjects)
//        {
//            if (obj != null) Destroy(obj);
//        }
//        activeSkillObjects.Clear();
//    }

//    public void SetDead()
//    {
//        isLive = false;
//        ClearAllSkillObjects();
//    }

//    // ────────── Dotween 잔상 관련 메서드 ──────────
//    private void FlipSprite(float dirX)
//    {
//        Vector3 scale = transform.localScale;
//        scale.x = Mathf.Abs(scale.x) * (dirX < 0 ? -1 : 1);
//        transform.localScale = scale;
//    }

//    private void SpawnAfterImagesWithTween(Vector3 startPos, Vector3 direction, float totalMoveTime)
//    {
//        StopAfterImagesWithTween();

//        afterImageTweener = DOTween.Sequence()
//            .Append(DOVirtual.DelayedCall(0, () => CreateAfterImageAt(startPos, direction, totalMoveTime), false))
//            .AppendInterval(afterImageSpawnInterval)
//            .SetLoops(-1, LoopType.Restart)
//            .SetId("AfterImageTweener");
//    }

//    private void StopAfterImagesWithTween()
//    {
//        DOTween.Kill("AfterImageTweener");
//    }

//    private void CreateAfterImageAt(Vector3 startPos, Vector3 direction, float totalMoveTime)
//    {
//        float totalDistance = Vector3.Distance(startPos, transform.position);
//        float moveProgress = totalDistance / Vector3.Distance(startPos, startPos + direction * totalMoveTime);
//        Vector3 afterImagePos = startPos + direction * totalDistance;

//        GameObject afterImage;
//        if (afterImagePrefab != null)
//        {
//            afterImage = Instantiate(afterImagePrefab, afterImagePos, transform.rotation);
//            afterImage.transform.localScale = transform.localScale;
//            SpriteRenderer afterImageSr = afterImage.GetComponent<SpriteRenderer>();
//            if (afterImageSr == null)
//            {
//                afterImageSr = afterImage.AddComponent<SpriteRenderer>();
//            }
//            SpriteRenderer enemySR = GetComponent<SpriteRenderer>();
//            if (enemySR != null)
//            {
//                afterImageSr.sprite = enemySR.sprite;
//                afterImageSr.flipX = enemySR.flipX;
//                afterImageSr.sortingLayerID = enemySR.sortingLayerID;
//                afterImageSr.sortingOrder = enemySR.sortingOrder - 1;
//            }
//        }
//        else
//        {
//            afterImage = new GameObject("AfterImage");
//            afterImage.transform.position = afterImagePos;
//            afterImage.transform.rotation = transform.rotation;
//            afterImage.transform.localScale = transform.localScale;

//            SpriteRenderer sr = afterImage.AddComponent<SpriteRenderer>();
//            SpriteRenderer enemySR = GetComponent<SpriteRenderer>();

//            if (enemySR != null)
//            {
//                sr.sprite = enemySR.sprite;
//                sr.flipX = enemySR.flipX;
//                sr.sortingLayerID = enemySR.sortingLayerID;
//                sr.sortingOrder = enemySR.sortingOrder - 1;
//            }
//        }

//        SpriteRenderer currentSr = afterImage.GetComponent<SpriteRenderer>();
//        if (currentSr != null)
//        {
//            Color c = currentSr.color;
//            c.a = 0.5f;
//            currentSr.color = c;

//            currentSr.DOFade(0f, afterImageFadeDuration)
//                .SetDelay(afterImageLifeTime - afterImageFadeDuration)
//                .OnComplete(() => Destroy(afterImage));
//        }
//        else
//        {
//            Destroy(afterImage, afterImageLifeTime);
//        }
//    }
//}

using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
public class FireBoss : EnemyBase
{
    private bool isLive = true;
    private SpriteRenderer spriter;
    private EnemyAnimation enemyAnimation;
    private NavMeshAgent agent;

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
        agent.speed = speed;
    }

    void Update()
    {
        if (!isLive || isSkillPlaying) return;

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
        agent.isStopped = true;

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
        Vector2 origin = transform.position;
        yield return StartCoroutine(FireballWarningAndBurst(origin));
        yield return StartCoroutine(SkillEndDelay());
    }

    private IEnumerator FireballWarningAndBurst(Vector2 origin)
    {
        GameObject player = GameObject.FindWithTag("Player");
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
    private IEnumerator WarningCircleSkill()
    {
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
        GameObject player = GameObject.FindWithTag("Player");
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
            FlipSprite(dir.x);

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
        while (returnElapsed < returnTime)
        {
            transform.position = Vector3.Lerp(returnStart, originalPos, returnElapsed / returnTime);
            returnElapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = originalPos;

        yield return StartCoroutine(SkillEndDelay());
    }

    private IEnumerator SkillEndDelay()
    {
        yield return new WaitForSeconds(1f);
        isSkillPlaying = false;
        agent.isStopped = false;
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
    }

    private void FlipSprite(float dirX)
    {
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (dirX < 0 ? -1 : 1);
        transform.localScale = scale;
    }
}
