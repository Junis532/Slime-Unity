using TMPro;
using UnityEngine;
using DG.Tweening;

public class DamageText : MonoBehaviour
{
    public TMP_Text text;
    public float floatHeight = 1f;
    public float duration = 0.7f;

    public void SetDamage(int damage, Vector3 worldPosition)
    {
        text.text = damage.ToString();

        // 스폰 위치 = world → screen 변환
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPosition);
        transform.position = screenPos;

        // 초기 상태 설정
        transform.localScale = Vector3.one * 1f;
        text.color = new Color(1f, 0f, 0f, 1f); // 빨간색

        // DOTween 애니메이션 (떠오르기 + 페이드아웃)
        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOMoveY(transform.position.y + floatHeight, duration).SetEase(Ease.OutCubic));
        seq.Join(text.DOFade(0f, duration));
        seq.OnComplete(() =>
        {
            gameObject.SetActive(false); // 풀로 반환되도록 비활성화
        });
    }
}
