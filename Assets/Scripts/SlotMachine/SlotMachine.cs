//using DG.Tweening;
//using System.Collections;
//using System.Collections.Generic;
//using TMPro;
//using UnityEngine;
//using UnityEngine.UI;

//public class SlotMachine : MonoBehaviour
//{
//    [Header("UI 슬롯 (루트 오브젝트)")]
//    public GameObject[] itemSlots;       // Slot1~3 루트 오브젝트

//    [Header("아이템 데이터 (ShopManager에서 가져옴)")]
//    public List<ItemStats> allItems;

//    private ItemStats[] selectedItems = new ItemStats[3];

//    void Start()
//    {
//        for (int i = 0; i < itemSlots.Length; i++)
//        {
//            int idx = i;

//            // 자식 BuyButton 가져오기
//            Button buyBtn = itemSlots[i].transform.Find("BuyButton").GetComponent<Button>();
//            buyBtn.onClick.AddListener(() => OnSelectItem(idx));
//            buyBtn.interactable = false;

//            // 초기 상태: Icon 스케일 0, 투명 / 텍스트 투명
//            Image icon = itemSlots[i].transform.Find("ItemIcon").GetComponent<Image>();
//            icon.transform.localScale = Vector3.zero;
//            SetAlpha(icon, 0f);

//            TMP_Text nameText = itemSlots[i].transform.Find("ItemName").GetComponent<TMP_Text>();
//            TMP_Text priceText = itemSlots[i].transform.Find("ItemPrice").GetComponent<TMP_Text>();
//            TMP_Text descText = itemSlots[i].transform.Find("ItemDescription").GetComponent<TMP_Text>();

//            SetAlpha(nameText, 0f);
//            SetAlpha(priceText, 0f);
//            SetAlpha(descText, 0f);
//        }

//        // 테스트용: 게임 시작 시 바로 보여주기
//        ShowItemChoices();
//    }

//    public void ShowItemChoices()
//    {
//        if (allItems.Count < 3)
//        {
//            Debug.LogWarning("allItems에 3개 이상의 아이템이 필요합니다!");
//            return;
//        }

//        gameObject.SetActive(true);
//        StartCoroutine(RollItemsCoroutine());
//    }

//    private IEnumerator RollItemsCoroutine()
//    {
//        float duration = 1f;
//        float timer = 0f;

//        // 회전 중: 아이콘만 랜덤 표시
//        while (timer < duration)
//        {
//            for (int i = 0; i < 3; i++)
//            {
//                int rand = Random.Range(0, allItems.Count);
//                Image icon = itemSlots[i].transform.Find("ItemIcon").GetComponent<Image>();
//                icon.sprite = allItems[rand].icon;
//                icon.transform.localScale = Vector3.one;
//                SetAlpha(icon, 1f);
//            }

//            timer += 0.1f;
//            yield return new WaitForSeconds(0.1f);
//        }

//        // 최종 아이템 결정
//        List<ItemStats> tempList = new List<ItemStats>(allItems);
//        for (int i = 0; i < 3; i++)
//        {
//            int rand = Random.Range(0, tempList.Count);
//            selectedItems[i] = tempList[rand];
//            tempList.RemoveAt(rand);

//            Image icon = itemSlots[i].transform.Find("ItemIcon").GetComponent<Image>();
//            icon.sprite = selectedItems[i].icon;
//            icon.transform.localScale = Vector3.zero;
//            SetAlpha(icon, 0f);

//            TMP_Text nameText = itemSlots[i].transform.Find("ItemName").GetComponent<TMP_Text>();
//            TMP_Text priceText = itemSlots[i].transform.Find("ItemPrice").GetComponent<TMP_Text>();
//            TMP_Text descText = itemSlots[i].transform.Find("ItemDescription").GetComponent<TMP_Text>();

//            nameText.text = selectedItems[i].itemName;
//            priceText.text = selectedItems[i].price.ToString();
//            descText.text = selectedItems[i].description;

//            SetAlpha(nameText, 0f);
//            SetAlpha(priceText, 0f);
//            SetAlpha(descText, 0f);
//        }

//        // DOTween 등장 애니메이션
//        Sequence seq = DOTween.Sequence();
//        for (int i = 0; i < 3; i++)
//        {
//            Transform iconT = itemSlots[i].transform.Find("ItemIcon");
//            TMP_Text nameText = itemSlots[i].transform.Find("ItemName").GetComponent<TMP_Text>();
//            TMP_Text priceText = itemSlots[i].transform.Find("ItemPrice").GetComponent<TMP_Text>();
//            TMP_Text descText = itemSlots[i].transform.Find("ItemDescription").GetComponent<TMP_Text>();
//            Button buyBtn = itemSlots[i].transform.Find("BuyButton").GetComponent<Button>();

//            seq.Append(iconT.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
//            seq.Join(iconT.GetComponent<Image>().DOFade(1f, 0.3f));
//            seq.Join(nameText.DOFade(1f, 0.3f));
//            seq.Join(priceText.DOFade(1f, 0.3f));
//            seq.Join(descText.DOFade(1f, 0.3f));
//            seq.OnComplete(() => buyBtn.interactable = true);
//        }
//    }

//    private void OnSelectItem(int index)
//    {
//        ItemStats chosenItem = selectedItems[index];
//        Debug.Log("선택한 아이템: " + chosenItem.itemName);

//        Transform iconT = itemSlots[index].transform.Find("ItemIcon");
//        TMP_Text nameText = itemSlots[index].transform.Find("ItemName").GetComponent<TMP_Text>();
//        TMP_Text priceText = itemSlots[index].transform.Find("ItemPrice").GetComponent<TMP_Text>();
//        TMP_Text descText = itemSlots[index].transform.Find("ItemDescription").GetComponent<TMP_Text>();
//        Button buyBtn = itemSlots[index].transform.Find("BuyButton").GetComponent<Button>();

//        // 선택 슬롯 강조
//        Sequence seq = DOTween.Sequence();
//        seq.Append(iconT.DOScale(1.3f, 0.2f).SetEase(Ease.OutBounce));

//        // 나머지 슬롯 사라짐
//        for (int i = 0; i < 3; i++)
//        {
//            if (i == index) continue;

//            Transform otherIcon = itemSlots[i].transform.Find("ItemIcon");
//            TMP_Text otherName = itemSlots[i].transform.Find("ItemName").GetComponent<TMP_Text>();
//            TMP_Text otherPrice = itemSlots[i].transform.Find("ItemPrice").GetComponent<TMP_Text>();
//            TMP_Text otherDesc = itemSlots[i].transform.Find("ItemDescription").GetComponent<TMP_Text>();
//            Button otherBtn = itemSlots[i].transform.Find("BuyButton").GetComponent<Button>();

//            otherIcon.DOScale(0f, 0.2f);
//            otherIcon.GetComponent<Image>().DOFade(0f, 0.2f);
//            otherName.DOFade(0f, 0.2f);
//            otherPrice.DOFade(0f, 0.2f);
//            otherDesc.DOFade(0f, 0.2f);
//            otherBtn.interactable = false;
//        }

//        // 선택 후 UI 끄기 및 적용
//        seq.Append(iconT.DOScale(1f, 0.2f).SetDelay(0.3f));
//        seq.OnComplete(() =>
//        {
//            gameObject.SetActive(false);
//            GameManager.Instance.shopManager.ApplyItemEffect(chosenItem);
//        });
//    }

//    private void SetAlpha(Graphic g, float alpha)
//    {
//        Color c = g.color;
//        c.a = alpha;
//        g.color = c;
//    }
//}
