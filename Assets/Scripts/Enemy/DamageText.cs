using TMPro;
using UnityEngine;
using DG.Tweening;

public class DamageText : MonoBehaviour
{
    public TMP_Text text;

    public void Show(int damage)
    {
        if (text == null)
            text = GetComponentInChildren<TMP_Text>();

        text.text = damage.ToString();
        transform.localScale = Vector3.zero;

        // 크기 및 위치 애니메이션
        transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
        transform.DOMoveY(transform.position.y + 1.5f, 1f) // 정상 작동

            .SetEase(Ease.OutQuad)
            .OnComplete(() => gameObject.SetActive(false));
    }
}
