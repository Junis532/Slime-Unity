using DG.Tweening;
using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuffEvent : MonoBehaviour
{
    [Header("버프 데이터")]
    public List<ItemStats> allItems
    {
        get { return GameManager.Instance.buffs; }
    }

    [Header("UI 슬롯 (2개)")]
    public List<GameObject> itemSlots;

    [Header("버프 패널")]
    public RectTransform shopPanel;

    [Header("버프 UI 오브젝트")]
    public GameObject shopUI;

    [Header("다이어로그")]
    public Image dialogImage;
    public TMP_Text dialogText;
    public List<string> dialogList;

    [Header("크리티컬 버프 개수")]
    public int criticalBuffCount = 0;

    private ItemStats[] selectedItems = new ItemStats[2];

    // 다이어로그 관련
    private bool isDialogActive = false;

    private void Start()
    {
        // 처음에는 아이템 슬롯 숨기기
        foreach (GameObject slot in itemSlots)
        {
            slot.SetActive(false);
        }

        // 다이어로그 랜덤 선택
        string chosenDialog;
        if (dialogList != null && dialogList.Count > 0)
        {
            int randIndex = Random.Range(0, dialogList.Count);
            chosenDialog = dialogList[randIndex];
        }
        else
        {
            chosenDialog = "힘을 얻을 기회가 왔습니다! 원하는 버프를 선택하세요.";
        }

        // 다이어로그 실행
        StartCoroutine(ShowDialogCoroutine(chosenDialog));
    }

    private void Update()
    {
        // 다이어로그 중에 클릭하면 → 종료
        if (isDialogActive && Input.GetMouseButtonDown(0))
        {
            StartCoroutine(HideDialogAndShowItems());
        }
    }

    // 다이어로그 한 글자씩 출력
    private IEnumerator ShowDialogCoroutine(string text)
    {
        isDialogActive = true;
        dialogImage.gameObject.SetActive(true);
        dialogImage.color = new Color(1, 1, 1, 1);
        dialogText.text = "";
        dialogText.color = new Color(0, 0, 0, 1);

        foreach (char c in text)
        {
            dialogText.text += c;
            yield return new WaitForSeconds(0.05f);
        }
    }

    // 다이어로그 내려가며 사라진 후 → 아이템 슬롯 등장
    private IEnumerator HideDialogAndShowItems()
    {
        isDialogActive = false;

        dialogImage.rectTransform.DOAnchorPosY(-500f, 0.5f).SetEase(Ease.InBack);
        dialogImage.DOFade(0f, 0.5f);
        dialogText.DOFade(0f, 0.5f);

        yield return new WaitForSeconds(0.6f);

        dialogImage.gameObject.SetActive(false);
        dialogText.gameObject.SetActive(false);

        // 슬롯 활성화 후 아이템 등장
        foreach (GameObject slot in itemSlots)
        {
            slot.SetActive(true);
        }

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
            Image icon = slot.transform.Find("ItemIcon").GetComponent<Image>();
            Button buyBtn = slot.transform.Find("BuyButton").GetComponent<Button>();

            nameText.text = item.itemName;
            descText.text = item.description;
            icon.sprite = item.icon;

            // 처음엔 투명
            icon.transform.localScale = Vector3.zero;
            SetAlpha(icon, 0f);
            SetAlpha(nameText, 0f);
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
            TMP_Text descText = slot.transform.Find("ItemDescription").GetComponent<TMP_Text>();
            Button buyBtn = slot.transform.Find("BuyButton").GetComponent<Button>();

            icon.sprite = selectedItems[i].icon;
            icon.transform.localScale = Vector3.zero;
            SetAlpha(icon, 0f);

            nameText.text = selectedItems[i].itemName;
            descText.text = selectedItems[i].description;

            // 처음엔 투명
            SetAlpha(nameText, 0f);
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
            TMP_Text descText = itemSlots[i].transform.Find("ItemDescription").GetComponent<TMP_Text>();

            seq.Append(iconT.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
            seq.Join(iconT.GetComponent<Image>().DOFade(1f, 0.3f));
            seq.Join(nameText.DOFade(1f, 0.3f));
            seq.Join(descText.DOFade(1f, 0.3f));
        }

        // 버튼 상태 업데이트
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
        TMP_Text descText = slot.transform.Find("ItemDescription").GetComponent<TMP_Text>();
        Button buyBtn = slot.transform.Find("BuyButton").GetComponent<Button>();

        Sequence seq = DOTween.Sequence();
        seq.Append(iconT.DOScale(1.3f, 0.2f).SetEase(Ease.OutBounce));

        for (int i = 0; i < itemSlots.Count; i++)
        {
            if (i == index) continue;

            Transform otherIcon = itemSlots[i].transform.Find("ItemIcon");
            TMP_Text otherName = itemSlots[i].transform.Find("ItemName").GetComponent<TMP_Text>();
            TMP_Text otherDesc = itemSlots[i].transform.Find("ItemDescription").GetComponent<TMP_Text>();

            otherIcon.DOScale(0f, 0.2f);
            otherIcon.GetComponent<Image>().DOFade(0f, 0.2f);
            otherName.DOFade(0f, 0.2f);
            otherDesc.DOFade(0f, 0.2f);
        }

        seq.Append(iconT.DOScale(1f, 0.2f).SetDelay(0.3f));
        seq.OnComplete(() =>
        {
            BuyItem(chosenItem, slot);
        });
    }

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
        Debug.Log($"[선택] {item.itemName}");

        ApplyItemEffect(item);
        UpdateBuyButtonStates();
        OnButtonExitClick();
    }

    void ApplyItemEffect(ItemStats item)
    {
        int index = GameManager.Instance.buffs.IndexOf(item);
        switch (index)
        {
            case 0:
                GameManager.Instance.playerStats.criticalChance += 5;
                criticalBuffCount++;
                break;

            case 1:
                float maxHPGet = GameManager.Instance.playerStats.maxHP * 0.2f;
                GameManager.Instance.playerStats.maxHP += maxHPGet;
                GameManager.Instance.playerStats.currentHP += maxHPGet;
                break;

            case 2:
                GameManager.Instance.playerStats.attack += GameManager.Instance.playerStats.attack * 0.25f;
                break;

            case 3:
                var gmObj = GameObject.Find("GameManager");
                if (gmObj != null)
                {
                    var bulletSpawner = gmObj.GetComponent<BulletSpawner>();
                    if (bulletSpawner != null)
                    {
                        bulletSpawner.attackSpeedMultiplier += 0.1f;
                    }
                }
                break;

            case 4:
                var plObj = GameObject.Find("Player");
                if (plObj != null)
                {
                    var jumpPower = plObj.GetComponent<JoystickDirectionIndicator>();
                    if (jumpPower != null)
                    {
                        jumpPower.slimeJumpDamage += jumpPower.slimeJumpDamage * 0.1f;
                    }
                }
                break;

            default:
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
        foreach (GameObject slot in itemSlots)
        {
            Button buyBtn = slot.transform.Find("BuyButton").GetComponent<Button>();
            buyBtn.interactable = true;

            TMP_Text priceText = slot.transform.Find("ItemPrice")?.GetComponent<TMP_Text>();
            if (priceText != null)
                priceText.gameObject.SetActive(false);

            TMP_Text nameText = slot.transform.Find("ItemName").GetComponent<TMP_Text>();
            TMP_Text descText = slot.transform.Find("ItemDescription").GetComponent<TMP_Text>();
            Color normalColor = Color.black;
            nameText.color = normalColor;
            descText.color = normalColor;
        }
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
        }

        // 🔥 버프 선택 창 닫으면 WaveManager에서 몹 스폰 시작
        WaveManager wm = FindFirstObjectByType<WaveManager>();
        if (wm != null)
        {
            GameManager.Instance.ChangeStateToGame();
            wm.StartSpawnLoop();
        }
    }

    public void OpenPanel()
    {
        // 1. 내부 상태 초기화
        isDialogActive = false;
        for (int i = 0; i < selectedItems.Length; i++) selectedItems[i] = null;

        // 2. UI 초기화
        dialogImage.gameObject.SetActive(true);
        dialogImage.rectTransform.anchoredPosition = new Vector2(0f, -275f);
        dialogImage.color = new Color(1, 1, 1, 1);

        dialogText.gameObject.SetActive(true);
        dialogText.text = "";
        dialogText.color = new Color(0, 0, 0, 1);

        foreach (var slot in itemSlots) slot.SetActive(false);

        if (shopPanel != null)
        {
            shopPanel.DOKill();
            shopPanel.anchoredPosition = new Vector2(0f, 0f);
            CanvasGroup cg = shopPanel.GetComponent<CanvasGroup>();
            if (cg != null)
                cg.alpha = 1f;
        }
        if (shopUI != null)
        {
            Canvas canvas = shopUI.GetComponent<Canvas>();
            if (canvas != null)
                canvas.sortingOrder = 100; // 필요시 UI가 뒤에 가려지지 않도록
        }

        // 3. 다이어로그 랜덤 선택
        string chosenDialog = (dialogList != null && dialogList.Count > 0)
                                ? dialogList[Random.Range(0, dialogList.Count)]
                                : "힘을 얻을 기회가 왔습니다! 원하는 버프를 선택하세요.";

        // 4. 코루틴 재시작
        StopAllCoroutines();
        StartCoroutine(ShowDialogCoroutine(chosenDialog));
    }


}
