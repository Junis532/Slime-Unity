using System.Collections.Generic;
using UnityEngine;

public class UIListAutoActivate : MonoBehaviour
{
    // Inspector 창에서 연결할 UI 패널 리스트
    public List<GameObject> uiPanels;

    void Start()
    {
        // 리스트에 있는 모든 UI 오브젝트를 활성화
        foreach (GameObject panel in uiPanels)
        {
            if (panel != null)
            {
                panel.SetActive(true);
            }
        }
    }
}
