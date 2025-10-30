using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

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

    [Header("공격 쿨타임")]
    public float attackCooldown = 0.3f;

    [Header("공격 쿨타임 UI")]
    [Tooltip("공격 쿨타임을 표시할 Filled UI (Image 컴포넌트)")]
    public Image attackCooldownUI;

    [Header("차징 시스템")]
    [Tooltip("0~1: 차지 상태, 1 = 완전히 차 있음")]
    public float chargeAmount = 0f;
    public float chargeSpeed = 0.5f; // 초당 충전량
    public float maxCharge = 1f;      // 최대 차지값
    [Tooltip("차징 게이지 UI")]
    public Image chargeUI;

    [Header("한 번에 발사할 총알 개수")]
    public int bulletsPerShot = 3;

    [Header("총알 간 각도 퍼짐 (도 단위)")]
    public float spreadAngle = 10f;

    [Header("타겟 표시 프리팹")]
    public GameObject targetMarkerPrefab;
    public Vector3 targetMarkerOffset = new Vector3(0, 0, 0);

    [Header("두 번째 타겟 표시 프리팹")]
    public GameObject secondTargetMarkerPrefab;
    public Vector3 secondTargetMarkerOffset = new Vector3(0, 1f, 0);

    [Header("공격 사거리 설정 (카메라 기준)")]
    [Tooltip("카메라 화면 크기의 몇 배까지 공격할지 (1.0 = 카메라 화면과 동일, 카메라 중심 기준)")]
    public float attackRangeMultiplier = 1.2f;

    [Header("사거리 모양 설정")]
    [Tooltip("Circle: 카메라 중심에서 원형 사거리, Rectangle: 카메라 화면 비율에 맞는 사각형 사거리")]
    public AttackRangeType attackRangeType = AttackRangeType.Rectangle;

    [Header("디버그")]
    public bool showAttackRange = false;

    private GameObject secondMarker;
    private GameObject currentMarker;

    private float cooldownTimer = 0f;
    private bool wasMoving = false;
    private PlayerController playerController;

    private int fireCount = 0;

    void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            playerController = playerObj.GetComponent<PlayerController>();

        if (attackCooldownUI != null)
            attackCooldownUI.fillAmount = 1f;

        if (chargeUI != null)
            chargeUI.fillAmount = 0f;
    }

    void Update()
    {
        if (playerController == null || bulletPrefab == null) return;

        Transform closestEnemy = FindClosestEnemy();
        UpdateMarkers(closestEnemy);

        bool isStill = playerController.inputVec.magnitude < 0.05f;
        bool isMoving = !isStill;

        // ================= 차징 처리 =================
        // 이동 중이거나 정지 중이지만 공격할 적이 없으면 차징
        if (isMoving || closestEnemy == null)
        {
            chargeAmount += chargeSpeed * Time.deltaTime;
            chargeAmount = Mathf.Min(chargeAmount, maxCharge);
        }

        if (chargeUI != null)
            chargeUI.fillAmount = chargeAmount / maxCharge;

        // ================= 공격 처리 =================
        // 멈춰있고 적이 있을 때만 공격
        if (isStill && closestEnemy != null && cooldownTimer <= 0f)
        {
            FireArrow(closestEnemy);

            // 공격 시 차지 초기화
            chargeAmount = 0f;

            float actualCooldown = attackCooldown / Mathf.Max(0.1f, attackSpeedMultiplier);
            cooldownTimer = actualCooldown;
        }

        // 쿨타임 감소
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        // 공격 쿨타임 UI 업데이트
        UpdateCooldownUI();

        wasMoving = isMoving;
    }


    private void UpdateCooldownUI()
    {
        if (attackCooldownUI == null) return;

        float actualCooldown = attackCooldown / Mathf.Max(0.1f, attackSpeedMultiplier);
        float cooldownProgress = Mathf.Clamp01(cooldownTimer / actualCooldown);
        attackCooldownUI.fillAmount = 1f - cooldownProgress;
    }

    private void UpdateMarkers(Transform closestEnemy)
    {
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
                if (IsEnemyInAttackRange(enemy.transform.position))
                {
                    float dist = Vector3.Distance(playerController.transform.position, enemy.transform.position);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closest = enemy.transform;
                    }
                }
            }
        }

        return closest;
    }

    private bool IsEnemyInAttackRange(Vector3 enemyPosition)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return false;

        Vector3 cameraPos = mainCamera.transform.position;

        if (attackRangeType == AttackRangeType.Circle)
        {
            float maxAttackRange = GetCameraBasedAttackRange();
            float distance = Vector3.Distance(cameraPos, enemyPosition);
            return distance <= maxAttackRange;
        }
        else
        {
            Vector2 cameraSize = GetCameraSize();
            float halfWidth = cameraSize.x * attackRangeMultiplier * 0.5f;
            float halfHeight = cameraSize.y * attackRangeMultiplier * 0.5f;

            float deltaX = Mathf.Abs(enemyPosition.x - cameraPos.x);
            float deltaY = Mathf.Abs(enemyPosition.y - cameraPos.y);

            return deltaX <= halfWidth && deltaY <= halfHeight;
        }
    }

    private Vector2 GetCameraSize()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return new Vector2(20f, 15f);

        if (mainCamera.orthographic)
        {
            float cameraHeight = mainCamera.orthographicSize * 2f;
            float cameraWidth = cameraHeight * mainCamera.aspect;
            return new Vector2(cameraWidth, cameraHeight);
        }
        else
        {
            return new Vector2(30f, 20f);
        }
    }

    private float GetCameraBasedAttackRange()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return 10f;

        if (mainCamera.orthographic)
        {
            float cameraHeight = mainCamera.orthographicSize * 2f;
            float cameraWidth = cameraHeight * mainCamera.aspect;
            float cameraDiagonal = Mathf.Sqrt(cameraWidth * cameraWidth + cameraHeight * cameraHeight);
            return cameraDiagonal * attackRangeMultiplier;
        }
        else
        {
            return 15f * attackRangeMultiplier;
        }
    }

    private void FireArrow(Transform centerTarget)
    {
        if (centerTarget == null) return;

        AudioManager.Instance?.PlayArrowSound(1.5f);
        VibrationManager.Vibrate(50);

        if (playerController != null)
        {
            Transform player = playerController.transform;

            player.DOKill();
            Sequence seq = DOTween.Sequence();
            seq.Append(player.DOScale(new Vector3(4.3f * 1.4f, 4.3f * 0.6f, player.localScale.z), 0.08f).SetEase(Ease.OutQuad));
            seq.Append(player.DOScale(new Vector3(4.3f, 4.3f, player.localScale.z), 0.18f).SetEase(Ease.OutBack));
        }

        fireCount++;
        bool isFireballShot = (fireCount % 7 == 0) && (fireballPrefab != null) && useFireball;

        Vector3 dirToTarget = (centerTarget.position - playerController.transform.position).normalized;
        float centerAngle = Mathf.Atan2(dirToTarget.y, dirToTarget.x) * Mathf.Rad2Deg;

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

                if (isFireballThisShot)
                {
                    FireballAI fireballAI = bullet.GetComponent<FireballAI>();
                    if (fireballAI != null)
                        fireballAI.InitializeBullet(spawnPos, angle);

                    SetAlphaRecursive(bullet, 1f);
                }
            }
        }
    }

    private void FlipPlayer(Vector3 dir)
    {
        SpriteRenderer sr = playerController.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.flipX = dir.x < 0;
    }

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

    private void OnDrawGizmosSelected()
    {
        if (!showAttackRange) return;

        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        Vector3 cameraPos = mainCamera.transform.position;
        Gizmos.color = Color.red;
        if (attackRangeType == AttackRangeType.Circle)
            DrawWireCircle(cameraPos, GetCameraBasedAttackRange());
        else
        {
            Vector2 cameraSize = GetCameraSize();
            DrawWireRectangle(cameraPos, cameraSize.x * attackRangeMultiplier, cameraSize.y * attackRangeMultiplier);
        }
    }

    private void DrawWireCircle(Vector3 center, float radius)
    {
        int segments = 32;
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }

    private void DrawWireRectangle(Vector3 center, float width, float height)
    {
        float halfWidth = width * 0.5f;
        float halfHeight = height * 0.5f;

        Vector3 topLeft = center + new Vector3(-halfWidth, halfHeight, 0);
        Vector3 topRight = center + new Vector3(halfWidth, halfHeight, 0);
        Vector3 bottomRight = center + new Vector3(halfWidth, -halfHeight, 0);
        Vector3 bottomLeft = center + new Vector3(-halfWidth, -halfHeight, 0);

        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
        Gizmos.DrawLine(bottomLeft, topLeft);
    }
}

public enum AttackRangeType
{
    Circle,
    Rectangle
}
