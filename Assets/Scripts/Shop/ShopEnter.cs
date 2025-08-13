using DG.Tweening;
using UnityEngine;

public class ShopEnter : MonoBehaviour
{
    [Header("UI")]
    public GameObject shopUI;
    [Header("상점 패널")]
    public RectTransform shopPanel;

    private bool hasTriggeredThisShop = false; // 이번 상점 세션에서 이미 실행했는지 여부

    private void Start()
    {
        if (shopUI != null)
        {
            CanvasGroup canvasGroup = shopPanel.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            if (shopUI != null)
            {
                Canvas canvas = shopUI.GetComponent<Canvas>();
                if (canvas != null)
                {
                    canvas.sortingOrder = -1;
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 이미 이번 세션에서 실행했다면 무시
        if (hasTriggeredThisShop) return;

        if (other.CompareTag("Player"))
        {
            hasTriggeredThisShop = true; // 첫 실행 이후 잠금

            Debug.Log("플레이어가 상점 영역에 진입함. 상점 상태로 변경합니다.");
            //DialogManager.Instance.StartShopDialog();

            if (shopPanel != null)
            {
                CanvasGroup canvasGroup = shopPanel.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.DOFade(1f, 0.7f);
                }
                shopPanel.DOAnchorPosY(0f, 0.7f);

                if (shopUI != null)
                {
                    Canvas canvas = shopUI.GetComponent<Canvas>();
                    if (canvas != null)
                    {
                        canvas.sortingOrder = 10;
                    }
                }

                GameManager.Instance.shopManager.FirstRerollItems();
                GameManager.Instance.playerController.canMove = false;
            }
        }
    }

    // 상점이 닫히고 다시 열릴 수 있도록 플래그 초기화
    public void ResetTrigger()
    {
        hasTriggeredThisShop = false;
    }
}
