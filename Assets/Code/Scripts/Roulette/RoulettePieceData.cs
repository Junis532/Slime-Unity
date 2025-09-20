using UnityEngine;

[System.Serializable]
public class RoulettePieceData
{
    public Sprite icon; // 룰렛 조각의 아이콘 이미지
    //public string description; // 룰렛 조각의 설명 텍스트

    // 3개의 아이템 등장확률(chance)이 100, 60, 40이면
    // 등장확률의 합은 200. 100/200 = 50%, 60/200 = 30%, 40/200 = 20%가 됨.

    [Range(1, 100)]
    public int chance = 100;

    [HideInInspector]
    public int index; // 룰렛 조각의 인덱스 (자동 할당)
    [HideInInspector]
    public int weight; // 룰렛 조각의 가중치 최소값 (자동 할당)
}
