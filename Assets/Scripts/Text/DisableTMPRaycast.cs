using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DisableTMPRaycast : MonoBehaviour
{
    void Awake()
    {
        // 현재 오브젝트와 자식에서 모든 Graphic 컴포넌트 가져오기
        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);

        foreach (Graphic g in graphics)
        {
            g.raycastTarget = false; // 클릭 막힘 방지
        }
    }
}
