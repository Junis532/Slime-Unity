using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

public class JoystickDirectionIndicator : MonoBehaviour
{
    [Header("플레이어")]
    public PlayerController playerController;  // Inspector에서 연결

    [Header("장애물 레이어")]
    public LayerMask obstacleLayer;

    [Header("슬라임 점프 설정")]
    public float slimeJumpDamage = 1000f;
    public float slimeJumpRadius = 5f;
    public LayerMask enemyLayer;

    [Header("쿨타임 관련")]
    public Image CooltimeImage;
    public int waitInterval = 10;

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

    [Header("대쉬 설정")]
    public float DashingDistance = 3f;

    private bool hasUsedSkill = false;
    private bool isSkillActive = false;
    private Coroutine rollCoroutine;
    private Coroutine afterImageCoroutine;
    private Vector3 originalScale;

    public bool IsUsingSkill => isSkillActive;

    private void Start()
    {
        originalScale = transform.localScale;
        if (CooltimeImage != null) CooltimeImage.fillAmount = 0f;

        if (slimeJumpButton != null)
            slimeJumpButton.onClick.AddListener(UseSkillButton);

        StartRollingLoop();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            UseSkillButton();
    }

    public void UseSkillButton()
    {
        if (hasUsedSkill || isSkillActive) return;

        UseSlimeJump();
        hasUsedSkill = true;

        if (slimeJumpButton != null) slimeJumpButton.transform.SetSiblingIndex(1);
        if (CooltimeImage != null) CooltimeImage.transform.SetSiblingIndex(2);

        OnSkillUsed();
    }

    private void UseSlimeJump()
    {
        if (isSkillActive) return;
        isSkillActive = true;

        transform.DOKill();
        AudioManager.Instance.PlaySFX(AudioManager.Instance.jumpSound);

        // 방향 결정
        Vector3 dashDirection = Vector3.right;
        if (playerController != null && playerController.inputVec.magnitude > 0.05f)
            dashDirection = new Vector3(playerController.inputVec.x, playerController.inputVec.y, 0f).normalized;

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
        float dashDuration = 0.3f * (dashDistance / DashingDistance);

        // ---------------------------
        // DOTween Sequence
        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOMove(targetPos, dashDuration).SetEase(Ease.OutQuad));
        seq.Join(transform.DOScale(new Vector3(originalScale.x * 1.4f, originalScale.y * 0.6f, originalScale.z), dashDuration * 0.4f).SetEase(Ease.OutQuad));
        seq.Append(transform.DOScale(originalScale, dashDuration * 0.6f).SetEase(Ease.OutBack));

        if (afterImageCoroutine != null) StopCoroutine(afterImageCoroutine);
        afterImageCoroutine = StartCoroutine(SpawnAfterImages());

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
            float waitTime = waitInterval;
            while (waitTime > 0f)
            {
                waitTime -= Time.deltaTime;
                if (CooltimeImage != null) CooltimeImage.fillAmount = waitTime / waitInterval;
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
        if (afterImagePrefab == null) return;

        GameObject afterImage = Instantiate(afterImagePrefab, transform.position, transform.rotation);
        afterImage.transform.parent = null;

        Collider2D col = afterImage.GetComponent<Collider2D>();
        if (col != null) Destroy(col);
        Rigidbody2D rb = afterImage.GetComponent<Rigidbody2D>();
        if (rb != null) Destroy(rb);

        foreach (Transform child in afterImage.transform)
            child.gameObject.SetActive(false);

        SpriteRenderer playerSR = GetComponent<SpriteRenderer>();
        SpriteRenderer sr = afterImage.GetComponent<SpriteRenderer>();
        if (playerSR != null && sr != null)
        {
            sr.sprite = playerSR.sprite;
            sr.flipX = playerSR.flipX;
            sr.color = playerSR.color;
        }

        sr.DOFade(0f, afterImageFadeDuration).SetDelay(afterImageLifeTime - afterImageFadeDuration)
            .OnComplete(() => Destroy(afterImage));
    }
}
