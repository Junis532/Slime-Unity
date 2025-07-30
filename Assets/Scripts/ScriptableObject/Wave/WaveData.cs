using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WaveData : ScriptableObject
{
    [Header("∏ÛΩ∫≈Õ «¡∏Æ∆’ ∏ÆΩ∫∆Æ")]
    public List<GameObject> MonsterLists;

    [Header("∏  «¡∏Æ∆’")]
    public GameObject mapPrefab;

    [Header("ªÛ¡° ∏  ø©∫Œ")]
    public bool isShopMap = false;
}

