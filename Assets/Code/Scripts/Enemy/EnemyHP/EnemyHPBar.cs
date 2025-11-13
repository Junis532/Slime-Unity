using UnityEngine;
using UnityEngine.UI;

public class EnemyHPBar : MonoBehaviour
{
    public Slider hpSlider;
    private Transform target; // 따라갈 대상(적)
    private Vector3 offset = new Vector3(0, 0.7f, 0); // HP바 위치 오프셋

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        rectTransform.localScale = Vector3.one; // 항상 1,1,1
    }

    public void Init(Transform target, float maxHP)
    {
        this.target = target;
        hpSlider.maxValue = maxHP;
        hpSlider.value = maxHP;
        gameObject.SetActive(true);
    }

    public void SetHP(float hp)
    {
        hpSlider.value = hp;
    }

    void LateUpdate()
    {
        if (target == null)
        {
            hpSlider.gameObject.SetActive(false);
            return;
        }

        Vector3 worldPos = target.position + offset;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

        if (screenPos.z < 0)
            hpSlider.gameObject.SetActive(false);
        else
        {
            hpSlider.gameObject.SetActive(true);
            rectTransform.position = screenPos;
            rectTransform.localScale = Vector3.one;
        }
    }
}
