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

    [Header("슬로우 화살")]
    public bool slowSkillActive = false;  // 슬로우 스킬 활성 여부

    [Header("🕒 전체 생성 간격")]
    public float spawnInterval = 1f;

    [Header("🌟 화살 발사 연출용 효과 활 프리팹")]
    public GameObject effectBowPrefab;

    [Header("↩️ 플레이어로부터 활의 거리")]
    public float bowDistance = 1.0f;

    [Header("🎯 플레이어로부터 화살의 거리")]
    public float arrowDistanceFromPlayer = 1.2f;

    [Header("🎯 타겟팅 표시 프리팹")]
    public GameObject targetingPrefab; // 기존 타겟팅 표시 프리팹

    [Header("🎯 추가 타겟팅 표시 프리팹")]
    public GameObject extraTargetingPrefab; // y + 2 위치에 띄울 다른 프리팹

    private float timer;
    private GameObject bowInstance;
    private GameObject effectBowInstance;
    private Transform playerTransform;
    private SpriteRenderer playerSpriteRenderer;
    private bool isBowActive = true;
    private BulletAI lastArrowAI = null;
    private bool arrowIsFlying = false;
    private float arrowAngle = 0f;
    private Vector3 currentBowPosition;
    private Vector3 currentArrowPosition;
    private Vector3 previousPlayerPosition;
    private float playerStillThreshold = 0.01f;

    // 타겟 표시 관련
    private Transform currentTarget;
    private GameObject targetingInstance;
    private GameObject extraTargetingInstance;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerSpriteRenderer = playerObj.GetComponent<SpriteRenderer>();
            previousPlayerPosition = playerTransform.position;
        }

        if (effectBowPrefab != null)
        {
            effectBowInstance = Instantiate(effectBowPrefab);
            effectBowInstance.SetActive(false);
        }
    }

    private bool canFire = true;

    void Update()
    {
        if (!GameManager.Instance.IsGame()) return;
        if (playerTransform == null || bulletPrefab == null) return;

        bool isPlayerStill = Vector3.Distance(previousPlayerPosition, playerTransform.position) < playerStillThreshold;
        previousPlayerPosition = playerTransform.position;

        if (!HasEnemyInScene())
        {
            ClearTargeting();
            return;
        }

        // 타겟 갱신
        UpdateTargeting();

        // 가장 가까운 적 방향 계산
        Vector3 playerToEnemyDir = Vector3.right;
        if (currentTarget != null)
        {
            playerToEnemyDir = (currentTarget.position - playerTransform.position).normalized;
        }

        // *** 플레이어 Flip 처리 (Idle 상태일 때만) ***
        if (IsPlayerIdle() && playerSpriteRenderer != null)
        {
            if (playerToEnemyDir.x > 0.01f)
                playerSpriteRenderer.flipX = false;  // 오른쪽 바라보기
            else if (playerToEnemyDir.x < -0.01f)
                playerSpriteRenderer.flipX = true;   // 왼쪽 바라보기
        }

        currentBowPosition = playerTransform.position + playerToEnemyDir * bowDistance;
        currentArrowPosition = playerTransform.position + playerToEnemyDir * arrowDistanceFromPlayer;
        arrowAngle = Mathf.Atan2(playerToEnemyDir.y, playerToEnemyDir.x) * Mathf.Rad2Deg;

        SyncBowAndArrowToPlayer();
        SyncBowAndArrowDirection(arrowAngle);

        if (isPlayerStill && canFire)
        {
            FireArrow();
            canFire = false;
            timer = 0f;
        }

        if (!canFire)
        {
            timer += Time.deltaTime;
            if (timer >= spawnInterval)
            {
                canFire = true;
            }
        }
    }

    bool HasEnemyInScene()
    {
        string[] enemyTags = { "Enemy", "DashEnemy", "LongRangeEnemy", "PotionEnemy" };
        foreach (string tag in enemyTags)
        {
            if (GameObject.FindGameObjectWithTag(tag) != null)
                return true;
        }
        return false;
    }

    void UpdateTargeting()
    {
        if (currentTarget != null)
        {
            EnemyHP enemyHP = currentTarget.GetComponent<EnemyHP>();
            if (enemyHP != null && enemyHP.currentHP <= 0)
            {
                ClearTargeting();
                currentTarget = null;
            }
        }

        Transform closestEnemy = FindClosestEnemy(playerTransform.position);

        if (closestEnemy != currentTarget)
        {
            ClearTargeting();
            currentTarget = closestEnemy;

            if (currentTarget != null)
            {
                Vector3 offset = Vector3.zero;
                var col = currentTarget.GetComponent<Collider2D>();
                if (col != null)
                    offset = new Vector3(0f, col.bounds.extents.y - 0.9f, 0f);

                if (targetingPrefab != null)
                {
                    targetingInstance = Instantiate(targetingPrefab, currentTarget.position + offset, Quaternion.identity);
                    targetingInstance.transform.SetParent(currentTarget);
                }

                if (extraTargetingPrefab != null)
                {
                    Vector3 extraOffset = offset + new Vector3(0f, 1.3f, 0f);
                    extraTargetingInstance = Instantiate(extraTargetingPrefab, currentTarget.position + extraOffset, Quaternion.Euler(0f, 0f, -90f));
                    extraTargetingInstance.transform.SetParent(currentTarget);
                }
            }
        }
    }

    void ClearTargeting()
    {
        if (targetingInstance != null)
        {
            Destroy(targetingInstance);
            targetingInstance = null;
        }

        if (extraTargetingInstance != null)
        {
            Destroy(extraTargetingInstance);
            extraTargetingInstance = null;
        }

        currentTarget = null;
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

        GameObject bulletToFire = bulletPrefab;
        if (useFireball && shotCount >= 6 && fireballPrefab != null)
        {
            bulletToFire = fireballPrefab;
            shotCount = 0;
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
            effectBowInstance.SetActive(false);

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

    bool IsPlayerIdle()
    {
        // 예1) 플레이어 애니메이터가 있고, 현재 상태가 Idle인지 체크
        // Animator animator = GameManager.Instance.playerAnimator;
        // return animator.GetCurrentAnimatorStateInfo(0).IsName("Idle");

        // 예2) 플레이어 상태를 enum 등으로 관리하는 경우
        return GameManager.Instance.playerAnimation.currentState == PlayerAnimation.State.Idle;
    }

}
