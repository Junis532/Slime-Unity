using DG.Tweening;
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

    [Header("리롤 가격")]
    public TextMeshProUGUI rerollPriceText;
    public int rerollPrice = 1;

    [Header("상점 패널")]
    public RectTransform shopPanel;

    [Header("상점 UI 오브젝트")]
    public GameObject shopUI;

    private void Start()
    {
        rerollPriceText.text = $"리롤 {rerollPrice}원";
        rerollButton.onClick.AddListener(RerollItems);
        exitButton.onClick.AddListener(OnButtonExitClick);
        RerollItems();
    }

    public void InitShopUI()
    {
        UpdateRerollButtonState();
        UpdateBuyButtonStates();
    }

    public void ResetRerollPrice()
    {
        rerollPrice = 1;
        rerollPriceText.text = $"리롤 {rerollPrice}원";
        UpdateRerollButtonState();
        UpdateBuyButtonStates();
    }

    public void RerollItems()
    {
        int coin = GameManager.Instance.playerStats.coin;

        if (coin < rerollPrice)
        {
            Debug.Log("코인이 부족하여 리롤할 수 없습니다!");
            return;
        }

        GameManager.Instance.playerStats.coin -= rerollPrice;
        rerollPrice *= 2;

        List<ItemStats> selectedItems = GetRandomItems(itemSlots.Count);

        for (int i = 0; i < itemSlots.Count; i++)
        {
            GameObject slot = itemSlots[i];
            ItemStats item = selectedItems[i];

            slot.transform.Find("ItemName").GetComponent<TextMeshProUGUI>().text = item.itemName;
            slot.transform.Find("ItemDescription").GetComponent<TextMeshProUGUI>().text = item.description;
            slot.transform.Find("ItemPrice").GetComponent<TextMeshProUGUI>().text = item.price.ToString();
            slot.transform.Find("ItemIcon").GetComponent<Image>().sprite = item.icon;

            Button buyBtn = slot.transform.Find("BuyButton").GetComponent<Button>();
            buyBtn.onClick.RemoveAllListeners();

            ItemStats capturedItem = item;
            buyBtn.onClick.AddListener(() => BuyItem(capturedItem, slot));

            // 🎯 리롤 시 모든 버튼 다시 활성화
            buyBtn.interactable = true;
        }

        rerollPriceText.text = $"리롤 {rerollPrice}원";
        UpdateRerollButtonState();
        UpdateBuyButtonStates();
    }

    public void FirstRerollItems()
    {
        List<ItemStats> selectedItems = GetRandomItems(itemSlots.Count);

        for (int i = 0; i < itemSlots.Count; i++)
        {
            GameObject slot = itemSlots[i];
            ItemStats item = selectedItems[i];

            slot.transform.Find("ItemName").GetComponent<TextMeshProUGUI>().text = item.itemName;
            slot.transform.Find("ItemPrice").GetComponent<TextMeshProUGUI>().text = item.price.ToString();
            slot.transform.Find("ItemDescription").GetComponent<TextMeshProUGUI>().text = item.description;
            slot.transform.Find("ItemIcon").GetComponent<Image>().sprite = item.icon;

            Button buyBtn = slot.transform.Find("BuyButton").GetComponent<Button>();
            buyBtn.onClick.RemoveAllListeners();

            ItemStats capturedItem = item;
            buyBtn.onClick.AddListener(() => BuyItem(capturedItem, slot));

            // 🎯 초기 아이템 설정 시 모든 버튼 활성화
            buyBtn.interactable = true;
        }

        rerollPriceText.text = $"리롤 {rerollPrice}원";
        UpdateRerollButtonState();
        UpdateBuyButtonStates();
    }

    void BuyItem(ItemStats item, GameObject slot)
    {
        int playerCoin = GameManager.Instance.playerStats.coin;

        if (playerCoin < item.price)
        {
            Debug.Log("코인이 부족하여 구매할 수 없습니다!");
            return;
        }

        GameManager.Instance.playerStats.coin -= item.price;

        Debug.Log($"[구매] {item.itemName} - 코인 {item.price} 차감 후 남은 코인: {GameManager.Instance.playerStats.coin}");

        // ====== 아이템 효과 적용 ======
        if (item == GameManager.Instance.itemStats1) // 최대체력 증가 + 회복
        {
            GameManager.Instance.playerStats.maxHP += 5;
            GameManager.Instance.playerStats.currentHP += 5;
        }
        else if (item == GameManager.Instance.itemStats3)
        {
            GameManager.Instance.playerStats.speed *= 1.05f;
            GameManager.Instance.playerStats.maxHP -= 5;
            if (GameManager.Instance.playerStats.currentHP > GameManager.Instance.playerStats.maxHP)
            {
                GameManager.Instance.playerStats.currentHP = GameManager.Instance.playerStats.maxHP;
            }
        }
        else if (item == GameManager.Instance.itemStats5)
        {
            GameManager.Instance.playerStats.attack *= 1.02f;
        }
        else if (item == GameManager.Instance.itemStats6)
        {
            BulletSpawner spawner = GameManager.Instance.gameObject.GetComponent<BulletSpawner>();
            if (spawner != null)
            {
                spawner.spawnInterval -= 0.1f;
            }
        }
        else if (item == GameManager.Instance.itemStats10)
        {
            GameObject gmObj = GameObject.Find("GameManager");
            if (gmObj != null)
            {
                var bulletSpawner = gmObj.GetComponent<BulletSpawner>();
                if (bulletSpawner != null)
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
        }

        // 🎯 클릭한 슬롯만 비활성화
        Button buyBtn = slot.transform.Find("BuyButton").GetComponent<Button>();
        buyBtn.interactable = false;

        UpdateRerollButtonState();
        UpdateBuyButtonStates();
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

    void UpdateRerollButtonState()
    {
        int coin = GameManager.Instance.playerStats.coin;
        rerollButton.interactable = coin >= rerollPrice;
    }

    void UpdateBuyButtonStates()
    {
        int coin = GameManager.Instance.playerStats.coin;

        foreach (GameObject slot in itemSlots)
        {
            Button buyBtn = slot.transform.Find("BuyButton").GetComponent<Button>();
            TextMeshProUGUI priceText = slot.transform.Find("ItemPrice").GetComponent<TextMeshProUGUI>();

            if (int.TryParse(priceText.text, out int price))
            {
                // 이미 비활성화된 버튼은 그대로 두기
                if (buyBtn.interactable)
                    buyBtn.interactable = coin >= price;
            }
        }
    }

    public void OnButtonExitClick()
    {
        Debug.Log("상점 나감");

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
