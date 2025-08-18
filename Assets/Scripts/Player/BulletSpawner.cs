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

        bool isStill = playerController.inputVec.magnitude < 0.05f;
        float actualSpawnInterval = spawnInterval / Mathf.Max(0.1f, attackSpeedMultiplier);

        if (isStill)
        {
            stopDelayTimer += Time.deltaTime;

            if (!firedAfterStop && stopDelayTimer >= stopDelay)
            {
                FireArrow();
                spawnTimer = 0f;
                firedAfterStop = true;
            }

            if (firedAfterStop)
            {
                spawnTimer += Time.deltaTime;
                if (spawnTimer >= actualSpawnInterval)
                {
                    FireArrow();
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

    private void FireArrow()
    {
        string[] enemyTags = { "Enemy", "DashEnemy", "LongRangeEnemy", "PotionEnemy" };

        // 가운데 총알 기준으로 가장 가까운 적 찾기
        Transform centerTarget = null;
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
                    centerTarget = enemy.transform;
                }
            }
        }

        if (centerTarget == null) return; // 적 없으면 발사 안 함

        fireCount++;
        bool isFireballShot = (fireCount % 7 == 0) && (fireballPrefab != null) && useFireball;

        // 가운데 총알 방향 (적 추적)
        Vector3 dirToTarget = (centerTarget.position - playerController.transform.position).normalized;
        float centerAngle = Mathf.Atan2(dirToTarget.y, dirToTarget.x) * Mathf.Rad2Deg;

        int count = Mathf.Max(1, bulletsPerShot);
        float totalSpread = spreadAngle * (count - 1);
        float startOffset = -totalSpread / 2f;

        for (int i = 0; i < count; i++)
        {
            bool isCenter = (i == count / 2); // 중앙 총알 판별
            bool isFireballThisShot = isCenter && isFireballShot; // 중앙+Fireball 여부
            GameObject bulletPrefabToUse = isFireballThisShot ? fireballPrefab : bulletPrefab;

            float angle = centerAngle + startOffset + i * spreadAngle;
            Vector3 dir = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0);
            Vector3 spawnPos = playerController.transform.position + dir * arrowDistanceFromPlayer;

            GameObject bullet = GameManager.Instance.poolManager.SpawnFromPool(
                bulletPrefabToUse.name, spawnPos, Quaternion.identity
            );

            if (isFireballThisShot)
            {
                FireballAI fireballAI = bullet.GetComponent<FireballAI>();
                if (fireballAI != null)
                {
                    fireballAI.InitializeBullet(spawnPos, angle); // Fireball 전용 초기화
                }

                // Fireball 알파값 1로 초기화 (부모 + 자식 VFX)
                SpriteRenderer sr = bullet.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    Color c = sr.color;
                    c.a = 1f;
                    sr.color = c;
                }

                for (int j = 0; j < bullet.transform.childCount; j++)
                {
                    SpriteRenderer childSr = bullet.transform.GetChild(j).GetComponent<SpriteRenderer>();
                    if (childSr != null)
                    {
                        Color cc = childSr.color;
                        cc.a = 1f;
                        childSr.color = cc;
                    }
                }
            }
            else
            {
                BulletAI bulletAI = bullet.GetComponent<BulletAI>();
                if (bulletAI != null)
                {
                    bulletAI.InitializeBullet(spawnPos, angle, isCenter); // 일반 화살 초기화
                }
            }
        }
    }
}
