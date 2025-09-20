using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHP : MonoBehaviour
{
    [Header("플레이어 체력바 (Filled 이미지)")]
    public Image hpFillImage;  // Slider 대신 Image 사용

    [Header("체력 숫자 표시 (예: 35 / 100)")]
    public TMP_Text hpText;

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.playerStats != null)
        {
            float currentHP = GameManager.Instance.playerStats.currentHP;
            float maxHP = GameManager.Instance.playerStats.maxHP;

            // FillAmount는 0 ~ 1 사이의 값이므로 비율로 설정
            hpFillImage.fillAmount = currentHP / maxHP;

            hpText.text = $"{(int)currentHP}"; // 체력 숫자 표시
            //// 텍스트 업데이트
            //hpText.text = $"{(int)currentHP} / {(int)maxHP}";
        }
    }
}
