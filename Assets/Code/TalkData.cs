using UnityEngine;
using System.Collections.Generic;
using System;

[CreateAssetMenu(fileName = "TalkData", menuName = "TalkDatable/CreateTalk")]
public class TalkData : ScriptableObject
{
    [Header("대화 내용")]
    public List<TalkLine> talks;  // <-- 여기에 구조체 리스트 사용
}

[Serializable]
public struct TalkLine
{
    [Header("대화 문장")]
    [Multiline]
    public string talkString;
}
