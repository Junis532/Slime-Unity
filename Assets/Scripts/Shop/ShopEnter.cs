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
            if (shopUI != null)
            {

                Canvas canvas = shopUI.GetComponent<Canvas>();
                if (canvas != null)
                {
                    canvas.sortingOrder = -1;  // 정렬 순서를 0으로 설정
                }
            }

        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("플레이어가 상점 영역에 진입함. 상점 상태로 변경합니다.");
            DialogManager.Instance.StartShopDialog();

            //waveManager.StopSpawnLoop();

            if (shopPanel != null)
            {

                CanvasGroup canvasGroup = shopPanel.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.DOFade(1f, 0.7f);  // 0f = 완전 투명, 0.5초 동안
                }
                shopPanel.DOAnchorPosY(0f, 0.7f);

                if (shopUI != null)
                {

                    Canvas canvas = shopUI.GetComponent<Canvas>();
                    if (canvas != null)
                    {
                        canvas.sortingOrder = 10;  // 정렬 순서를 0으로 설정
                    }
                }

                GameManager.Instance.shopManager.FirstRerollItems();
                GameManager.Instance.playerController.canMove = false;
            }
        }
    }
}
