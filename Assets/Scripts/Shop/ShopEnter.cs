using DG.Tweening;
using UnityEngine;

public class ShopEnter : MonoBehaviour
{
    [Header("UI")]
    public GameObject shopUI;

    [Header("상점 패널")]
    public RectTransform shopPanel;

    private void Start()
    {
        if (shopUI != null)
        {
            CanvasGroup canvasGroup = shopPanel.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            shopUI.SetActive(false);

        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("플레이어가 상점 영역에 진입함. 상점 상태로 변경합니다.");
            DialogManager.Instance.StartShopDialog();

            //waveManager.StopSpawnLoop();

            if (shopUI != null)
            {
                shopUI.SetActive(true);
            }

            if (shopPanel != null)
            {

                CanvasGroup canvasGroup = shopPanel.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.DOFade(1f, 0.7f);  // 0f = 완전 투명, 0.5초 동안
                }
                shopPanel.DOAnchorPosY(0f, 0.7f);
                GameManager.Instance.shopManager.FirstRerollItems();
                GameManager.Instance.playerController.canMove = false;
            }
        }
    }
}
