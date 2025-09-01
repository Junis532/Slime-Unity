using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    [Header("아이템 데이터")]
    public List<ItemStats> allItems;

    [Header("UI 슬롯 (3개)")]
    public List<GameObject> itemSlots;

    [Header("버튼")]
    public Button rerollButton;
    public Button exitButton;

    [Header("상점 패널")]
    public RectTransform shopPanel;

    [Header("상점 UI 오브젝트")]
    public GameObject shopUI;

    private ItemStats[] selectedItems = new ItemStats[3];

    private void Start()
    {

        FirstRerollItems();
    }

    public void FirstRerollItems()
    {
        List<ItemStats> selected = GetRandomItems(itemSlots.Count);

        for (int i = 0; i < itemSlots.Count; i++)
        {
            GameObject slot = itemSlots[i];
            ItemStats item = selected[i];

            TMP_Text nameText = slot.transform.Find("ItemName").GetComponent<TMP_Text>();
            TMP_Text descText = slot.transform.Find("ItemDescription").GetComponent<TMP_Text>();
            TMP_Text priceText = slot.transform.Find("ItemPrice").GetComponent<TMP_Text>();
            Image icon = slot.transform.Find("ItemIcon").GetComponent<Image>();
            Button buyBtn = slot.transform.Find("BuyButton").GetComponent<Button>();

            nameText.text = item.itemName;
            descText.text = item.description;
            priceText.text = item.price.ToString();
            icon.sprite = item.icon;

            // 처음엔 투명
            icon.transform.localScale = Vector3.zero;
            SetAlpha(icon, 0f);
            SetAlpha(nameText, 0f);
            SetAlpha(priceText, 0f);
            SetAlpha(descText, 0f);

            buyBtn.onClick.RemoveAllListeners();
            int idx = i;
            buyBtn.onClick.AddListener(() => OnSelectItem(idx));
        }

        ShowItemChoices();
    }

    public void ShowItemChoices()
    {
        if (allItems.Count < itemSlots.Count)
        {
            Debug.LogWarning("allItems에 충분한 아이템이 없습니다!");
            return;
        }

        gameObject.SetActive(true);
        StartCoroutine(RollItemsCoroutine());
    }

    private IEnumerator RollItemsCoroutine()
    {
        float duration = 1f;
        float timer = 0f;

        // 슬롯 아이콘 랜덤 회전 효과
        while (timer < duration)
        {
            for (int i = 0; i < itemSlots.Count; i++)
            {
                int rand = Random.Range(0, allItems.Count);
                Image icon = itemSlots[i].transform.Find("ItemIcon").GetComponent<Image>();
                icon.sprite = allItems[rand].icon;
                icon.transform.localScale = Vector3.one;
                SetAlpha(icon, 1f);
            }

            timer += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        // 최종 아이템 선택
        List<ItemStats> tempList = new List<ItemStats>(allItems);
        for (int i = 0; i < itemSlots.Count; i++)
        {
            int rand = Random.Range(0, tempList.Count);
            selectedItems[i] = tempList[rand];
            tempList.RemoveAt(rand);

            GameObject slot = itemSlots[i];
            Image icon = slot.transform.Find("ItemIcon").GetComponent<Image>();
            TMP_Text nameText = slot.transform.Find("ItemName").GetComponent<TMP_Text>();
            TMP_Text priceText = slot.transform.Find("ItemPrice").GetComponent<TMP_Text>();
            TMP_Text descText = slot.transform.Find("ItemDescription").GetComponent<TMP_Text>();
            Button buyBtn = slot.transform.Find("BuyButton").GetComponent<Button>();

            icon.sprite = selectedItems[i].icon;
            icon.transform.localScale = Vector3.zero;
            SetAlpha(icon, 0f);

            nameText.text = selectedItems[i].itemName;
            priceText.text = selectedItems[i].price.ToString();
            descText.text = selectedItems[i].description;

            // 처음엔 투명
            SetAlpha(nameText, 0f);
            SetAlpha(priceText, 0f);
            SetAlpha(descText, 0f);

            buyBtn.onClick.RemoveAllListeners();
            int idx = i;
            buyBtn.onClick.AddListener(() => OnSelectItem(idx));
        }

        // DOTween Sequence
        Sequence seq = DOTween.Sequence();
        for (int i = 0; i < itemSlots.Count; i++)
        {
            Transform iconT = itemSlots[i].transform.Find("ItemIcon");
            TMP_Text nameText = itemSlots[i].transform.Find("ItemName").GetComponent<TMP_Text>();
            TMP_Text priceText = itemSlots[i].transform.Find("ItemPrice").GetComponent<TMP_Text>();
            TMP_Text descText = itemSlots[i].transform.Find("ItemDescription").GetComponent<TMP_Text>();

            seq.Append(iconT.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
            seq.Join(iconT.GetComponent<Image>().DOFade(1f, 0.3f));
            seq.Join(nameText.DOFade(1f, 0.3f));
            seq.Join(priceText.DOFade(1f, 0.3f));
            seq.Join(descText.DOFade(1f, 0.3f));
        }

        // ★ 여기서 버튼 상태 업데이트
        UpdateBuyButtonStates();
    }


    private void OnSelectItem(int index)
    {
        GameObject slot = itemSlots[index];
        ItemStats chosenItem = selectedItems[index];

        // 선택 시 나머지 버튼 모두 비활성화
        DisableAllBuyButtons();

        Transform iconT = slot.transform.Find("ItemIcon");
        TMP_Text nameText = slot.transform.Find("ItemName").GetComponent<TMP_Text>();
        TMP_Text priceText = slot.transform.Find("ItemPrice").GetComponent<TMP_Text>();
        TMP_Text descText = slot.transform.Find("ItemDescription").GetComponent<TMP_Text>();
        Button buyBtn = slot.transform.Find("BuyButton").GetComponent<Button>();

        Sequence seq = DOTween.Sequence();
        seq.Append(iconT.DOScale(1.3f, 0.2f).SetEase(Ease.OutBounce));

        for (int i = 0; i < itemSlots.Count; i++)
        {
            if (i == index) continue;

            Transform otherIcon = itemSlots[i].transform.Find("ItemIcon");
            TMP_Text otherName = itemSlots[i].transform.Find("ItemName").GetComponent<TMP_Text>();
            TMP_Text otherPrice = itemSlots[i].transform.Find("ItemPrice").GetComponent<TMP_Text>();
            TMP_Text otherDesc = itemSlots[i].transform.Find("ItemDescription").GetComponent<TMP_Text>();

            otherIcon.DOScale(0f, 0.2f);
            otherIcon.GetComponent<Image>().DOFade(0f, 0.2f);
            otherName.DOFade(0f, 0.2f);
            otherPrice.DOFade(0f, 0.2f);
            otherDesc.DOFade(0f, 0.2f);
        }

        seq.Append(iconT.DOScale(1f, 0.2f).SetDelay(0.3f));
        seq.OnComplete(() =>
        {
            BuyItem(chosenItem, slot);
        });
    }

    // 나머지 버튼 모두 비활성화
    private void DisableAllBuyButtons()
    {
        foreach (GameObject slot in itemSlots)
        {
            Button buyBtn = slot.transform.Find("BuyButton").GetComponent<Button>();
            buyBtn.interactable = false;
            buyBtn.onClick.RemoveAllListeners();
        }
    }

    private void SetAlpha(Graphic g, float alpha)
    {
        Color c = g.color;
        c.a = alpha;
        g.color = c;
    }

    void BuyItem(ItemStats item, GameObject slot)
    {
        if (GameManager.Instance.playerStats.coin < item.price)
        {
            TMP_Text priceText = slot.transform.Find("ItemPrice").GetComponent<TMP_Text>();
            priceText.color = Color.red;
            Debug.Log("코인이 부족하여 구매할 수 없습니다!");
            return;
        }

        GameManager.Instance.playerStats.coin -= item.price;
        Debug.Log($"[구매] {item.itemName} - 남은 코인: {GameManager.Instance.playerStats.coin}");

        ApplyItemEffect(item);
        UpdateBuyButtonStates();
        OnButtonExitClick();
    }

    void ApplyItemEffect(ItemStats item)
    {
        int index = GameManager.Instance.allItemStats.IndexOf(item);
        switch (index)
        {
            case 0: // itemStats1 효과
                GameManager.Instance.playerStats.maxHP += 160;
                GameManager.Instance.playerStats.currentHP += 160;
                break;

            case 1: // itemStats2 효과 - 총알 수 증가
            case 3: // itemStats4 효과 - 파이어볼 관련 (공통 bulletSpawner 사용)
            case 4: // itemStats5 효과 - 공격 속도 증가
            case 7: // itemStats8 효과 - 슬로우 스킬 관련
                {
                    GameObject gmObj = GameObject.Find("GameManager");
                    if (gmObj == null) break;
                    var bulletSpawner = gmObj.GetComponent<BulletSpawner>();
                    if (bulletSpawner == null) break;

                    if (index == 1)
                    {
                        bulletSpawner.bulletsPerShot += 2;
                    }
                    else if (index == 3)
                    {
                        if (!bulletSpawner.useFireball)
                        {
                            bulletSpawner.useFireball = true;
                            Debug.Log("파이어볼 활성화");
                        }
                        else
                        {
                            bulletSpawner.fireballDotMultiplier += 0.1f;
                            Debug.Log($"[Shop] 파이어볼 DOT 배율 증가: {bulletSpawner.fireballDotMultiplier}");
                        }
                    }
                    else if (index == 4)
                    {
                        bulletSpawner.attackSpeedMultiplier += 0.1f;
                    }
                    else if (index == 7)
                    {
                        if (!bulletSpawner.slowSkillActive)
                        {
                            bulletSpawner.slowSkillActive = true;
                            Debug.Log("[Shop] BulletSpawner의 slowSkillActive 활성화");
                        }
                        else
                        {
                            var slowSkill = gmObj.GetComponent<SlowSkill>();
                            if (slowSkill != null)
                            {
                                slowSkill.slowDuration += 0.5f;
                                Debug.Log("[Shop] 슬로우 지속 시간 증가");
                            }
                        }
                    }
                }
                break;

            case 2: // itemStats3 효과
                GameManager.Instance.playerStats.speed *= 1.05f;
                GameManager.Instance.playerStats.maxHP -= 5;
                if (GameManager.Instance.playerStats.currentHP > GameManager.Instance.playerStats.maxHP)
                    GameManager.Instance.playerStats.currentHP = GameManager.Instance.playerStats.maxHP;
                break;

            case 5: // itemStats6 효과 - 공격력 증가
                GameManager.Instance.playerStats.attack += 60;
                break;

            case 6: // itemStats7 효과 - 피 회복 활성화
                {
                    GameObject plObj = GameObject.Find("Player");
                    if (plObj == null) break;
                    var playerHeal = plObj.GetComponent<PlayerHeal>();
                    if (playerHeal == null) break;

                    if (!playerHeal.hpHeal)
                    {
                        playerHeal.hpHeal = true;
                        Debug.Log("피 회복 활성화");
                    }
                    else
                    {
                        playerHeal.hpHealAmount += 12;
                        Debug.Log("힐량 증가");
                    }
                }
                break;

            default:
                Debug.LogWarning("알 수 없는 아이템 효과 인덱스");
                break;
        }
    }

    List<ItemStats> GetRandomItems(int count)
    {
        List<ItemStats> copy = new List<ItemStats>(allItems);
        List<ItemStats> result = new List<ItemStats>();

        for (int i = 0; i < count && copy.Count > 0; i++)
        {
            int idx = Random.Range(0, copy.Count);
            result.Add(copy[idx]);
            copy.RemoveAt(idx);
        }
        return result;
    }

    void UpdateBuyButtonStates()
    {
        int coin = GameManager.Instance.playerStats.coin;
        bool allDisabled = true; // 모든 버튼이 비활성인지 체크

        foreach (GameObject slot in itemSlots)
        {
            TMP_Text priceText = slot.transform.Find("ItemPrice").GetComponent<TMP_Text>();
            Button buyBtn = slot.transform.Find("BuyButton").GetComponent<Button>();

            if (int.TryParse(priceText.text, out int price))
            {
                // 코인이 부족하면 버튼 비활성화, 충분하면 활성화
                bool canBuy = coin >= price;
                buyBtn.interactable = canBuy;

                TMP_Text nameText = slot.transform.Find("ItemName").GetComponent<TMP_Text>();
                TMP_Text descText = slot.transform.Find("ItemDescription").GetComponent<TMP_Text>();

                Color targetColor = canBuy ? Color.black : Color.red;
                nameText.color = targetColor;
                priceText.color = targetColor;
                descText.color = targetColor;

                if (canBuy) allDisabled = false; // 하나라도 살 수 있으면 false
            }
        }

        // 모든 아이템 구매 불가 시 1초 뒤 자동 종료
        if (allDisabled)
        {
            StartCoroutine(AutoExitCoroutine());
        }
    }

    private IEnumerator AutoExitCoroutine()
    {
        yield return new WaitForSeconds(1f);
        OnButtonExitClick();
    }



    public void OnButtonExitClick()
    {
        if (shopPanel != null)
        {
            shopPanel.DOKill();
            CanvasGroup canvasGroup = shopPanel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.DOFade(0f, 0.7f);
            }
            shopPanel.DOAnchorPosY(1500f, 0.7f);
            if (shopUI != null)
            {
                Canvas canvas = shopUI.GetComponent<Canvas>();
                if (canvas != null)
                {
                    canvas.sortingOrder = -1;
                }
            }
            GameManager.Instance.playerController.canMove = true;
        }
    }
}
