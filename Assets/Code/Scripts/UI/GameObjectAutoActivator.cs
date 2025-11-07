using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameObjectSetting
{
    public GameObject targetObject;  // 대상 오브젝트
    public bool startActive;         // 시작 시 활성화 여부
}

public class GameObjectAutoActivator : MonoBehaviour
{
    [Header("자동 활성화/비활성화할 오브젝트 리스트")]
    public List<GameObjectSetting> objectSettings;

    void Start()
    {
        foreach (var setting in objectSettings)
        {
            if (setting.targetObject != null)
            {
                setting.targetObject.SetActive(setting.startActive);
            }
        }
    }
}
