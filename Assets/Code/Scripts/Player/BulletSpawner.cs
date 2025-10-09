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

    [Header("한 번에 발사할 총알 개수")]
    public int bulletsPerShot = 3;

    [Header("총알 간 각도 퍼짐 (도 단위)")]
    public float spreadAngle = 10f;

    [Header("타겟 표시 프리팹")]
    public GameObject targetMarkerPrefab;

    public Vector3 targetMarkerOffset = new Vector3(0, 0, 0);

    [Header("두 번째 타겟 표시 프리팹")]
    public GameObject secondTargetMarkerPrefab;

    [Header("두 번째 타겟 마커 위치 오프셋")]
    public Vector3 secondTargetMarkerOffset = new Vector3(0, 1f, 0);

    [Header("공격 사거리 설정 (카메라 기준)")]
    [Tooltip("카메라 화면 크기의 몇 배까지 공격할지 (1.0 = 카메라 화면과 동일, 카메라 중심 기준)")]
    public float attackRangeMultiplier = 1.2f;
    
    [Header("사거리 모양 설정")]
    [Tooltip("Circle: 카메라 중심에서 원형 사거리, Rectangle: 카메라 화면 비율에 맞는 사각형 사거리")]
    public AttackRangeType attackRangeType = AttackRangeType.Rectangle;
    
    [Header("디버그")]
    public bool showAttackRange = false;

    //public GameObject bowEffectPrefab;
    //public float bowEffectDuration = 0.2f;

    //[Header("Bow 거리")]
    //public float bowDistance = 0.7f; // 플레이어에서 이 거리에 Bow 이펙트가 생성됨

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

        // 초기 쿨타임 UI 상태 설정 (공격 가능 상태 = 1)
        if (attackCooldownUI != null)
        {
            attackCooldownUI.fillAmount = 1f;
        }
    }

    void Update()
    {
        if (playerController == null || bulletPrefab == null) return;

        Transform closestEnemy = FindClosestEnemy();
        UpdateMarkers(closestEnemy);

        bool isStill = playerController.inputVec.magnitude < 0.05f;
        bool isMoving = !isStill;

        // 쿨타임 감소
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }

        // 쿨타임 UI 업데이트
        UpdateCooldownUI();

        // 정지 상태에서 공격 처리 (적이 있을 때만)
        if (isStill && closestEnemy != null)
        {
            // 이전에 움직이고 있었다면 즉시 공격 (쿨타임이 끝났을 때)
            if (wasMoving && cooldownTimer <= 0f)
            {
                FireArrow(closestEnemy);
                float actualCooldown = attackCooldown / Mathf.Max(0.1f, attackSpeedMultiplier);
                cooldownTimer = actualCooldown;
            }
            // 계속 정지 상태에서 쿨타임이 끝나면 연속 공격
            else if (!wasMoving && cooldownTimer <= 0f)
            {
                FireArrow(closestEnemy);
                float actualCooldown = attackCooldown / Mathf.Max(0.1f, attackSpeedMultiplier);
                cooldownTimer = actualCooldown;
            }
        }

        // 이동 상태 업데이트
        wasMoving = isMoving;
    }

    /// <summary>
    /// 공격 쿨타임 UI를 업데이트합니다.
    /// 공격 가능: 1, 쿨타임 중: 0
    /// </summary>
    private void UpdateCooldownUI()
    {
        if (attackCooldownUI == null) return;

        // 실제 쿨타임 계산 (공격 속도 배율 적용)
        float actualCooldown = attackCooldown / Mathf.Max(0.1f, attackSpeedMultiplier);
        
        // 쿨타임 진행률 계산 (0: 쿨타임 완료, 1: 쿨타임 시작)
        float cooldownProgress = Mathf.Clamp01(cooldownTimer / actualCooldown);
        
        // 공격 가능: 1, 쿨타임 중: 0으로 표시
        attackCooldownUI.fillAmount = 1f - cooldownProgress;
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
                // 사거리 타입에 따라 다른 검사 방법 사용
                bool isInRange = IsEnemyInAttackRange(enemy.transform.position);
                
                if (isInRange)
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

    /// <summary>
    /// 적이 공격 사거리 내에 있는지 확인합니다. (카메라 기준)
    /// </summary>
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
        else // Rectangle
        {
            Vector2 cameraSize = GetCameraSize();
            float halfWidth = cameraSize.x * attackRangeMultiplier * 0.5f;
            float halfHeight = cameraSize.y * attackRangeMultiplier * 0.5f;
            
            float deltaX = Mathf.Abs(enemyPosition.x - cameraPos.x);
            float deltaY = Mathf.Abs(enemyPosition.y - cameraPos.y);
            
            return deltaX <= halfWidth && deltaY <= halfHeight;
        }
    }

    /// <summary>
    /// 카메라 크기를 반환합니다.
    /// </summary>
    private Vector2 GetCameraSize()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return new Vector2(20f, 15f); // 기본값

        if (mainCamera.orthographic)
        {
            float cameraHeight = mainCamera.orthographicSize * 2f;
            float cameraWidth = cameraHeight * mainCamera.aspect;
            return new Vector2(cameraWidth, cameraHeight);
        }
        else
        {
            return new Vector2(30f, 20f); // Perspective 카메라용 기본값
        }
    }

    /// <summary>
    /// 카메라 사이즈를 기반으로 공격 사거리를 계산합니다.
    /// </summary>
    private float GetCameraBasedAttackRange()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return 10f; // 기본값

        // 카메라가 Orthographic인 경우
        if (mainCamera.orthographic)
        {
            // 카메라의 orthographicSize는 화면 높이의 절반
            // 화면의 대각선 길이를 기준으로 사거리 계산
            float cameraHeight = mainCamera.orthographicSize * 2f;
            float cameraWidth = cameraHeight * mainCamera.aspect;
            float cameraDiagonal = Mathf.Sqrt(cameraWidth * cameraWidth + cameraHeight * cameraHeight);
            
            return cameraDiagonal * attackRangeMultiplier;
        }
        else
        {
            // Perspective 카메라인 경우 (일반적으로 2D 게임에서는 사용하지 않음)
            return 15f * attackRangeMultiplier; // 기본값
        }
    }

    /// <summary>
    /// 에디터에서 공격 사거리를 시각적으로 표시합니다. (카메라 기준)
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!showAttackRange) return;

        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;
        
        Vector3 cameraPos = mainCamera.transform.position;
        
        // 공격 사거리 표시 (빨간색) - 카메라 기준
        Gizmos.color = Color.red;
        if (attackRangeType == AttackRangeType.Circle)
        {
            float attackRange = GetCameraBasedAttackRange();
            DrawWireCircle(cameraPos, attackRange);
        }
        else // Rectangle
        {
            Vector2 cameraSize = GetCameraSize();
            float width = cameraSize.x * attackRangeMultiplier;
            float height = cameraSize.y * attackRangeMultiplier;
            DrawWireRectangle(cameraPos, width, height);
        }
        
        // 카메라 화면 크기도 표시 (파란색, 참고용) - 카메라 기준
        if (mainCamera.orthographic)
        {
            Vector2 cameraSize = GetCameraSize();
            Gizmos.color = Color.blue;
            
            if (attackRangeType == AttackRangeType.Circle)
            {
                float cameraDiagonal = Mathf.Sqrt(cameraSize.x * cameraSize.x + cameraSize.y * cameraSize.y);
                DrawWireCircle(cameraPos, cameraDiagonal);
            }
            else
            {
                DrawWireRectangle(cameraPos, cameraSize.x, cameraSize.y);
            }
        }
    }

    /// <summary>
    /// Gizmos를 사용하여 원을 그립니다.
    /// </summary>
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

    /// <summary>
    /// Gizmos를 사용하여 사각형을 그립니다.
    /// </summary>
    private void DrawWireRectangle(Vector3 center, float width, float height)
    {
        float halfWidth = width * 0.5f;
        float halfHeight = height * 0.5f;
        
        Vector3 topLeft = center + new Vector3(-halfWidth, halfHeight, 0);
        Vector3 topRight = center + new Vector3(halfWidth, halfHeight, 0);
        Vector3 bottomRight = center + new Vector3(halfWidth, -halfHeight, 0);
        Vector3 bottomLeft = center + new Vector3(-halfWidth, -halfHeight, 0);
        
        // 사각형의 각 변을 그립니다
        Gizmos.DrawLine(topLeft, topRight);      // 위쪽
        Gizmos.DrawLine(topRight, bottomRight);  // 오른쪽
        Gizmos.DrawLine(bottomRight, bottomLeft); // 아래쪽
        Gizmos.DrawLine(bottomLeft, topLeft);    // 왼쪽
    }

    private void FireArrow(Transform centerTarget)
    {
        if (centerTarget == null) return;

        AudioManager.Instance?.PlayArrowSound(1.5f); // 🔊 커스텀 1.5배

        VibrationManager.Vibrate(50);


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

/// <summary>
/// 공격 사거리의 모양을 정의하는 열거형
/// </summary>
public enum AttackRangeType
{
    Circle,    // 원형 사거리
    Rectangle  // 사각형 사거리
}