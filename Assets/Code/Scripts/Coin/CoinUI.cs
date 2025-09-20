using UnityEngine;
using TMPro;

public class CoinUI : MonoBehaviour
{
    public TextMeshProUGUI coinText;
    public TextMeshProUGUI shopCoinText; // 추가된 텍스트 필드

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.playerStats != null)
        {
            coinText.text = $"{GameManager.Instance.playerStats.coin}";
            shopCoinText.text = $"{GameManager.Instance.playerStats.coin}";
        }
    }
}
