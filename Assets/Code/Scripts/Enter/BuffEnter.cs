using DG.Tweening;
using UnityEngine;

public class BuffEnter : MonoBehaviour
{
    [Header("UI")]
    public GameObject buffUI;

    [Header("버프 패널")]
    public RectTransform buffPanel;

    private bool hasTriggeredThisBuff = false; // 이번 버프 세션에서 이미 실행했는지 여부
    private BuffEvent buffEvent; // BuffEvent 참조
    private CanvasGroup canvasGroup; // 페이드 제어용

    private void Start()
    {
        // ✅ GameManager에서 직접 가져오기
        buffEvent = GameManager.Instance?.buffEvent;
        if (buffEvent == null)
        {
            Debug.LogError("⚠️ GameManager.Instance.buffEvent가 할당되지 않았습니다!");
        }

        // CanvasGroup 캐싱
        if (buffPanel != null)
        {
            canvasGroup = buffPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = buffPanel.gameObject.AddComponent<CanvasGroup>();

            // 시작은 투명 + 화면 위쪽
            canvasGroup.alpha = 0f;
            buffPanel.anchoredPosition = new Vector2(buffPanel.anchoredPosition.x, 1080f);
        }

        // 정렬 순서 초기화 (UI 숨기기)
        if (buffUI != null)
        {
            Canvas canvas = buffUI.GetComponent<Canvas>();
            if (canvas != null)
                canvas.sortingOrder = -1;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 이미 이번 세션에서 실행했다면 무시
        if (hasTriggeredThisBuff) return;

        if (other.CompareTag("Player"))
        {
            hasTriggeredThisBuff = true; // 첫 실행 이후 잠금

            Debug.Log("플레이어가 버프 영역에 진입함. 버프 상태로 변경합니다.");

            OpenPanelAnimation();

            // 🔹 GameManager에서 불러온 buffEvent 실행
            if (buffEvent != null)
            {
                buffEvent.OpenPanel();
            }
            else
            {
                Debug.LogWarning("⚠️ GameManager.Instance.buffEvent가 연결되지 않았습니다!");
            }

            // 🔹 플레이어 이동 잠금
            if (GameManager.Instance?.playerController != null)
                GameManager.Instance.playerController.canMove = false;
        }
    }

    /// <summary>
    /// 패널 열기 애니메이션 (Y=0, 페이드인)
    /// </summary>
    private void OpenPanelAnimation()
    {
        if (buffPanel == null || canvasGroup == null) return;

        // 정렬 순서 올리기
        if (buffUI != null)
        {
            Canvas canvas = buffUI.GetComponent<Canvas>();
            if (canvas != null)
                canvas.sortingOrder = 10;
        }

        // 열릴 때 애니메이션
        buffPanel.DOKill();
        canvasGroup.DOKill();

        buffPanel.DOAnchorPosY(0f, 0.7f).SetEase(Ease.OutCubic);
        canvasGroup.DOFade(1f, 0.7f);
    }

    /// <summary>
    /// 패널 닫기 애니메이션 (Y=1080, 페이드아웃)
    /// </summary>
    public void ClosePanelAnimation()
    {
        if (buffPanel == null || canvasGroup == null) return;

        buffPanel.DOKill();
        canvasGroup.DOKill();

        buffPanel.DOAnchorPosY(1080f, 0.7f).SetEase(Ease.InBack);
        canvasGroup.DOFade(0f, 0.7f);

        // 닫힌 후 정렬 순서 낮추기
        DOVirtual.DelayedCall(0.7f, () =>
        {
            if (buffUI != null)
            {
                Canvas canvas = buffUI.GetComponent<Canvas>();
                if (canvas != null)
                    canvas.sortingOrder = -1;
            }
        });
    }

    /// <summary>
    /// 버프가 닫히고 다시 열릴 수 있도록 플래그 초기화
    /// </summary>
    public void ResetTrigger()
    {
        hasTriggeredThisBuff = false;
    }
}
