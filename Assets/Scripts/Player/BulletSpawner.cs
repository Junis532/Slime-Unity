using DG.Tweening;
using System.Collections;
using UnityEngine.UI;
using UnityEngine;

public class BulletSpawner : MonoBehaviour
{
    [Header("🔫 총알 프리팹")]
    public GameObject bulletPrefab;

    [Header("🔥 Fireball 프리팹")]
    public GameObject fireballPrefab;

    [Header("🟩 Fireball 체크박스 (임시용)")]
    public bool useFireball = false;

    [Header("🕒 전체 생성 간격")]
    public float spawnInterval = 1f;

    [Header("🌟 화살 발사 연출용 효과 활 프리팹")]
    public GameObject effectBowPrefab;

    [Header("↩️ 플레이어로부터 활의 거리")]
    public float bowDistance = 1.0f;

    [Header("🎯 플레이어로부터 화살의 거리")]
    public float arrowDistanceFromPlayer = 1.2f;

    [Header("플레이어 공격 게이지 부모")]
    public Image attackGaugeImageParrent;
    public Image attackGaugeImage;

    [Header("게이지가 위치할 Y 오프셋 (플레이어 기준)")]
    public float gaugeYOffset = -0.1f;


    private float timer;
    private GameObject bowInstance;
    private GameObject effectBowInstance;
    private Transform playerTransform;
    private bool isBowActive = true;
    private BulletAI lastArrowAI = null;
    private bool arrowIsFlying = false;
    private float arrowAngle = 0f;
    private Vector3 currentBowPosition;
    private Vector3 currentArrowPosition;

    private Vector3 previousPlayerPosition;
    private float playerStillThreshold = 0.01f;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            previousPlayerPosition = playerTransform.position;
        }

        if (effectBowPrefab != null)
        {
            effectBowInstance = Instantiate(effectBowPrefab);
            effectBowInstance.SetActive(false);
        }
    }

    void Update()
    {
        if (!GameManager.Instance.IsGame()) return;
        if (playerTransform == null || bulletPrefab == null) return;

        // 🟢 게이지 UI 위치 따라다니게
        if (attackGaugeImageParrent != null)
        {
            Vector3 gaugePos = playerTransform.position + new Vector3(0, gaugeYOffset, 0);
            attackGaugeImageParrent.transform.position = Camera.main.WorldToScreenPoint(gaugePos);

            // 🔴 채워지는 방식
            attackGaugeImage.fillAmount = timer / spawnInterval;
        }

        // 플레이어 정지 여부 판단
        bool isPlayerStill = Vector3.Distance(previousPlayerPosition, playerTransform.position) < playerStillThreshold;
        previousPlayerPosition = playerTransform.position;

        if (!isPlayerStill)
        {
            timer = 0f;
        }


        // 적 존재 여부 확인
        bool hasEnemy = false;
        string[] enemyTags = { "Enemy", "DashEnemy", "LongRangeEnemy", "PotionEnemy" };
        foreach (string tag in enemyTags)
        {
            if (GameObject.FindGameObjectWithTag(tag) != null)
            {
                hasEnemy = true;
                break;
            }
        }
        if (!hasEnemy) return;

        // 가장 가까운 적 방향 계산
        Transform closestEnemy = FindClosestEnemy(playerTransform.position);
        Vector3 playerToEnemyDir = Vector3.right;
        if (closestEnemy != null)
        {
            playerToEnemyDir = (closestEnemy.position - playerTransform.position).normalized;
        }

        currentBowPosition = playerTransform.position + playerToEnemyDir * bowDistance;
        currentArrowPosition = playerTransform.position + playerToEnemyDir * arrowDistanceFromPlayer;
        arrowAngle = Mathf.Atan2(playerToEnemyDir.y, playerToEnemyDir.x) * Mathf.Rad2Deg;

        SyncBowAndArrowToPlayer();
        SyncBowAndArrowDirection(arrowAngle);

        if (isPlayerStill)
        {
            timer += Time.deltaTime;
            if (timer >= spawnInterval)
            {
                FireArrow();
            }
        }
    }

    private int shotCount = 0;

    private void FireArrow()
    {
        arrowIsFlying = false;

        if (bowInstance != null)
        {
            bowInstance.transform.DOKill();
            bowInstance.SetActive(false);
            isBowActive = false;
        }

        if (effectBowInstance != null)
        {
            effectBowInstance.SetActive(true);
            effectBowInstance.transform.position = currentBowPosition;
            effectBowInstance.transform.rotation = Quaternion.Euler(0, 0, arrowAngle - 180f);
            effectBowInstance.transform.localScale = new Vector3(0.4f, 0.4f, 1f);
        }

        // 3번째에 fireball 쏘기
        GameObject bulletToFire = bulletPrefab;
        if (useFireball && shotCount >= 6 && fireballPrefab != null)
        {
            bulletToFire = fireballPrefab;
            shotCount = 0; // 리셋
        }
        else
        {
            shotCount++;
        }

        GameObject bullet = GameManager.Instance.poolManager.SpawnFromPool(
            bulletToFire.name, currentArrowPosition, Quaternion.Euler(0, 0, arrowAngle));

        lastArrowAI = bullet.GetComponent<BulletAI>();
        if (lastArrowAI != null)
        {
            lastArrowAI.InitializeBullet(currentArrowPosition, arrowAngle);
        }

        timer = 0f;
        StartCoroutine(ReleaseArrowAfterDelay(0.4f));
    }

    IEnumerator ReleaseArrowAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        arrowIsFlying = true;

        if (effectBowInstance != null)
        {
            effectBowInstance.SetActive(false);
        }

        if (bowInstance != null)
            bowInstance.SetActive(true);

        isBowActive = true;
    }

    void SyncBowAndArrowToPlayer()
    {
        if (!arrowIsFlying && playerTransform != null)
        {
            if (effectBowInstance != null && effectBowInstance.activeSelf)
                effectBowInstance.transform.position = currentBowPosition;

            if (lastArrowAI != null && lastArrowAI.isActiveAndEnabled)
                lastArrowAI.transform.position = currentArrowPosition;
        }
    }

    void SyncBowAndArrowDirection(float currentArrowAngle)
    {
        if (!arrowIsFlying && effectBowInstance != null && effectBowInstance.activeSelf && lastArrowAI != null)
        {
            effectBowInstance.transform.rotation = Quaternion.Euler(0, 0, currentArrowAngle - 180f);
            lastArrowAI.SyncSetRotation(currentArrowAngle);
        }
    }

    Transform FindClosestEnemy(Vector3 fromPos)
    {
        string[] enemyTags = { "Enemy", "DashEnemy", "LongRangeEnemy", "PotionEnemy" };
        float closestDist = Mathf.Infinity;
        Transform closest = null;

        foreach (string tag in enemyTags)
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject enemy in enemies)
            {
                float dist = Vector3.Distance(fromPos, enemy.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = enemy.transform;
                }
            }
        }

        return closest;
    }
}
