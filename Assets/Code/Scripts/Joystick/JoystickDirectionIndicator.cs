using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

public class JoystickDirectionIndicator : MonoBehaviour
{
    [Header("플레이어")]
    public PlayerController playerController; // Inspector에서 연결

    [Header("장애물 레이어")]
    public LayerMask obstacleLayer;

    [Header("슬라임 점프 설정")]
    public float slimeJumpDamage = 1000f;
    public float slimeJumpRadius = 5f;
    public LayerMask enemyLayer;

    [Header("쿨타임 관련")]
    public Image CooltimeImage;
    public float waitInterval = 10;

    [Header("넉백 관련")]
    public float knockbackDistance = 1f;
    public float knockbackTime = 0.2f;
    public float knockbackJumpPower = 0f;

    [Header("스킬 버튼")]
    public Button slimeJumpButton;

    [Header("잔상 효과")]
    public GameObject afterImagePrefab;
    public float afterImageSpawnInterval = 0.05f;
    public float afterImageFadeDuration = 0.3f;
    public float afterImageLifeTime = 0.5f;
    public int maxAfterImageCount = 10; // 🔥 잔상 최대 개수
    private List<GameObject> afterImages = new List<GameObject>();

    [Header("대쉬 설정")]
    public float DashingDistance = 3f;

    private bool hasUsedSkill = false;
    private bool isSkillActive = false;
    private Coroutine rollCoroutine;
    private Coroutine afterImageCoroutine;
    private Vector3 originalScale;
    private Vector3 lastDashDirection = Vector3.right; // 마지막 이동 방향 저장

    public bool IsUsingSkill => isSkillActive;

    private Sequence dashSequence; // 현재 대쉬 트윈 저장용

    private void Start()
    {
        originalScale = transform.localScale;
        if (CooltimeImage != null) CooltimeImage.fillAmount = 1f;
        if (slimeJumpButton != null) slimeJumpButton.onClick.AddListener(UseSkillButton);
        StartRollingLoop();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            UseSkillButton();

        // 이동 중일 때마다 마지막 방향 갱신
        if (playerController != null && playerController.inputVec.magnitude > 0.05f)
        {
            lastDashDirection = new Vector3(playerController.inputVec.x, playerController.inputVec.y, 0f).normalized;
        }
    }

    public void UseSkillButton()
    {
        if (hasUsedSkill || isSkillActive) return;

        // ✅ 플레이어 이동 불가 상태일 때 스킬 사용 금지
        if (playerController != null && !playerController.canMove) return;

        else if (playerController != null && playerController.canMove)
        {
            UseSlimeJump();
            hasUsedSkill = true;

            if (slimeJumpButton != null) slimeJumpButton.transform.SetSiblingIndex(1);
            if (CooltimeImage != null) CooltimeImage.transform.SetSiblingIndex(2);

            OnSkillUsed();
        }
    }

    private void UseSlimeJump()
    {
        if (isSkillActive) return;
        isSkillActive = true;

        GameManager.Instance.audioManager.PlayDashSound(1.5f);

        transform.DOKill();
        //AudioManager.Instance.PlaySFX(AudioManager.Instance.jumpSound);

        // 방향 결정
        Vector3 dashDirection = Vector3.right;
        if (playerController != null && playerController.inputVec.magnitude > 0.05f)
        {
            dashDirection = new Vector3(playerController.inputVec.x, playerController.inputVec.y, 0f).normalized;
            lastDashDirection = dashDirection; // 마지막 이동 방향 저장
        }
        else
        {
            dashDirection = lastDashDirection; // 이동이 없으면 마지막 방향 사용
        }

        // 예상 목표 위치
        Vector3 targetPos = transform.position + dashDirection * DashingDistance;

        // ---------------------------
        // 1️⃣ 장애물 체크
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dashDirection, DashingDistance, obstacleLayer);
        if (hit.collider != null)
        {
            Vector3 closestPoint = hit.collider.ClosestPoint(transform.position);
            targetPos = closestPoint - dashDirection * 0.1f;
        }

        // ---------------------------
        // 2️⃣ Room 안으로 제한
        RoomData currentRoom = GameManager.Instance.waveManager.GetPlayerRoom();
        if (currentRoom != null && currentRoom.roomCollider != null)
        {
            if (!currentRoom.roomCollider.OverlapPoint(targetPos))
            {
                Vector3 closestPointInRoom = currentRoom.roomCollider.ClosestPoint(targetPos);
                targetPos = closestPointInRoom;
            }
        }

        float dashDistance = Vector3.Distance(transform.position, targetPos);
        float dashDuration = 0.2f * (dashDistance / DashingDistance);

        // ---------------------------
        // DOTween Sequence
        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOMove(targetPos, dashDuration).SetEase(Ease.OutQuad));
        seq.Join(transform.DOScale(new Vector3(originalScale.x * 1.4f, originalScale.y * 0.6f, originalScale.z), dashDuration * 0.4f).SetEase(Ease.OutQuad));
        seq.Append(transform.DOScale(originalScale, dashDuration * 0.6f).SetEase(Ease.OutBack));

        if (afterImageCoroutine != null) StopCoroutine(afterImageCoroutine);
        afterImageCoroutine = StartCoroutine(SpawnAfterImages());

        // 대쉬 감시 코루틴 시작
        StartCoroutine(DashCancelWatcher(seq));

        seq.OnComplete(() =>
        {
            if (afterImageCoroutine != null)
            {
                StopCoroutine(afterImageCoroutine);
                afterImageCoroutine = null;
            }
            StartCoroutine(EndSkillAfterDelay(0.5f));
        });
    }

    private IEnumerator DashCancelWatcher(Sequence seq)
    {
        // 대쉬 중일 때만 감시
        while (isSkillActive)
        {
            if (playerController != null && !playerController.canMove)
            {
                Debug.Log("⛔ 대쉬 중 이동 불가 상태 감지! 대쉬 즉시 중단");
                transform.localScale = new Vector3(4.5f, 4.5f, 0f);
                seq.Kill(); // 트윈 중단
                if (afterImageCoroutine != null)
                {
                    StopCoroutine(afterImageCoroutine);
                    afterImageCoroutine = null;
                }
                isSkillActive = false;
                yield break;
            }
            yield return null;
        }
    }


    private IEnumerator EndSkillAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        isSkillActive = false;
    }

    private string[] enemyTags = { "Enemy", "DashEnemy", "LongRangeEnemy", "PotionEnemy" };

    void DealSlimeJumpDamage(Vector3 position)
    {
        foreach (string tag in enemyTags)
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject enemy in enemies)
            {
                float dist = Vector3.Distance(position, enemy.transform.position);
                if (dist <= slimeJumpRadius)
                {
                    EnemyHP enemyhp = enemy.GetComponent<EnemyHP>();
                    if (enemyhp != null)
                        enemyhp.SkillTakeDamage((int)slimeJumpDamage);

                    Vector3 dir = (enemy.transform.position - position).normalized;
                    Vector3 knockbackPos = enemy.transform.position + dir * knockbackDistance;
                    enemy.transform.DOKill();
                    enemy.transform.DOJump(knockbackPos, knockbackJumpPower, 1, knockbackTime).SetEase(Ease.OutQuad);
                }
            }
        }
    }

    // ==========================
    // 쿨타임 루프
    // ==========================
    IEnumerator RollingLoopRoutine()
    {
        while (hasUsedSkill)
        {
            float elapsedTime = 0f;
            while (elapsedTime < waitInterval)
            {
                elapsedTime += Time.deltaTime;
                // 쿨타임이 진행될수록 0에서 1로 차오르게 변경
                if (CooltimeImage != null) CooltimeImage.fillAmount = 1f - (elapsedTime / waitInterval);
                yield return null;
            }
            hasUsedSkill = false;

            if (slimeJumpButton != null)
                slimeJumpButton.transform.SetSiblingIndex(3);
        }
    }

    public void StartRollingLoop()
    {
        if (rollCoroutine == null)
            rollCoroutine = StartCoroutine(RollingLoopRoutine());
    }

    public void StopRollingLoop()
    {
        if (rollCoroutine != null)
        {
            StopCoroutine(rollCoroutine);
            rollCoroutine = null;
        }
    }

    public void OnSkillUsed()
    {
        if (rollCoroutine != null)
        {
            StopCoroutine(rollCoroutine);
            rollCoroutine = null;
        }
        rollCoroutine = StartCoroutine(RollingLoopRoutine());
    }

    // ==========================
    // 잔상 관련
    // ==========================
    private IEnumerator SpawnAfterImages()
    {
        while (isSkillActive)
        {
            CreateAfterImage();
            yield return new WaitForSeconds(afterImageSpawnInterval);
        }
    }

    private void CreateAfterImage()
    {
        // 🎯 플레이어의 SpriteRenderer를 자식까지 포함해서 찾기
        SpriteRenderer playerSR = GetComponentInChildren<SpriteRenderer>();
        if (playerSR == null)
        {
            Debug.LogWarning("❌ 플레이어 SpriteRenderer를 찾을 수 없습니다!");
            return;
        }

        // 🎯 잔상 오브젝트 생성 (부모 없음, 월드 공간에 직접 생성)
        GameObject afterImage = new GameObject("AfterImage");
        afterImage.transform.position = playerSR.transform.position; // 자식 기준 좌표
        afterImage.transform.rotation = playerSR.transform.rotation;
        afterImage.transform.localScale = playerSR.transform.lossyScale; // ✅ 월드 스케일 유지
        afterImage.transform.parent = null; // 🔥 반드시 부모 해제 (월드에 존재하도록)

        // 🎯 SpriteRenderer 추가
        SpriteRenderer sr = afterImage.AddComponent<SpriteRenderer>();
        sr.sprite = playerSR.sprite;
        sr.flipX = playerSR.flipX;

        // 색상 설정 (투명도는 필요에 따라 조절)
        Color c = playerSR.color;
        c.a = 0.6f;
        sr.color = c;

        // 🔥 자식 SpriteRenderer와 동일한 Layer / Order 유지
        sr.sortingLayerID = playerSR.sortingLayerID;
        sr.sortingOrder = playerSR.sortingOrder - 1;

        // 🔥 잔상 Fade 및 파괴 루틴
        float delay = Mathf.Max(0f, afterImageLifeTime - afterImageFadeDuration);
        sr.DOFade(0f, afterImageFadeDuration)
          .SetDelay(delay)
          .OnComplete(() => Destroy(afterImage));

        // 🔥 리스트 관리
        afterImages.Add(afterImage);
        if (afterImages.Count > maxAfterImageCount)
        {
            GameObject oldest = afterImages[0];
            afterImages.RemoveAt(0);
            if (oldest != null) Destroy(oldest);
        }
    }

}
