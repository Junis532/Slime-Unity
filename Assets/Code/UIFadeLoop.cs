using UnityEngine;
using UnityEngine.UI;
using DG.Tweening; // DOTween 사용 (필수!)

public class UIFadeLoop : MonoBehaviour
{
    public Graphic targetGraphic;            // Image, Text, TMP_Text 등
    public float fadeDuration = 1.0f;        // 페이드 인/아웃 시간
    public float minAlpha = 0.3f;            // 최소 투명도
    public float maxAlpha = 1.0f;            // 최대 투명도

    private void Start()
    {
        if (targetGraphic == null)
            targetGraphic = GetComponent<Graphic>();

        StartFadeLoop();
    }

    private void StartFadeLoop()
    {
        targetGraphic.DOFade(minAlpha, fadeDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }
}
