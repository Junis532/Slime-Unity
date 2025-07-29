using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SkillNumber.Skills; // Keep this if SkillType enum is defined here

public class JoystickDirectionIndicator3 : MonoBehaviour
{
    [Header("조이스틱 및 UI 요소")]
    public VariableJoystick joystick;
    public CanvasGroup joystickCanvasGroup;
    public GameObject imageToHideWhenTouching;
    public Image diceImage;
    public GameObject blockInputCanvas;
    public Button skillSaveButton;

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
    public GameObject teleportEffectPrefab; // Still present but commented out in use
    public float teleportEffectDuration = 1f; // Still present but commented out in use
    public GameObject bombPrefab; // Still present but not used
    public GameObject mucusProjectilePrefab; // Still present but not used

    private GameObject indicatorInstance;
    private int currentIndicatorIndex = -1;
    private PlayerController playerController;
    private bool isTouchingJoystick, wasTouchingJoystickLastFrame;
    private Vector2 lastInputDirection = Vector2.right;
    private float lastInputMagnitude = 0f;
    private bool hasUsedSkill = false, prevIsRolling = false;
    private bool isLightningMode = false; // isTeleportMode might not be strictly needed now but kept for consistency with original script
    private Vector3 lightningTargetPosition;
    private Vector2 lightningCastDirection;
    private bool prevBlockInputActive = false;

    [Header("오디오")]
    private AudioSource audioSource;
    public AudioClip fireballSound;
    //public AudioClip teleportSound; // Commented out based on previous script's teleport functionality removal
    public AudioClip lightningSound;
    public AudioClip windWallSound;

    // SkillType enum should be defined in SkillNumber.Skills namespace or a global namespace
    // For demonstration, if it's not globally accessible, you might need to define it here:
    // public enum SkillType
    // {
    //     None = 0,
    //     Fireball = 1,
    //     Lightning = 2,
    //     Windwall = 3,
    //     Teleport = 4, // Keep if other parts of the project still reference it
    //     // Add other skills as needed
    // }

    // CurrentUsingSkillIndex and CurrentUsingSkillType are now simplified
    public int CurrentUsingSkillIndex
    {
        get
        {
            int diceValue = DiceAnimation.currentDiceResult;
            if (diceValue <= 0 || hasUsedSkill) return 0;
            // Directly map dice value to skill index (1, 2, 3 for Fireball, Lightning, Windwall)
            if (diceValue >= 1 && diceValue <= 3)
            {
                return diceValue;
            }
            return 0; // Return 0 for unmapped or invalid dice results
        }
    }

    public SkillType CurrentUsingSkillType
    {
        get
        {
            int diceValue = DiceAnimation.currentDiceResult;
            if (diceValue <= 0 || hasUsedSkill) return SkillType.None;
            // Directly map dice value to SkillType
            switch (diceValue)
            {
                case 1: return SkillType.Fireball;
                case 2: return SkillType.Lightning;
                case 3: return SkillType.Windwall;
                default: return SkillType.None;
            }
        }
    }

    void Start()
    {
        playerController = GetComponent<PlayerController>();
        if (joystickCanvasGroup != null) joystickCanvasGroup.alpha = 0f;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Update()
    {
        SetDiceImageAlpha(1f);

        bool isBlockActive = blockInputCanvas != null && blockInputCanvas.activeSelf;

        skillSaveButton.interactable = !(hasUsedSkill || DiceAnimation.isRolling);

        if (prevBlockInputActive && !isBlockActive)
        {
            ResetInputStates();
        }
        prevBlockInputActive = isBlockActive;

        if (prevIsRolling && !DiceAnimation.isRolling)
        {
            ResetInputStates();
        }

        if (isBlockActive || DiceAnimation.currentDiceResult <= 0)
        {
            DisableInputAndIndicators();
            return;
        }

        Vector2 input = (joystick != null) ? new Vector2(joystick.Horizontal, joystick.Vertical) : playerController.InputVector;
        isTouchingJoystick = input.magnitude > 0.2f;
        SetHideImageState(!isTouchingJoystick);

        if (prevIsRolling && !DiceAnimation.isRolling)
        {
            hasUsedSkill = false;
            //isTeleportMode = false;
            isLightningMode = false;
            DiceAnimation.hasUsedSkill = false;
        }
        prevIsRolling = DiceAnimation.isRolling;

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
            SkillType currentSkill = GetMappedSkillType(DiceAnimation.currentDiceResult);

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
            joystick.enabled = !DiceAnimation.isRolling && !hasUsedSkill;
    }

    void ResetInputStates()
    {
        isTouchingJoystick = false;
        wasTouchingJoystickLastFrame = false;
        lastInputDirection = Vector2.right;
        lastInputMagnitude = 0f;
        hasUsedSkill = false;
        //isTeleportMode = false;
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
        //isTeleportMode = false;
        isLightningMode = false;
        if (joystickCanvasGroup != null) joystickCanvasGroup.alpha = 0f;
    }

    void SetHideImageState(bool isVisible) => imageToHideWhenTouching?.SetActive(isVisible);

    void SetDiceImageAlpha(float alpha)
    {
        if (diceImage != null)
        {
            Color c = diceImage.color;
            c.a = alpha;
            diceImage.color = c;
        }
    }

    // Modified to directly map dice result to SkillType
    SkillType GetMappedSkillType(int diceResult)
    {
        if (diceResult <= 0)
            return SkillType.None;

        switch (diceResult)
        {
            case 1: return SkillType.Fireball;
            case 2: return SkillType.Lightning;
            case 3: return SkillType.Windwall;
            default: return SkillType.None; // For any other dice result
        }
    }

    void UpdateSkillIndicator(Vector2 input, int skillIndex)
    {
        // Adjust skillIndex to be 0-based for list access
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
        int index = (int)SkillType.Lightning - 1; // Assuming Lightning is an enum value
        // Ensure index is within bounds for distancesFromPlayer and spriteBackOffsets
        if (index < 0 || index >= distancesFromPlayer.Count || index >= spriteBackOffsets.Count || index >= skillAngleOffsets.Count)
        {
            // Handle error or use default values if index is out of bounds
            Debug.LogWarning("Lightning indicator arrays out of bounds. Using default values.");
            index = 0; // Use a default index or handle as an error
        }

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
        int currentDiceResult = DiceAnimation.currentDiceResult;

        if (indicatorInstance != null) Destroy(indicatorInstance);
        currentIndicatorIndex = -1;

        SkillType currentSkill = GetMappedSkillType(currentDiceResult);
        int prefabIndex = (int)currentSkill - 1; // Convert SkillType enum to 0-based index for prefab lists
        if (prefabIndex >= 0 && prefabIndex < directionSpritePrefabs.Count)
            SetupIndicator(prefabIndex);

        // Retain Teleport and Lightning mode flags for their specific indicator updates
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

        int skillDiceValue = DiceAnimation.currentDiceResult;

        SkillType skill = GetMappedSkillType(skillDiceValue);

        switch (skill)
        {
            case SkillType.Fireball:
                ShootFireball();
                break;

            // Teleport case is commented out based on your original script's commented out section.
            // If you wish to re-enable it, uncomment this section and the TeleportPlayer method.
            // case SkillType.Teleport:
            //    if (isTeleportMode)
            //    {
            //        TeleportPlayer(teleportTargetPosition);
            //        isTeleportMode = false;
            //    }
            //    break;

            case SkillType.Lightning:
                if (isLightningMode) // Ensure it's in lightning mode to cast
                {
                    lightningCastDirection = lastInputDirection; // Capture direction at release
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

        GameManager.Instance.diceAnimation.OnSkillUsed();
    }

    private void ShootFireball()
    {
        if (fireballPrefab == null || firePoint == null) return;

        if (fireballSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(fireballSound);
        }

        GameObject obj = Instantiate(fireballPrefab, firePoint.position, Quaternion.identity);
        obj.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(lastInputDirection.y, lastInputDirection.x) * Mathf.Rad2Deg);
        obj.GetComponent<FireballProjectile>()?.Init(lastInputDirection);
    }

    // private void TeleportPlayer(Vector3 targetPos)
    // {
    //     if (teleportEffectPrefab != null)
    //     {
    //         GameObject effect = Instantiate(teleportEffectPrefab, targetPos, Quaternion.identity);
    //         Destroy(effect, teleportEffectDuration);
    //     }
    //     transform.position = targetPos;

    //     if (teleportSound != null && audioSource != null)
    //         audioSource.PlayOneShot(teleportSound);
    // }

    private void CastLightning(Vector3 targetPos)
    {
        StartCoroutine(LightningStrikeSequence(targetPos));
    }

    private IEnumerator LightningStrikeSequence(Vector3 targetPos)
    {
        int count = 3;
        float onTime = 0.2f, offTime = 0.23f;
        // Use lightningCastDirection captured at release, not lastInputDirection
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

            if (audioSource != null && lightningSound != null)
            {
                audioSource.PlayOneShot(lightningSound);
            }

            yield return new WaitForSeconds(onTime);
            lightning?.SetActive(false);
            yield return new WaitForSeconds(offTime);
        }
        if (lightning != null) Destroy(lightning);
    }

    private void SpawnWindWall()
    {
        if (windWallPrefab == null) return;

        if (windWallSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(windWallSound);
        }

        GameObject wall = Instantiate(windWallPrefab, transform.position, Quaternion.identity);
        wall.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(lastInputDirection.y, lastInputDirection.x) * Mathf.Rad2Deg);
    }
}