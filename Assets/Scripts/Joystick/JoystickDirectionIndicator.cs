using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using SkillNumber.Skills;

public class JoystickDirectionIndicator : MonoBehaviour
{
    [Header("조이스틱 및 UI 요소")]
    public VariableJoystick joystick;
    public CanvasGroup joystickCanvasGroup;
    public GameObject imageToHideWhenTouching;

    [Header("범위 관련 프리팹 및 위치")]
    public List<GameObject> directionSpritePrefabs;
    public List<float> distancesFromPlayer;
    public List<float> spriteBackOffsets;
    public List<float> skillAngleOffsets;

    private GameObject landingIndicatorInstance; // 점프 착지 범위 표시용

    [Header("착지 이펙트")]
    public GameObject slimeJumpLandEffectPrefab;


    [Header("슬라임 점프 설정")]
    public float slimeJumpDamage = 1000f;
    public float slimeJumpRadius = 5f;
    public LayerMask enemyLayer;

    private GameObject indicatorInstance;
    private int currentIndicatorIndex = -1;
    private PlayerController playerController;

    private bool isTouchingJoystick, wasTouchingJoystickLastFrame;
    private Vector2 lastInputDirection = Vector2.right;
    private float lastInputMagnitude = 0f;
    private bool hasUsedSkill = false;
    private bool prevBlockInputActive = false;

    [Header("스킬 쿨타임 관련")]
    public TMP_Text waitTimerText;
    public Image CooltimeImange;
    public int waitInterval = 10;

    [Header("넉백 관련")]
    public float knockbackDistance = 1f;   // 밀려나는 거리
    public float knockbackTime = 0.2f;     // 밀려나는 시간
    public float knockbackJumpPower = 0f; // 위로 살짝 튀는 높이 (원하면 0)

    private Coroutine rollCoroutine;

    public static bool isRolling = false;
    public static int currentDiceResult = 1;

    private Vector3 originalScale;
    private Vector3 originalPosition;

    private bool isUsingSkill = false;
    public bool IsUsingSkill => isUsingSkill;

    // --- ▼ 추가: 라인 렌더러 준비 ▼ ---
    private LineRenderer arcLine;

    void Awake()
    {
        arcLine = GetComponent<LineRenderer>();
        if (arcLine == null)
        {
            arcLine = gameObject.AddComponent<LineRenderer>();
            arcLine.startWidth = 0.18f;
            arcLine.endWidth = 0.10f;
            arcLine.useWorldSpace = true;
            arcLine.material = new Material(Shader.Find("Sprites/Default"));
            arcLine.startColor = new Color(1f, 1f, 0f, 1f);
            arcLine.endColor = new Color(1f, 1f, 0f, 1f);
            arcLine.sortingLayerName = "Default"; // 필요에 따라 "UI"로 수정 가능
            arcLine.sortingOrder = 999;
            arcLine.positionCount = 0;
            Debug.Log("LineRenderer가 추가되었습니다.");
        }
    }

    void Start()
    {
        playerController = GetComponent<PlayerController>();
        if (joystickCanvasGroup != null) joystickCanvasGroup.alpha = 0f;

        currentDiceResult = 1;
        StartRollingLoop();

        waitTimerText.text = "";
        if (CooltimeImange != null)
            CooltimeImange.fillAmount = 0f;

        originalScale = transform.localScale;
        originalPosition = transform.position;
    }

    private bool wasInGameStateLastFrame = false; // 이전 프레임 게임 상태 저장

    void Update()
    {
        bool isGameState = GameManager.Instance != null && GameManager.Instance.CurrentState == "Game";
        bool isShopState = GameManager.Instance != null && GameManager.Instance.CurrentState == "Shop";

        // --- 게임 상태로 전환되었을 때만 쿨타임 초기화 ---
        if (isGameState && !wasInGameStateLastFrame)
        {
            hasUsedSkill = false;

            if (waitTimerText != null)
                waitTimerText.text = "";

            if (CooltimeImange != null)
                CooltimeImange.fillAmount = 0f;

            if (rollCoroutine != null)
            {
                StopCoroutine(rollCoroutine);
                rollCoroutine = null;
            }

            StartRollingLoop(); // 롤링 루프 재시작
        }

        wasInGameStateLastFrame = isGameState; // 상태 저장

        // --- 기존 블록 입력 처리 ---
        if (prevBlockInputActive && !isShopState)
            ResetInputStates();
        prevBlockInputActive = isShopState;

        // --- 입력 비활성 상태 처리 ---
        if (isShopState || currentDiceResult <= 0)
        {
            DisableInputAndIndicators();
            return;
        }

        Vector2 input = (joystick != null) ? new Vector2(joystick.Horizontal, joystick.Vertical) : playerController.InputVector;
        isTouchingJoystick = input.magnitude > 0.2f;
        SetHideImageState(!isTouchingJoystick);

        if (hasUsedSkill)
        {
            DisableInputAndIndicators();
            return;
        }

        if (joystickCanvasGroup != null)
            joystickCanvasGroup.alpha = isTouchingJoystick ? 1f : 0f;

        if (isTouchingJoystick)
        {
            lastInputDirection = input.normalized;
            lastInputMagnitude = input.magnitude;
            SkillType currentSkill = GetMappedSkillType(currentDiceResult);

            if (currentSkill == SkillType.SlimeJump)
                UpdateSlimeJumpIndicator(input);
            else
            {
                OnSkillButtonPressed();
                UpdateSkillIndicator(input, (int)currentSkill);
            }
        }
        else
        {
            if (wasTouchingJoystickLastFrame && !hasUsedSkill && lastInputMagnitude > 0.3f)
            {
                OnSkillButtonReleased();
                hasUsedSkill = true;
            }

            if (indicatorInstance != null)
                indicatorInstance.SetActive(false);

            if (arcLine != null)
                arcLine.positionCount = 0;

            currentIndicatorIndex = -1;
        }

        if (indicatorInstance != null && indicatorInstance.activeSelf && currentDiceResult == 1)
            DrawArc(transform.position, indicatorInstance.transform.position, 2f, 30);
        else if (arcLine != null)
            arcLine.positionCount = 0;

        wasTouchingJoystickLastFrame = isTouchingJoystick;
        if (joystick != null)
            joystick.enabled = !isRolling && !hasUsedSkill;
    }


    void ResetInputStates()
    {
        isTouchingJoystick = false;
        wasTouchingJoystickLastFrame = false;
        lastInputDirection = Vector2.right;
        lastInputMagnitude = 0f;
        hasUsedSkill = false;
        currentIndicatorIndex = -1;

        transform.DOKill();

        transform.localScale = originalScale;
        transform.position = originalPosition;

        if (indicatorInstance != null) Destroy(indicatorInstance);
        if (joystickCanvasGroup != null) joystickCanvasGroup.alpha = 0f;
        if (joystick != null) { joystick.ResetInput(); joystick.enabled = true; }

        if (arcLine != null) arcLine.positionCount = 0;
    }

    void DisableInputAndIndicators()
    {
        if (joystick != null) joystick.enabled = false;
        SetHideImageState(true);
        if (indicatorInstance != null) indicatorInstance.SetActive(false);
        currentIndicatorIndex = -1;
        if (joystickCanvasGroup != null) joystickCanvasGroup.alpha = 0f;
        transform.DOKill();

        if (arcLine != null) arcLine.positionCount = 0;
    }

    void SetHideImageState(bool isVisible) => imageToHideWhenTouching?.SetActive(isVisible);

    SkillType GetMappedSkillType(int diceResult)
    {
        switch (diceResult)
        {
            case 1: return SkillType.SlimeJump;
            default: return SkillType.None;
        }
    }

    void UpdateSlimeJumpIndicator(Vector2 input)
    {
        int index = (int)SkillType.SlimeJump - 1;
        if (index < 0 || index >= directionSpritePrefabs.Count)
        {
            if (indicatorInstance != null) indicatorInstance.SetActive(false);
            currentIndicatorIndex = -1;
            if (arcLine != null) arcLine.positionCount = 0;
            return;
        }

        float maxDistance = distancesFromPlayer.Count > index ? distancesFromPlayer[index] : 3f;
        float backOffset = spriteBackOffsets.Count > index ? spriteBackOffsets[index] : 0f;
        float offsetAngle = skillAngleOffsets.Count > index ? skillAngleOffsets[index] : 0f;

        Vector3 direction = new Vector3(input.x, input.y, 0f).normalized;
        float clampedMagnitude = Mathf.Clamp01(input.magnitude);

        Vector3 targetPos = transform.position + direction * maxDistance * clampedMagnitude;

        if (indicatorInstance == null || currentIndicatorIndex != index)
        {
            if (indicatorInstance != null) Destroy(indicatorInstance);
            indicatorInstance = Instantiate(directionSpritePrefabs[index], transform.position, Quaternion.identity);
            currentIndicatorIndex = index;
        }

        indicatorInstance.SetActive(true);

        Vector3 offset = -indicatorInstance.transform.up * backOffset;
        indicatorInstance.transform.position = targetPos + offset;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + offsetAngle;
        indicatorInstance.transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void UpdateSkillIndicator(Vector2 input, int skillIndex)
    {
        int index = skillIndex - 1;
        if (index < 0 || index >= directionSpritePrefabs.Count)
        {
            if (indicatorInstance != null) indicatorInstance.SetActive(false);
            currentIndicatorIndex = -1;
            return;
        }

        if (currentIndicatorIndex != index)
        {
            if (indicatorInstance != null) Destroy(indicatorInstance);
            indicatorInstance = Instantiate(directionSpritePrefabs[index], transform.position, Quaternion.identity);
            currentIndicatorIndex = index;
        }

        indicatorInstance.SetActive(true);
        Vector3 direction = new Vector3(input.x, input.y, 0f).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float offsetAngle = skillAngleOffsets.Count > index ? skillAngleOffsets[index] : 0f;
        indicatorInstance.transform.rotation = Quaternion.Euler(0f, 0f, angle + offsetAngle);

        float dist = distancesFromPlayer.Count > index ? distancesFromPlayer[index] : 0f;
        float backOffset = spriteBackOffsets.Count > index ? spriteBackOffsets[index] : 0f;
        Vector3 basePos = transform.position + direction * dist;
        Vector3 offset = -indicatorInstance.transform.up * backOffset;
        indicatorInstance.transform.position = basePos + offset;
    }

    public void OnSkillButtonPressed()
    {
        if (joystickCanvasGroup != null) joystickCanvasGroup.alpha = 1f;
        int currentDice = currentDiceResult;

        //if (indicatorInstance != null) Destroy(indicatorInstance);
        currentIndicatorIndex = -1;

        SkillType currentSkill = GetMappedSkillType(currentDice);
        int prefabIndex = (int)currentSkill - 1;
        if (prefabIndex >= 0 && prefabIndex < directionSpritePrefabs.Count)
            SetupIndicator(prefabIndex);
    }

    void SetupIndicator(int prefabIndex)
    {
        if (indicatorInstance != null) Destroy(indicatorInstance);
        if (directionSpritePrefabs.Count > prefabIndex)
        {
            indicatorInstance = Instantiate(directionSpritePrefabs[prefabIndex], transform.position, Quaternion.identity);
            currentIndicatorIndex = prefabIndex;
        }
    }

    public void OnSkillButtonReleased()
    {
        if (hasUsedSkill) return;

        if (joystickCanvasGroup != null)
            joystickCanvasGroup.alpha = 0f;

        SkillType skill = GetMappedSkillType(currentDiceResult);

        switch (skill)
        {
            case SkillType.SlimeJump:
                UseSlimeJump();
                break;
            default:
                Debug.Log("해당 스킬은 아직 구현되지 않았습니다.");
                break;
        }

        hasUsedSkill = true;
        OnSkillUsed();
    }

    private void UseSlimeJump()
    {
        transform.DOKill();
        isUsingSkill = true;

        AudioManager.Instance.PlaySFX(AudioManager.Instance.jumpSound);
        Vector3 jumpDirection = new Vector3(lastInputDirection.x, lastInputDirection.y, 0).normalized;
        float jumpDistance = distancesFromPlayer.Count > 3 ? distancesFromPlayer[3] : 3f;
        Vector3 targetPos = transform.position + jumpDirection * jumpDistance;

        // --- 착지 위치 표시 (프리팹 크기 기준으로 radius 보정) ---
        int index = (int)SkillType.SlimeJump - 1;
        if (landingIndicatorInstance != null) Destroy(landingIndicatorInstance);

        if (index >= 0 && index < directionSpritePrefabs.Count)
        {
            landingIndicatorInstance = Instantiate(directionSpritePrefabs[index], targetPos, Quaternion.identity);

            // SpriteRenderer 기준 월드 단위 스케일 계산
            SpriteRenderer sr = landingIndicatorInstance.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                float spriteDiameter = Mathf.Max(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y);
                float scaleFactor = slimeJumpRadius * 2f / spriteDiameter; // 반지름 -> 직경
                landingIndicatorInstance.transform.localScale = sr.transform.localScale * scaleFactor;
            }
            landingIndicatorInstance.SetActive(true);
        }

        float jumpPower = 1.5f;
        int jumpCount = 1;
        Sequence jumpSeq = DOTween.Sequence();
        jumpSeq.Append(transform.DOJump(targetPos, jumpPower, jumpCount, 0.7f).SetEase(Ease.InOutQuad));
        jumpSeq.Join(transform.DOScale(originalScale * 1.5f, 0.35f).SetEase(Ease.OutQuad));
        jumpSeq.Join(transform.DOScale(originalScale, 0.35f).SetDelay(0.35f).SetEase(Ease.InQuad));
        jumpSeq.OnComplete(() =>
        {
            transform.position = targetPos;
            transform.localScale = originalScale;

            if (slimeJumpLandEffectPrefab != null)
            {
                GameObject effect = Instantiate(slimeJumpLandEffectPrefab, targetPos, Quaternion.identity);
                Destroy(effect, 0.3f);
            }
            AudioManager.Instance.PlaySFX(AudioManager.Instance.land);
            DealSlimeJumpDamage(targetPos);

            // --- 착지 표시 제거 ---
            if (landingIndicatorInstance != null) Destroy(landingIndicatorInstance);

            StartCoroutine(EndSkillAfterDelay(1f));
        });
    }

    private IEnumerator EndSkillAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        isUsingSkill = false;
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
                    // 1. 데미지 부여
                    EnemyHP enemyhp = enemy.GetComponent<EnemyHP>();
                    if (enemyhp != null)
                    {
                        enemyhp.SkillTakeDamage((int)slimeJumpDamage);
                    }

                    // 2. Dotween으로 부드럽게 밀려남 (위로 살짝 점프 효과)
                    Vector3 dir = (enemy.transform.position - position).normalized;
                    Vector3 knockbackPos = enemy.transform.position + dir * knockbackDistance;

                    // Dotween: enemy.transform이 knockbackPos로 점프 처럼 이동
                    enemy.transform.DOKill(); // 기존 트윈 중단
                    enemy.transform.DOJump(knockbackPos, knockbackJumpPower, 1, knockbackTime)
                        .SetEase(Ease.OutQuad);
                }
            }
        }
    }


    void DrawArc(Vector3 from, Vector3 to, float arcHeight = 2f, int segmentCount = 30)
    {
        if (arcLine == null) return;

        arcLine.positionCount = segmentCount + 1;
        for (int i = 0; i <= segmentCount; i++)
        {
            float t = (float)i / segmentCount;
            Vector3 pos = Vector3.Lerp(from, to, t);
            pos.y += Mathf.Sin(Mathf.PI * t) * arcHeight;
            pos.z = 0f;  // 반드시 카메라 앞쪽 등 적절히 고정
            arcLine.SetPosition(i, pos);
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

                if (CooltimeImange != null)
                    CooltimeImange.fillAmount = waitTime / waitInterval;

                yield return null;
            }

            hasUsedSkill = false;

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

    public void ForceStopRolling()
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, slimeJumpRadius);
    }
}
