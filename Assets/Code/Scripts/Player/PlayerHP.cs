using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHP : MonoBehaviour
{
    [Header("플레이어 체력바 (Filled 이미지)")]
    public Image hpFillImage;  // 즉시 변경되는 체력바
    public Image hpFillImage2; // 추가 체력바

    [Header("지연 체력바 (천천히 감소하는 연출용)")]
    public Image hpDelayFillImage;  // 천천히 감소하는 체력바
    public Image hpDelayFillImage2; // 추가 지연 체력바

    [Header("체력 숫자 표시 (예: 35 / 100)")]
    public TMP_Text hpText;

    [Header("지연 체력바 설정")]
    [SerializeField] private float delayTime = 1f;     // 몇 초 후에 감소 시작할지
    [SerializeField] private float delaySpeed = 2f;    // 지연 체력바 감소 속도

    private float targetFillAmount;  // 목표 fillAmount 값
    private float currentDelayFillAmount;  // 현재 지연 체력바 fillAmount
    private float delayTimer;  // 지연 시간을 측정하는 타이머
    private bool shouldStartDelay;  // 지연 감소를 시작해야 하는지 여부

    void Start()
    {
        // 초기값 설정
        if (GameManager.Instance != null && GameManager.Instance.playerStats != null)
        {
            float initialRatio = GameManager.Instance.playerStats.currentHP / GameManager.Instance.playerStats.maxHP;
            targetFillAmount = initialRatio;
            currentDelayFillAmount = initialRatio;
            
            if (hpDelayFillImage != null)
                hpDelayFillImage.fillAmount = initialRatio;
            if (hpDelayFillImage2 != null)
                hpDelayFillImage2.fillAmount = initialRatio;
        }
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.playerStats != null)
        {
            float currentHP = GameManager.Instance.playerStats.currentHP;
            float maxHP = GameManager.Instance.playerStats.maxHP;
            float currentRatio = currentHP / maxHP;

            // 즉시 변경되는 체력바 업데이트
            if (hpFillImage != null)
                hpFillImage.fillAmount = currentRatio;
            if (hpFillImage2 != null)
                hpFillImage2.fillAmount = currentRatio;

            // 목표값이 변경되었는지 확인
            if (targetFillAmount != currentRatio)
            {
                targetFillAmount = currentRatio;
                
                // 체력이 감소한 경우에만 지연 타이머 시작
                if (currentDelayFillAmount > targetFillAmount)
                {
                    shouldStartDelay = true;
                    delayTimer = 0f;
                }
                else
                {
                    // 체력이 회복된 경우 즉시 따라감
                    shouldStartDelay = false;
                    currentDelayFillAmount = targetFillAmount;
                }
            }

            // 지연 체력바 업데이트
            if (hpDelayFillImage != null || hpDelayFillImage2 != null)
            {
                if (shouldStartDelay && currentDelayFillAmount > targetFillAmount)
                {
                    // 지연 시간 카운트
                    delayTimer += Time.deltaTime;
                    
                    // 지연 시간이 지나면 감소 시작
                    if (delayTimer >= delayTime)
                    {
                        currentDelayFillAmount = Mathf.MoveTowards(currentDelayFillAmount, targetFillAmount, delaySpeed * Time.deltaTime);
                        
                        // 목표값에 도달하면 지연 상태 해제
                        if (Mathf.Approximately(currentDelayFillAmount, targetFillAmount))
                        {
                            shouldStartDelay = false;
                        }
                    }
                }
                
                if (hpDelayFillImage != null)
                    hpDelayFillImage.fillAmount = currentDelayFillAmount;
                if (hpDelayFillImage2 != null)
                    hpDelayFillImage2.fillAmount = currentDelayFillAmount;
            }

            // 체력 텍스트 업데이트
            if (hpText != null)
                hpText.text = $"{(int)currentHP}";
        }
    }
}
