using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class PlayerDie : MonoBehaviour
{
    private bool isDead = false;

    [Header("죽음 패널 관련")]
    public RectTransform deathPanel;            // 죽음 UI 패널
    public CanvasGroup deathCanvasGroup;        // 죽음 UI 페이드용

    void Update()
    {
        if (!isDead && GameManager.Instance.playerStats.currentHP <= 0)
        {
            isDead = true;
            PlayDeathSequence();
        }
    }

    public void PlayDeathSequence()
    {
        Sequence deathSequence = DOTween.Sequence();

        Vector3 backwardDir = -transform.right * 0.5f;

        deathSequence.Append(transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack));
        deathSequence.Join(transform.DOMove(transform.position + backwardDir, 0.5f).SetEase(Ease.OutQuad));
        deathSequence.Join(transform.DORotate(new Vector3(0, 0, 360 * 3), 0.5f, RotateMode.FastBeyond360));

        deathSequence.OnComplete(() =>
        {
            Destroy(gameObject);

            // 죽음 UI 등장 애니메이션
            if (deathCanvasGroup != null && deathPanel != null)
            {
                deathCanvasGroup.alpha = 0f;

                // 패널 이동
                deathPanel.DOAnchorPosY(0f, 0.7f).SetEase(Ease.OutCubic)
                    .OnComplete(() =>
                    {
                        // 위치 애니메이션 끝난 뒤 상태 변경
                        GameManager.Instance.ChangeStateToEnd();
                    });

                // 페이드 인
                deathCanvasGroup.DOFade(1f, 0.7f);
            }
            else
            {
                // UI가 연결되지 않았으면 바로 상태 전환
                GameManager.Instance.ChangeStateToEnd();
            }
        });
    }
}
