//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;
//using SkillNumber.Skills;

//public class JoystickDirectionIndicator3 : MonoBehaviour
//{
//    [Header("조이스틱 및 UI 요소")]
//    public VariableJoystick joystick;
//    public CanvasGroup joystickCanvasGroup;
//    public GameObject imageToHideWhenTouching;
//    public Image diceImage;
//    public GameObject blockInputCanvas;
//    public Button skillSaveButton;

//    [Header("스킬 관련 프리팹 및 위치")]
//    public List<GameObject> directionSpritePrefabs;
//    public List<float> distancesFromPlayer;
//    public List<float> spriteBackOffsets;
//    public List<float> skillAngleOffsets;

//    public GameObject fireballPrefab;
//    public Transform firePoint;
//    public GameObject lightningPrefab;
//    public GameObject LightningEffectPrefab;
//    public GameObject windWallPrefab;
//    public GameObject teleportEffectPrefab;
//    public float teleportEffectDuration = 1f;
//    public GameObject bombPrefab;
//    public GameObject mucusProjectilePrefab;

//    private GameObject indicatorInstance;
//    private int currentIndicatorIndex = -1;
//    private PlayerController playerController;
//    private bool isTouchingJoystick, wasTouchingJoystickLastFrame;
//    private Vector2 lastInputDirection = Vector2.right;
//    private float lastInputMagnitude = 0f;
//    private bool hasUsedSkill = false, prevIsRolling = false;
//    private bool isTeleportMode = false, isLightningMode = false;
//    private Vector3 teleportTargetPosition, lightningTargetPosition;
//    private Vector2 lightningCastDirection;
//    private bool prevBlockInputActive = false;

//    [Header("오디오")]
//    private AudioSource audioSource;
//    public AudioClip fireballSound;
//    //public AudioClip teleportSound;
//    public AudioClip lightningSound;
//    public AudioClip windWallSound;



//    // 현재 사용 중인 스킬(플레이어가 dice로 발동한)의 "스킬번호" (1~8)
//    public int CurrentUsingSkillIndex
//    {
//        get
//        {
//            int diceValue = DiceAnimation.currentDiceResult;
//            if (diceValue <= 0 || hasUsedSkill) return 0;
//            if (SkillSelect.FinalSkillOrder == null || SkillSelect.FinalSkillOrder.Count < diceValue)
//                return 0;
//            return SkillSelect.FinalSkillOrder[diceValue - 1];
//        }
//    }

//    // 선택적 SkillType 반환(사용처에 따라)
//    public SkillType CurrentUsingSkillType
//    {
//        get
//        {
//            int diceValue = DiceAnimation.currentDiceResult;
//            if (diceValue <= 0 || hasUsedSkill) return SkillType.None;
//            if (SkillSelect.FinalSkillOrder == null || SkillSelect.FinalSkillOrder.Count < diceValue)
//                return SkillType.None;
//            int mappedSkillNumber = SkillSelect.FinalSkillOrder[diceValue - 1];
//            return (SkillType)mappedSkillNumber;
//        }
//    }

//    void Start()
//    {
//        playerController = GetComponent<PlayerController>();
//        if (joystickCanvasGroup != null) joystickCanvasGroup.alpha = 0f;

//        audioSource = GetComponent<AudioSource>();
//        if (audioSource == null)
//            audioSource = gameObject.AddComponent<AudioSource>();
//    }

//    void Update()
//    {
//        SetDiceImageAlpha(1f);

//        bool isBlockActive = blockInputCanvas != null && blockInputCanvas.activeSelf;

//        skillSaveButton.interactable = !(hasUsedSkill || DiceAnimation.isRolling);

//        if (prevBlockInputActive && !isBlockActive)
//        {
//            ResetInputStates();
//        }
//        prevBlockInputActive = isBlockActive;

//        if (prevIsRolling && !DiceAnimation.isRolling)
//        {
//            ResetInputStates();
//        }


//        if (isBlockActive || DiceAnimation.currentDiceResult <= 0)
//        {
//            DisableInputAndIndicators();
//            return;
//        }


//        Vector2 input = (joystick != null) ? new Vector2(joystick.Horizontal, joystick.Vertical) : playerController.InputVector;
//        isTouchingJoystick = input.magnitude > 0.2f;
//        SetHideImageState(!isTouchingJoystick);

//        if (prevIsRolling && !DiceAnimation.isRolling)
//        {
//            hasUsedSkill = false;
//            isTeleportMode = false;
//            isLightningMode = false;
//            DiceAnimation.hasUsedSkill = false;
//        }
//        prevIsRolling = DiceAnimation.isRolling;

//        if (hasUsedSkill)
//        {
//            DisableInputAndIndicators();
//            return;
//        }

//        if (joystickCanvasGroup != null)
//            joystickCanvasGroup.alpha = isTouchingJoystick ? 1f : 0f;

//        if (isTouchingJoystick)
//        {
//            lastInputDirection = input.normalized;
//            lastInputMagnitude = input.magnitude;
//            SkillType currentSkill = GetMappedSkillType(DiceAnimation.currentDiceResult);

//            if (currentSkill == SkillType.Teleport && isTeleportMode)
//                UpdateTeleportIndicator(input);
//            else if (currentSkill == SkillType.Lightning && isLightningMode)
//                UpdateLightningIndicator(input);
//            //else if (currentSkill == SkillType.Mucus && isMucusMode)
//            //    UpdateMucusIndicator(input);
//            else
//            {
//                OnSkillButtonPressed();
//                UpdateSkillIndicator(input, (int)currentSkill);
//            }
//        }
//        else
//        {
//            if (wasTouchingJoystickLastFrame && !hasUsedSkill && lastInputMagnitude > 0.3f)
//            {
//                OnSkillButtonReleased();
//                hasUsedSkill = true;
//            }

//            if (indicatorInstance != null)
//                indicatorInstance.SetActive(false);
//            currentIndicatorIndex = -1;
//        }

//        wasTouchingJoystickLastFrame = isTouchingJoystick;
//        if (joystick != null)
//            joystick.enabled = !DiceAnimation.isRolling && !hasUsedSkill;
//    }

//    void ResetInputStates()
//    {
//        isTouchingJoystick = false;
//        wasTouchingJoystickLastFrame = false;
//        lastInputDirection = Vector2.right;
//        lastInputMagnitude = 0f;
//        hasUsedSkill = false;
//        isTeleportMode = false;
//        isLightningMode = false;
//        currentIndicatorIndex = -1;

//        if (indicatorInstance != null) Destroy(indicatorInstance);
//        if (joystickCanvasGroup != null) joystickCanvasGroup.alpha = 0f;
//        if (joystick != null) { joystick.ResetInput(); joystick.enabled = true; }
//    }

//    void DisableInputAndIndicators()
//    {
//        if (joystick != null) joystick.enabled = false;
//        SetHideImageState(true);
//        if (indicatorInstance != null) indicatorInstance.SetActive(false);
//        currentIndicatorIndex = -1;
//        isTeleportMode = false;
//        isLightningMode = false;
//        if (joystickCanvasGroup != null) joystickCanvasGroup.alpha = 0f;
//    }

//    void SetHideImageState(bool isVisible) => imageToHideWhenTouching?.SetActive(isVisible);

//    void SetDiceImageAlpha(float alpha)
//    {
//        if (diceImage != null)
//        {
//            Color c = diceImage.color;
//            c.a = alpha;
//            diceImage.color = c;
//        }
//    }

//    SkillType GetMappedSkillType(int diceResult)
//    {
//        if (SkillSelect.FinalSkillOrder == null || SkillSelect.FinalSkillOrder.Count < diceResult || diceResult <= 0)
//            return SkillType.None;
//        int mappedSkillNumber = SkillSelect.FinalSkillOrder[diceResult - 1];
//        return (SkillType)mappedSkillNumber;
//    }

//    void UpdateSkillIndicator(Vector2 input, int skillIndex)
//    {
//        int index = skillIndex - 1;
//        if (index < 0 || index >= directionSpritePrefabs.Count) { indicatorInstance?.SetActive(false); currentIndicatorIndex = -1; return; }

//        if (currentIndicatorIndex != index)
//        {
//            if (indicatorInstance != null) Destroy(indicatorInstance);
//            indicatorInstance = Instantiate(directionSpritePrefabs[index], transform.position, Quaternion.identity);
//            currentIndicatorIndex = index;
//        }

//        indicatorInstance.SetActive(true);
//        Vector3 direction = new Vector3(input.x, input.y, 0f).normalized;
//        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
//        float offsetAngle = skillAngleOffsets.Count > index ? skillAngleOffsets[index] : 0f;
//        indicatorInstance.transform.rotation = Quaternion.Euler(0f, 0f, angle + offsetAngle);

//        float dist = distancesFromPlayer.Count > index ? distancesFromPlayer[index] : 0f;
//        float backOffset = spriteBackOffsets.Count > index ? spriteBackOffsets[index] : 0f;
//        Vector3 basePos = transform.position + direction * dist;
//        Vector3 offset = -indicatorInstance.transform.up * backOffset;
//        indicatorInstance.transform.position = basePos + offset;
//    }

//    void UpdateTeleportIndicator(Vector2 input)
//    {
//        int index = (int)SkillType.Teleport - 1;
//        float maxDist = distancesFromPlayer.Count > index ? distancesFromPlayer[index] : 5f;
//        Vector3 direction = new Vector3(input.x, input.y, 0f).normalized;
//        Vector3 basePos = transform.position + direction * maxDist * Mathf.Clamp01(input.magnitude);
//        teleportTargetPosition = basePos;
//        if (indicatorInstance == null) return;
//        float offset = spriteBackOffsets.Count > index ? spriteBackOffsets[index] : 0f;
//        indicatorInstance.transform.position = basePos - indicatorInstance.transform.up * offset;
//        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
//        indicatorInstance.transform.rotation = Quaternion.Euler(0f, 0f, angle + skillAngleOffsets[index]);
//        indicatorInstance.SetActive(true);
//    }

//    void UpdateLightningIndicator(Vector2 input)
//    {
//        int index = (int)SkillType.Lightning - 1;
//        float maxDist = distancesFromPlayer.Count > index ? distancesFromPlayer[index] : 5f;
//        Vector3 direction = new Vector3(input.x, input.y, 0f).normalized;
//        Vector3 basePos = transform.position + direction * maxDist * Mathf.Clamp01(input.magnitude);
//        lightningTargetPosition = basePos;
//        if (indicatorInstance == null) return;
//        float offset = spriteBackOffsets.Count > index ? spriteBackOffsets[index] : 0f;
//        indicatorInstance.transform.position = basePos - indicatorInstance.transform.up * offset;
//        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
//        indicatorInstance.transform.rotation = Quaternion.Euler(0f, 0f, angle + skillAngleOffsets[index]);
//        indicatorInstance.SetActive(true);
//    }



//    public void OnSkillButtonPressed()
//    {
//        if (joystickCanvasGroup != null) joystickCanvasGroup.alpha = 1f;
//        int currentDiceResult = DiceAnimation.currentDiceResult;

//        if (indicatorInstance != null) Destroy(indicatorInstance);
//        currentIndicatorIndex = -1;

//        SkillType currentSkill = GetMappedSkillType(currentDiceResult);
//        int prefabIndex = (int)currentSkill - 1;
//        if (prefabIndex >= 0 && prefabIndex < directionSpritePrefabs.Count)
//            SetupIndicator(prefabIndex);

//        isTeleportMode = currentSkill == SkillType.Teleport;
//        isLightningMode = currentSkill == SkillType.Lightning;
//    }

//    void SetupIndicator(int prefabIndex)
//    {
//        if (indicatorInstance != null) Destroy(indicatorInstance);
//        if (directionSpritePrefabs.Count > prefabIndex)
//        {
//            indicatorInstance = Instantiate(directionSpritePrefabs[prefabIndex], transform.position, Quaternion.identity);
//            currentIndicatorIndex = prefabIndex;
//        }
//    }

//    public void OnSkillButtonReleased()
//    {
//        if (hasUsedSkill) return;

//        if (joystickCanvasGroup != null)
//            joystickCanvasGroup.alpha = 0f;

//        int skillDiceValue = DiceAnimation.currentDiceResult;
//        int effectDiceValue;

//        if (!DiceAnimation.isRolling && DiceAnimation.hasUsedSkill)
//        {
//            effectDiceValue = DiceAnimation.noSkillUseCount;
//            Debug.Log($"[세이브 주사위 사용] 부가 효과 주사위 눈금: {effectDiceValue}");
//        }
//        else
//        {
//            effectDiceValue = skillDiceValue;
//            Debug.Log($"[일반 주사위 사용] 눈금: {effectDiceValue}");
//        }

//        SkillType skill = GetMappedSkillType(skillDiceValue);

//        switch (skill)
//        {
//            case SkillType.Fireball:
//                ShootFireball();
//                break;

//            //case SkillType.Teleport:
//            //    if (isTeleportMode)
//            //    {
//            //        TeleportPlayer(teleportTargetPosition);
//            //        isTeleportMode = false;
//            //    }
//            //    break;

//            case SkillType.Lightning:
//                if (isLightningMode)
//                {
//                    lightningCastDirection = lastInputDirection;
//                    CastLightning(lightningTargetPosition);
//                    isLightningMode = false;
//                }
//                break;

//            case SkillType.Windwall:
//                SpawnWindWall();
//                break;

//            default:
//                Debug.Log("해당 스킬은 아직 구현되지 않았습니다.");
//                break;
//        }

//        hasUsedSkill = true;

//        //GameManager.Instance.diceAnimation.ExecuteSkillEffect(effectDiceValue);

//        GameManager.Instance.diceAnimation.OnSkillUsed();
//    }

//    private void ShootFireball()
//    {
//        if (fireballPrefab == null || firePoint == null) return;

//        // Play the Fireball sound if assigned
//        if (fireballSound != null && audioSource != null)
//        {
//            audioSource.PlayOneShot(fireballSound);
//        }

//        GameObject obj = Instantiate(fireballPrefab, firePoint.position, Quaternion.identity);
//        obj.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(lastInputDirection.y, lastInputDirection.x) * Mathf.Rad2Deg);
//        obj.GetComponent<FireballProjectile>()?.Init(lastInputDirection);
//    }

//    //private void TeleportPlayer(Vector3 targetPos)
//    //{
//    //    if (teleportEffectPrefab != null)
//    //    {
//    //        GameObject effect = Instantiate(teleportEffectPrefab, targetPos, Quaternion.identity);
//    //        Destroy(effect, teleportEffectDuration);
//    //    }
//    //    transform.position = targetPos;

//    //    if (teleportSound != null && audioSource != null)
//    //        audioSource.PlayOneShot(teleportSound);
//    //}

//    private void CastLightning(Vector3 targetPos)
//    {
//        StartCoroutine(LightningStrikeSequence(targetPos));
//    }

//    private IEnumerator LightningStrikeSequence(Vector3 targetPos)
//    {
//        int count = 3;
//        float onTime = 0.2f, offTime = 0.23f;
//        float angle = Mathf.Atan2(lightningCastDirection.y, lightningCastDirection.x) * Mathf.Rad2Deg;
//        GameObject lightning = null;
//        LightningDamage ld = null;

//        for (int i = 0; i < count; i++)
//        {
//            float fallDelay = 0f;
//            if (LightningEffectPrefab != null)
//            {
//                Vector3 start = targetPos + Vector3.up * 5f;
//                GameObject effect = Instantiate(LightningEffectPrefab, start, Quaternion.identity);
//                var fallScript = effect.GetComponent<FallingLightningEffect>();
//                if (fallScript != null)
//                {
//                    fallScript.targetPosition = targetPos;
//                    fallDelay = Vector3.Distance(start, targetPos) / fallScript.fallSpeed;
//                }
//            }
//            yield return new WaitForSeconds(fallDelay);

//            if (lightning == null)
//            {
//                lightning = Instantiate(lightningPrefab, targetPos, Quaternion.Euler(0f, 0f, angle));
//                ld = lightning.GetComponent<LightningDamage>();
//            }
//            else
//            {
//                lightning.transform.SetPositionAndRotation(targetPos, Quaternion.Euler(0f, 0f, angle));
//                lightning.SetActive(true);
//            }
//            ld?.Init();

//            // 번개 이펙트 시작할 때 효과음 재생
//            if (audioSource != null && lightningSound != null)
//            {
//                audioSource.PlayOneShot(lightningSound);
//            }

//            yield return new WaitForSeconds(onTime);
//            lightning?.SetActive(false);
//            yield return new WaitForSeconds(offTime);
//        }
//        if (lightning != null) Destroy(lightning);
//    }

//    private void SpawnWindWall()
//    {
//        if (windWallPrefab == null) return;

//        // Play the Wind Wall sound if assigned
//        if (windWallSound != null && audioSource != null)
//        {
//            audioSource.PlayOneShot(windWallSound);
//        }

//        GameObject wall = Instantiate(windWallPrefab, transform.position, Quaternion.identity);
//        wall.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(lastInputDirection.y, lastInputDirection.x) * Mathf.Rad2Deg);
//    }
//}