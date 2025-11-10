using UnityEngine;
using System.Collections.Generic;
using System;

[CreateAssetMenu(fileName = "TalkData", menuName = "TalkDatable/CreateTalk")]
public class TalkData : ScriptableObject
{
    [Header("대화 내용")]
    public List<TalkLine> talks = new List<TalkLine>();
}

[Serializable]
public struct TalkLine
{
    [Header("대사")]
    [Multiline]
    public string talkString;

    [Header("초상화/스프라이트 (선택)")]
    public Sprite talkSprite; // ← 이게 비어있지 않고, 현재 것과 다르면 교체 + ‘뽁?’ 애니메이션
}
