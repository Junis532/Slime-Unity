using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;

public class JoystickDirectionIndicator : MonoBehaviour
{
    [Header("플레이어")]
    public PlayerController playerController;  // Inspector에서 연결

    [Header("장애물 레이어")]
    public LayerMask obstacleLayer; // Inspector에서 Obstacle 레이어 지정


    [Header("스킬 이펙트")]
    public GameObject slimeJumpLandEffectPrefab;

    [Header("슬라임 점프 설정")]
    public float slimeJumpDamage = 1000f;
    public float slimeJumpRadius = 5f;
    public LayerMask enemyLayer;

    [Header("쿨타임 관련")]
    public TMP_Text waitTimerText;
    public Image CooltimeImage;
    public int waitInterval = 10;

    [Header("넉백 관련")]
    public float knockbackDistance = 1f;
    public float knockbackTime = 0.2f;
    public float knockbackJumpPower = 0f;

    [Header("스킬 버튼")]
    public Button slimeJumpButton; // Inspector에서 드래그 가능

    private bool hasUsedSkill = false;
    private bool isSkillActive = false;
    private Coroutine rollCoroutine;
    private Vector3 originalScale;

    public const float DashingDistance = 4f;

    // 외부에서 참조 가능
    public bool IsUsingSkill => isSkillActive;

    private void Start()
    {
        originalScale = transform.localScale;
        waitTimerText.text = "";
        if (CooltimeImage != null)
            CooltimeImage.fillAmount = 0f;

        // 버튼 이벤트 연결
        if (slimeJumpButton != null)
            slimeJumpButton.onClick.AddListener(UseSkillButton);

        StartRollingLoop();
    }

    /// <summary>
    /// 버튼 클릭 → 스킬 발동 시도
    /// </summary>
    public void UseSkillButton()
    {
        if (hasUsedSkill || isSkillActive) return;

        UseSlimeJump();
        hasUsedSkill = true;

        // 스킬 사용 시 버튼/쿨타임 UI 순서 변경
        if (slimeJumpButton != null)
            slimeJumpButton.transform.SetSiblingIndex(1); // 버튼 order 1
        if (CooltimeImage != null)
            CooltimeImage.transform.SetSiblingIndex(2);   // 쿨타임 이미지 order 2
        if (waitTimerText != null)
            waitTimerText.transform.SetSiblingIndex(2);   // 쿨타임 텍스트 order 2

        OnSkillUsed();
    }

    private void UseSlimeJump()
    {
        if (isSkillActive) return;
        isSkillActive = true;

        transform.DOKill();
        AudioManager.Instance.PlaySFX(AudioManager.Instance.jumpSound);

        Vector3 dashDirection = Vector3.right;
        if (playerController != null && playerController.inputVec.magnitude > 0.05f)
            dashDirection = new Vector3(playerController.inputVec.x, playerController.inputVec.y, 0f).normalized;

        // 장애물 체크 (Raycast 사용)
        float dashDistance = DashingDistance;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dashDirection, DashingDistance, obstacleLayer);
        if (hit.collider != null)
        {
            dashDistance = hit.distance - 0.1f; // 약간 앞에서 멈추게
        }


        Vector3 targetPos = transform.position + dashDirection * dashDistance;

        float dashDuration = 0.3f * (dashDistance / DashingDistance); // 거리 비례
        transform.DOMove(targetPos, dashDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                transform.localScale = originalScale;
                if (slimeJumpLandEffectPrefab != null)
                {
                    GameObject effect = Instantiate(slimeJumpLandEffectPrefab, targetPos, Quaternion.identity);
                    Destroy(effect, 0.3f);
                }
                AudioManager.Instance.PlaySFX(AudioManager.Instance.land);
                DealSlimeJumpDamage(targetPos);
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

    IEnumerator RollingLoopRoutine()
    {
        while (hasUsedSkill)
        {
            float waitTime = waitInterval;
            while (waitTime > 0f)
            {
                if (waitTimerText != null)
                    waitTimerText.text = $"{Mathf.CeilToInt(waitTime)}";
                waitTime -= Time.deltaTime;
                if (CooltimeImage != null)
                    CooltimeImage.fillAmount = waitTime / waitInterval;
                yield return null;
            }

            hasUsedSkill = false;

            // 쿨타임 종료 → 버튼 order 다시 3으로
            if (slimeJumpButton != null)
                slimeJumpButton.transform.SetSiblingIndex(3);

            if (waitTimerText != null)
                waitTimerText.text = "";
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
            if (waitTimerText != null)
                waitTimerText.text = "";
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
}
