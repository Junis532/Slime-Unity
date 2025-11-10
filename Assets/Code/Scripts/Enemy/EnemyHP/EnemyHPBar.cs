using UnityEngine;
using UnityEngine.UI;

public class EnemyHPBar : MonoBehaviour
{
    public Slider hpSlider;
    private Transform target; // 따라갈 대상(적)
    private Vector3 offset = new Vector3(0, 0.7f, 0); // HP바 위치 오프셋

    public void Init(Transform target, float maxHP)
    {
        this.target = target;
        hpSlider.maxValue = maxHP;
        hpSlider.value = maxHP;
    }

    public void SetHP(float hp)
    {
        hpSlider.value = hp;
    }

    void LateUpdate()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }
        // 월드 위치 → 캔버스 위치 변환
        Vector3 worldPos = target.position + offset;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        transform.position = screenPos;
    }
}
