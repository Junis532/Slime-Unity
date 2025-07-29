//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;

//public class SkillSelect : MonoBehaviour
//{
//    public Image[] bottomSlots;   // 하단 선택 슬롯 (8개)
//    public Image[] topSlots;      // 상단 배치 슬롯 (4개)
//    public Sprite[] skillSprites; // 스킬 이미지 (0: 파이어볼, 1: 텔레포트, 2: 번개, 3: 윈드월)
//    public Image[] resetSlots;    // 초기화용 스프라이트
//    public Button resetButton;
//    public Button confirmButton;

//    private List<int> selectedIndices = new(); // 선택된 하단 슬롯 인덱스
//    private int currentIndex = 0;

//    // 최종 스킬 순서(상단 슬롯 인덱스, 1-based) 저장
//    public static List<int> FinalSkillOrder = new List<int>() { 1, 2, 3, 4 };
//    // 최종 스킬 이미지 저장용 추가
//    public static List<Sprite> FinalSkillSprites = new List<Sprite>();

//    void OnEnable()
//    {
//        Time.timeScale = 0f;

//        resetButton.onClick.AddListener(ResetSelection);
//        confirmButton.onClick.AddListener(ConfirmSelection);

//        for (int i = 0; i < bottomSlots.Length; i++)
//        {
//            int index = i;
//            bottomSlots[i].GetComponent<Button>().onClick.AddListener(() => SelectSkill(index));
//        }

//        SetupBottomSlots();
//        ResetSelection();
//    }

//    void OnDisable()
//    {
//        Time.timeScale = 1f;

//        resetButton.onClick.RemoveAllListeners();
//        confirmButton.onClick.RemoveAllListeners();

//        for (int i = 0; i < bottomSlots.Length; i++)
//        {
//            bottomSlots[i].GetComponent<Button>().onClick.RemoveAllListeners();
//        }
//    }

//    void SelectSkill(int index)
//    {
//        if (currentIndex >= topSlots.Length || selectedIndices.Contains(index))
//            return;

//        topSlots[currentIndex].sprite = bottomSlots[index].sprite;
//        selectedIndices.Add(index);
//        currentIndex++;
//    }

//    void ResetSelection()
//    {
//        for (int i = 0; i < topSlots.Length; i++)
//        {
//            if (i < resetSlots.Length && resetSlots[i] != null)
//                topSlots[i].sprite = resetSlots[i].sprite;
//            else
//                topSlots[i].sprite = null;
//        }

//        selectedIndices.Clear();
//        currentIndex = 0;
//    }

//    void ConfirmSelection()
//    {
//        // 선택된 스킬이 4개가 아니면 리턴
//        if (selectedIndices.Count < topSlots.Length)
//        {
//            Debug.LogWarning("스킬 4개를 모두 선택해야 합니다.");
//            return;
//        }

//        Debug.Log("저장된 스킬 인덱스(하단 슬롯 번호):");
//        foreach (var idx in selectedIndices)
//        {
//            Debug.Log(idx + 1);
//        }

//        // 최종 스킬 순서 저장 (1-based 인덱스)
//        FinalSkillOrder = new List<int>();
//        FinalSkillSprites = new List<Sprite>();

//        foreach (int idx in selectedIndices)
//        {
//            FinalSkillOrder.Add(idx + 1);                 // 1-based 인덱스 저장
//            FinalSkillSprites.Add(bottomSlots[idx].sprite); // 이미지도 저장
//        }

//        gameObject.SetActive(false);
//    }


//    void SetupBottomSlots()
//    {
//        for (int i = 0; i < bottomSlots.Length; i++)
//        {
//            if (i >= 0 && i < skillSprites.Length)
//                bottomSlots[i].sprite = skillSprites[i];
//            else
//                bottomSlots[i].sprite = null;
//        }
//    }

//    // 주사위 결과에 따라 하단 슬롯 이미지 업데이트 (옵션)
//    public void ApplyDiceResult(int result)
//    {
//        if (result >= 1 && result <= 4 && skillSprites.Length >= result)
//        {
//            bottomSlots[result - 1].sprite = skillSprites[result - 1];
//        }
//    }

//    public Sprite[] GetSelectedSkillSprites()
//    {
//        Sprite[] selected = new Sprite[topSlots.Length];
//        for (int i = 0; i < topSlots.Length; i++)
//        {
//            selected[i] = topSlots[i].sprite;
//        }
//        return selected;
//    }

//    /// <summary>
//    /// 위쪽 슬롯에 저장된 스킬의 하단 슬롯 번호(1~4)를 배열로 반환.
//    /// 못 찾으면 0으로 채움.
//    /// </summary>
//    public int[] GetTopSlotSkillCaseIndices()
//    {
//        int[] results = new int[topSlots.Length];

//        for (int i = 0; i < topSlots.Length; i++)
//        {
//            results[i] = -1; // 초기값

//            Sprite topSprite = topSlots[i].sprite;
//            if (topSprite == null) continue;

//            for (int j = 0; j < skillSprites.Length; j++)
//            {
//                if (skillSprites[j] == topSprite)
//                {
//                    results[i] = j + 1; // case 번호는 1부터 시작
//                    break;
//                }
//            }
//        }

//        return results;
//    }

//    /// <summary>
//    /// 스킬 고유 번호(1~4)를 키로, 그 스킬이 배치된 상단 슬롯 번호(1~4)를 값으로 리턴
//    /// </summary>
//    public Dictionary<int, int> GetSkillToTopSlotIndexMapping()
//    {
//        Dictionary<int, int> mapping = new();

//        for (int i = 0; i < topSlots.Length; i++)
//        {
//            Sprite topSprite = topSlots[i].sprite;
//            if (topSprite == null) continue;

//            for (int skillId = 0; skillId < skillSprites.Length; skillId++)
//            {
//                if (skillSprites[skillId] == topSprite)
//                {
//                    mapping[skillId + 1] = i + 1; // 1-based 상단 슬롯 번호
//                    break;
//                }
//            }
//        }

//        return mapping;
//    }
//}
