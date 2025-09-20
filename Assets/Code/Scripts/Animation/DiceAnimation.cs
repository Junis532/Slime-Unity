using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class DiceAnimation : MonoBehaviour
{
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
    }

    void OnEnable() => StartRollingLoop();
    void OnDisable() => StopRollingLoop();

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

            while (elapsed < rollDuration)
            {
                elapsed += frameRate;
                yield return new WaitForSeconds(frameRate);
            }

            if (!hasUsedSkill)
            {
                noSkillUseCount = Mathf.Min(noSkillUseCount + 1, 4);
            }

            isRolling = false;

            if (waitTimerText != null)
                waitTimerText.text = "";
        }
    }

    public void StartRollingLoop()
    {
        if (rollCoroutine == null)
        {
            rollCoroutine = StartCoroutine(RollingLoopRoutine());
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
