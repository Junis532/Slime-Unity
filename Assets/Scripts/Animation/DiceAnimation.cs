using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class DiceAnimation : MonoBehaviour
{
    [Header("사용 가능 주사위 이미지 (0~3 인덱스: 1~4 숫자에 대응)")]
    public List<Sprite> diceSprites;

    [Header("스킬 이미지 (1: 파이어볼, 2: 번개, 3: 방어막)")]
    public Sprite fireballSkillSprite;
    public Sprite lightningSkillSprite;
    public Sprite windwallSkillSprite;

    [Header("조이스틱 관련")]
    public CanvasGroup joystickCanvasGroup;
    public VariableJoystick joystick;

    [Header("스킬 이미지 표시용 UI")]
    public Image skillImage;

    [Header("대기 시간 표시용 텍스트 이미지")]
    public TMP_Text waitTimerText;
    public Image CooltimeImange;

    public float frameRate = 0.05f;
    public float rollDuration = 3f;
    public int waitInterval = 10;

    private Image image;
    private Coroutine rollCoroutine;

    public static bool isRolling = false;
    public static int currentDiceResult = 0;

    public static bool hasUsedSkill = false;
    public static int noSkillUseCount = 1;

    void Start()
    {
        image = GetComponent<Image>();

        if (image == null)
        {
            Debug.LogError("❌ Image 컴포넌트가 없습니다!");
            return;
        }

        if (diceSprites == null || diceSprites.Count == 0)
        {
            Debug.LogError("❌ diceSprites가 설정되지 않았거나 비어 있습니다!");
            return;
        }

        RollOnceAtStart();
    }

    void OnEnable() => StartRollingLoop();
    void OnDisable() => StopRollingLoop();

    public void RollOnceAtStart()
    {
        StartCoroutine(RollOnceCoroutine());
    }

    private IEnumerator RollOnceCoroutine()
    {
        isRolling = true;
        float elapsed = 0f;
        int animFrame = 0;

        while (elapsed < rollDuration)
        {
            image.sprite = diceSprites[animFrame];
            animFrame = (animFrame + 1) % diceSprites.Count;
            elapsed += frameRate;
            yield return new WaitForSeconds(frameRate);
        }

        int result = Random.Range(1, 4);  // 1~3
        currentDiceResult = result;
        image.sprite = diceSprites[result - 1];

        UpdateSkillImage();

        isRolling = false;
        if (waitTimerText != null)
            waitTimerText.text = "";
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

            isRolling = true;
            float elapsed = 0f;
            int animFrame = 0;

            while (elapsed < rollDuration)
            {
                image.sprite = diceSprites[animFrame];
                animFrame = (animFrame + 1) % diceSprites.Count;
                elapsed += frameRate;
                yield return new WaitForSeconds(frameRate);
            }

            UpdateSkillImage();

            if (!hasUsedSkill)
            {
                noSkillUseCount = Mathf.Min(noSkillUseCount + 1, 4);
            }

            isRolling = false;

            if (waitTimerText != null)
                waitTimerText.text = "";
        }
    }

    void UpdateSkillImage()
    {
        if (skillImage == null) return;

        switch (currentDiceResult)
        {
            case 1:
                skillImage.sprite = fireballSkillSprite;
                skillImage.enabled = true;
                break;
            case 2:
                skillImage.sprite = lightningSkillSprite;
                skillImage.enabled = true;
                break;
            case 3:
                skillImage.sprite = windwallSkillSprite;
                skillImage.enabled = true;
                break;
            default:
                skillImage.enabled = false;
                break;
        }
    }

    public void StartRollingLoop()
    {
        RollOnceAtStart();

        if (rollCoroutine == null)
        {
            rollCoroutine = StartCoroutine(RollingLoopRoutine());

            if (currentDiceResult > 0 && currentDiceResult <= diceSprites.Count)
            {
                image.sprite = diceSprites[currentDiceResult - 1];
            }
        }
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
        hasUsedSkill = true;

        if (rollCoroutine != null)
        {
            StopCoroutine(rollCoroutine);
            rollCoroutine = null;
            isRolling = false;

            if (waitTimerText != null)
                waitTimerText.text = "";
        }
    }

    public void OnSkillUsed()
    {
        hasUsedSkill = false;

        if (rollCoroutine != null)
        {
            StopCoroutine(rollCoroutine);
            rollCoroutine = null;
        }

        rollCoroutine = StartCoroutine(RollingLoopRoutine());
    }
}
