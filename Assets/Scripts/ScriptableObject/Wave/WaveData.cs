using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WaveData : ScriptableObject
{
    [Header("몬스터 프리팹 리스트")]
    public List<GameObject> MonsterLists;

    [Header("각 몬스터 스폰 대기 시간 (초)")]
    public List<float> spawnDelays; // MonsterLists와 인덱스 동일하게 맞춰야 함

    [Header("맵 프리팹")]
    public GameObject mapPrefab;

    [Header("상점 맵 여부")]
    public bool isShopMap = false;

    [Header("이벤트 스테이지 여부")]
    public bool isEventStageBuff;
    public bool isEventStageDebuff;
}
