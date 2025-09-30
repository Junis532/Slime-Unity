using DG.Tweening;
using UnityEngine;

public class BulletSpawner : MonoBehaviour
{
    [Header("총알 프리팹")]
    public GameObject bulletPrefab;

    [Header("Fireball 프리팹")]
    public GameObject fireballPrefab;

    [Header("Fireball 사용 여부")]
    public bool useFireball = false;
    public float fireballDotMultiplier = 0.5f;

    [Header("슬로우 화살 스킬 활성화")]
    public bool slowSkillActive = false;

    [Header("발사 간격")]
    public float spawnInterval = 1f;

    [Header("공격 속도 배율 (1 = 기본, 2 = 2배 빠름)")]
    public float attackSpeedMultiplier = 1f;

    [Header("플레이어 기준 거리")]
    public float arrowDistanceFromPlayer = 0f;

    [Header("멈춘 후 대기 시간")]
    public float stopDelay = 0.1f;

    [Header("한 번에 발사할 총알 개수")]
    public int bulletsPerShot = 3;

    [Header("총알 간 각도 퍼짐 (도 단위)")]
    public float spreadAngle = 10f;

    [Header("타겟 표시 프리팹")]
    public GameObject targetMarkerPrefab;

    [Header("타겟 마커 위치 오프셋")]
    public Vector3 targetMarkerOffset = new Vector3(0, 0, 0);

    [Header("두 번째 타겟 표시 프리팹")]
    public GameObject secondTargetMarkerPrefab;

    [Header("두 번째 타겟 마커 위치 오프셋")]
    public Vector3 secondTargetMarkerOffset = new Vector3(0, 1f, 0);

    //[Header("Bow 이펙트 프리팹")]
    //public GameObject bowEffectPrefab;

    //[Header("Bow 이펙트 지속 시간")]
    //public float bowEffectDuration = 0.2f;

    //[Header("Bow 거리")]
    //public float bowDistance = 0.7f; // 플레이어에서 이 거리에 Bow 이펙트가 생성됨

    private GameObject secondMarker;
    private GameObject currentMarker;

    private float stopDelayTimer = 0f;
    private float spawnTimer = 0f;
    private bool firedAfterStop = false;
    private PlayerController playerController;

    private int fireCount = 0;

    void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            playerController = playerObj.GetComponent<PlayerController>();
    }

    void Update()
    {
        if (playerController == null || bulletPrefab == null) return;

        Transform closestEnemy = FindClosestEnemy();

        UpdateMarkers(closestEnemy);

        bool isStill = playerController.inputVec.magnitude < 0.05f;
        float actualSpawnInterval = spawnInterval / Mathf.Max(0.1f, attackSpeedMultiplier);

        if (isStill)
        {
            stopDelayTimer += Time.deltaTime;

            if (!firedAfterStop && stopDelayTimer >= stopDelay)
            {
                FireArrow(closestEnemy);
                spawnTimer = 0f;
                firedAfterStop = true;
            }

            if (firedAfterStop)
            {
                spawnTimer += Time.deltaTime;
                if (spawnTimer >= actualSpawnInterval)
                {
                    FireArrow(closestEnemy);
                    spawnTimer = 0f;
                }
            }
        }
        else
        {
            stopDelayTimer = 0f;
            spawnTimer = 0f;
            firedAfterStop = false;
        }
    }

    private void UpdateMarkers(Transform closestEnemy)
    {
        // 첫 번째 마커
        if (targetMarkerPrefab != null)
        {
            if (closestEnemy != null)
            {
                Vector3 markerPos = closestEnemy.position + targetMarkerOffset;
                if (currentMarker == null)
                    currentMarker = Instantiate(targetMarkerPrefab, markerPos, Quaternion.identity);
                else
                    currentMarker.transform.position = markerPos;
            }
            else if (currentMarker != null)
            {
                Destroy(currentMarker);
                currentMarker = null;
            }
        }

        // 두 번째 마커
        if (secondTargetMarkerPrefab != null)
        {
            if (closestEnemy != null)
            {
                Vector3 markerPos = closestEnemy.position + secondTargetMarkerOffset;
                if (secondMarker == null)
                    secondMarker = Instantiate(secondTargetMarkerPrefab, markerPos, Quaternion.Euler(0, 0, -90));
                else
                    secondMarker.transform.position = markerPos;
            }
            else if (secondMarker != null)
            {
                Destroy(secondMarker);
                secondMarker = null;
            }
        }
    }

    private Transform FindClosestEnemy()
    {
        string[] enemyTags = { "Enemy", "DashEnemy", "LongRangeEnemy", "PotionEnemy" };
        Transform closest = null;
        float closestDist = Mathf.Infinity;

        foreach (string tag in enemyTags)
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag(tag);
            foreach (var enemy in enemies)
            {
                float dist = Vector3.Distance(playerController.transform.position, enemy.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = enemy.transform;
                }
            }
        }

        return closest;
    }

    private void FireArrow(Transform centerTarget)
    {
        if (centerTarget == null) return;

        AudioManager.Instance?.PlayArrowSound(1.5f); // 🔊 커스텀 1.5배

        // 🔥 플레이어 강한 찌부 효과
        if (playerController != null)
        {
            Transform player = playerController.transform;

            player.DOKill(); // 기존 트윈 정리
            Sequence seq = DOTween.Sequence();

            // 1) 강하게 찌부
            seq.Append(player.DOScale(
                new Vector3(4.3f * 1.4f, 4.3f * 0.6f, player.localScale.z),
                0.08f
            ).SetEase(Ease.OutQuad));

            // 2) 크기를 (4.3, 4.3)으로 복귀
            seq.Append(player.DOScale(
                new Vector3(4.3f, 4.3f, player.localScale.z),
                0.18f
            ).SetEase(Ease.OutBack));
        }


        fireCount++;
        bool isFireballShot = (fireCount % 7 == 0) && (fireballPrefab != null) && useFireball;

        Vector3 dirToTarget = (centerTarget.position - playerController.transform.position).normalized;
        float centerAngle = Mathf.Atan2(dirToTarget.y, dirToTarget.x) * Mathf.Rad2Deg;

        // 플레이어 방향 반전
        FlipPlayer(dirToTarget);

        int count = Mathf.Max(1, bulletsPerShot);
        float totalSpread = spreadAngle * (count - 1);
        float startOffset = -totalSpread / 2f;

        for (int i = 0; i < count; i++)
        {
            bool isCenter = (i == count / 2);
            bool isFireballThisShot = isCenter && isFireballShot;
            GameObject bulletPrefabToUse = isFireballThisShot ? fireballPrefab : bulletPrefab;

            float angle = centerAngle + startOffset + i * spreadAngle;
            Vector3 dir = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0);
            Vector3 spawnPos = playerController.transform.position + dir * arrowDistanceFromPlayer;

            GameObject bullet = GameManager.Instance.poolManager.SpawnFromPool(
                bulletPrefabToUse.name, spawnPos, Quaternion.identity
            );

            if (bullet != null)
            {
                BulletAI bulletAI = bullet.GetComponent<BulletAI>();
                if (bulletAI != null)
                {
                    bulletAI.ResetBullet();
                    bulletAI.InitializeBullet(spawnPos, angle, isCenter);
                }
            }

            if (isFireballThisShot)
            {
                FireballAI fireballAI = bullet.GetComponent<FireballAI>();
                if (fireballAI != null)
                    fireballAI.InitializeBullet(spawnPos, angle);

                SetAlphaRecursive(bullet, 1f);
            }
            else
            {
                BulletAI bulletAI = bullet.GetComponent<BulletAI>();
                if (bulletAI != null)
                    bulletAI.InitializeBullet(spawnPos, angle, isCenter);
            }
        }
    }


    private void FlipPlayer(Vector3 dir)
    {
        SpriteRenderer sr = playerController.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            // 오른쪽 기준이면 flipX false, 왼쪽이면 flipX true
            sr.flipX = dir.x < 0;
        }
    }


    //private void SpawnBowEffect(Vector3 dirToTarget)
    //{
    //    if (bowEffectPrefab == null) return;

    //    float angle = Mathf.Atan2(dirToTarget.y, dirToTarget.x) * Mathf.Rad2Deg + 180f;
    //    Vector3 offset = dirToTarget.normalized * bowDistance;

    //    GameObject bowEffect = Instantiate(bowEffectPrefab, Vector3.zero, Quaternion.Euler(0, 0, angle));

    //    BowEffectFollow follow = bowEffect.AddComponent<BowEffectFollow>();
    //    follow.offset = offset;
    //    follow.duration = bowEffectDuration;
    //}

    private void SetAlphaRecursive(GameObject obj, float alpha)
    {
        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }

        foreach (Transform child in obj.transform)
            SetAlphaRecursive(child.gameObject, alpha);
    }
}
