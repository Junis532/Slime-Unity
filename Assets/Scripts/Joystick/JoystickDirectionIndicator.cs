using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SkillNumber.Skills; // SkillType enum이 이 네임스페이스에 정의돼 있다고 가정

public class JoystickDirectionIndicator : MonoBehaviour
{
    [Header("조이스틱 및 UI 요소")]
    public VariableJoystick joystick;
    public CanvasGroup joystickCanvasGroup;
    public GameObject imageToHideWhenTouching;
    public GameObject blockInputCanvas;

    [Header("스킬 관련 프리팹 및 위치")]
    public List<GameObject> directionSpritePrefabs;
    public List<float> distancesFromPlayer;
    public List<float> spriteBackOffsets;
    public List<float> skillAngleOffsets;

    public GameObject fireballPrefab;
    public Transform firePoint;
    public GameObject lightningPrefab;
    public GameObject LightningEffectPrefab;
    public GameObject windWallPrefab;

    private GameObject indicatorInstance;
    private int currentIndicatorIndex = -1;
    private PlayerController playerController;

    private bool isTouchingJoystick, wasTouchingJoystickLastFrame;
    private Vector2 lastInputDirection = Vector2.right;
    private float lastInputMagnitude = 0f;
    private bool hasUsedSkill = false;
    private bool isLightningMode = false;
    private Vector3 lightningTargetPosition;
    private Vector2 lightningCastDirection;
    private bool prevBlockInputActive = false;

    private AudioSource audioSource;
    public AudioClip fireballSound;
    public AudioClip lightningSound;
    public AudioClip windWallSound;

    [Header("스킬 쿨타임 관련")]
    public TMP_Text waitTimerText;
    public Image CooltimeImange;
    public int waitInterval = 10;

    private Coroutine rollCoroutine;

    public static bool isRolling = false;
    public static int currentDiceResult = 0;

    void Start()
    {
        playerController = GetComponent<PlayerController>();
        if (joystickCanvasGroup != null) joystickCanvasGroup.alpha = 0f;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        currentDiceResult = 1;
        StartRollingLoop();
    }

    void Update()
    {
        bool isBlockActive = blockInputCanvas != null && blockInputCanvas.activeSelf;

        if (prevBlockInputActive && !isBlockActive)
        {
            ResetInputStates();
        }
        prevBlockInputActive = isBlockActive;

        if (isBlockActive || currentDiceResult <= 0)
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

            if (currentSkill == SkillType.Lightning && isLightningMode)
                UpdateLightningIndicator(input);
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
            currentIndicatorIndex = -1;
        }

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
        isLightningMode = false;
        currentIndicatorIndex = -1;

        if (indicatorInstance != null) Destroy(indicatorInstance);
        if (joystickCanvasGroup != null) joystickCanvasGroup.alpha = 0f;
        if (joystick != null) { joystick.ResetInput(); joystick.enabled = true; }
    }

    void DisableInputAndIndicators()
    {
        if (joystick != null) joystick.enabled = false;
        SetHideImageState(true);
        if (indicatorInstance != null) indicatorInstance.SetActive(false);
        currentIndicatorIndex = -1;
        isLightningMode = false;
        if (joystickCanvasGroup != null) joystickCanvasGroup.alpha = 0f;
    }

    void SetHideImageState(bool isVisible) => imageToHideWhenTouching?.SetActive(isVisible);

    SkillType GetMappedSkillType(int diceResult)
    {
        switch (diceResult)
        {
            case 1: return SkillType.Fireball;
            case 2: return SkillType.Lightning;
            case 3: return SkillType.Windwall;
            default: return SkillType.None;
        }
    }

    void UpdateSkillIndicator(Vector2 input, int skillIndex)
    {
        int index = skillIndex - 1;
        if (index < 0 || index >= directionSpritePrefabs.Count) { indicatorInstance?.SetActive(false); currentIndicatorIndex = -1; return; }

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

    void UpdateLightningIndicator(Vector2 input)
    {
        int index = (int)SkillType.Lightning - 1;
        float maxDist = distancesFromPlayer[index];
        Vector3 direction = new Vector3(input.x, input.y, 0f).normalized;
        Vector3 basePos = transform.position + direction * maxDist * Mathf.Clamp01(input.magnitude);
        lightningTargetPosition = basePos;
        if (indicatorInstance == null) return;
        float offset = spriteBackOffsets[index];
        indicatorInstance.transform.position = basePos - indicatorInstance.transform.up * offset;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        indicatorInstance.transform.rotation = Quaternion.Euler(0f, 0f, angle + skillAngleOffsets[index]);
        indicatorInstance.SetActive(true);
    }

    public void OnSkillButtonPressed()
    {
        if (joystickCanvasGroup != null) joystickCanvasGroup.alpha = 1f;
        int currentDice = currentDiceResult;

        if (indicatorInstance != null) Destroy(indicatorInstance);
        currentIndicatorIndex = -1;

        SkillType currentSkill = GetMappedSkillType(currentDice);
        int prefabIndex = (int)currentSkill - 1;
        if (prefabIndex >= 0 && prefabIndex < directionSpritePrefabs.Count)
            SetupIndicator(prefabIndex);

        isLightningMode = currentSkill == SkillType.Lightning;
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
            case SkillType.Fireball:
                ShootFireball();
                break;
            case SkillType.Lightning:
                if (isLightningMode)
                {
                    lightningCastDirection = lastInputDirection;
                    CastLightning(lightningTargetPosition);
                    isLightningMode = false;
                }
                break;
            case SkillType.Windwall:
                SpawnWindWall();
                break;
            default:
                Debug.Log("해당 스킬은 아직 구현되지 않았습니다.");
                break;
        }

        hasUsedSkill = true;
        OnSkillUsed();
    }

    private void ShootFireball()
    {
        if (fireballPrefab == null || firePoint == null) return;

        audioSource?.PlayOneShot(fireballSound);
        GameObject obj = Instantiate(fireballPrefab, firePoint.position, Quaternion.identity);
        obj.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(lastInputDirection.y, lastInputDirection.x) * Mathf.Rad2Deg);
        obj.GetComponent<FireballProjectile>()?.Init(lastInputDirection);
    }

    private void CastLightning(Vector3 targetPos)
    {
        StartCoroutine(LightningStrikeSequence(targetPos));
    }

    private IEnumerator LightningStrikeSequence(Vector3 targetPos)
    {
        int count = 3;
        float onTime = 0.2f, offTime = 0.23f;
        float angle = Mathf.Atan2(lightningCastDirection.y, lightningCastDirection.x) * Mathf.Rad2Deg;
        GameObject lightning = null;
        LightningDamage ld = null;

        for (int i = 0; i < count; i++)
        {
            float fallDelay = 0f;
            if (LightningEffectPrefab != null)
            {
                Vector3 start = targetPos + Vector3.up * 5f;
                GameObject effect = Instantiate(LightningEffectPrefab, start, Quaternion.identity);
                var fallScript = effect.GetComponent<FallingLightningEffect>();
                if (fallScript != null)
                {
                    fallScript.targetPosition = targetPos;
                    fallDelay = Vector3.Distance(start, targetPos) / fallScript.fallSpeed;
                }
            }
            yield return new WaitForSeconds(fallDelay);

            if (lightning == null)
            {
                lightning = Instantiate(lightningPrefab, targetPos, Quaternion.Euler(0f, 0f, angle));
                ld = lightning.GetComponent<LightningDamage>();
            }
            else
            {
                lightning.transform.SetPositionAndRotation(targetPos, Quaternion.Euler(0f, 0f, angle));
                lightning.SetActive(true);
            }
            ld?.Init();

            audioSource?.PlayOneShot(lightningSound);

            yield return new WaitForSeconds(onTime);
            lightning?.SetActive(false);
            yield return new WaitForSeconds(offTime);
        }
        if (lightning != null) Destroy(lightning);
    }

    private void SpawnWindWall()
    {
        if (windWallPrefab == null) return;

        audioSource?.PlayOneShot(windWallSound);
        GameObject wall = Instantiate(windWallPrefab, transform.position, Quaternion.identity);
        wall.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(lastInputDirection.y, lastInputDirection.x) * Mathf.Rad2Deg);
    }

    IEnumerator RollingLoopRoutine()
    {
        while (true)
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
}
