using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class deBuffEvent : MonoBehaviour
{
    [Header("디버프 데이터")]
    public List<ItemStats> allItems
    {
        get { return GameManager.Instance.debuffs; }
    }

    [Header("UI 슬롯 (1개)")]
    public GameObject itemSlot;

    [Header("버프 패널")]
    public RectTransform shopPanel;

    [Header("버프 UI 오브젝트")]
    public GameObject shopUI;

    [Header("거절 버튼")]
    public Button declineButton;

    [Header("다이어로그")]
    public Image dialogImage;
    public Image shopNPC;
    public TMP_Text dialogText;
    public string currentDialog = "대가를 치러야 합니다... 디버프를 선택하거나 거절하세요.";

    private ItemStats selectedItem;
    private bool isDialogActive = false;

    private void Start()
    {
        // 슬롯 먼저 숨기기
        itemSlot.SetActive(false);

        // 다이어로그 실행
        StartCoroutine(ShowDialogCoroutine(currentDialog));
    }

    private void Update()
    {
        // 다이어로그 중에 클릭하면 → 종료 후 슬롯 등장
        if (isDialogActive && Input.GetMouseButtonDown(0))
        {
            StartCoroutine(HideDialogAndShowItem());
        }
    }

    private IEnumerator ShowDialogCoroutine(string text)
    {
        isDialogActive = true;
        dialogImage.gameObject.SetActive(true);
        dialogImage.color = new Color(1, 1, 1, 1);
        dialogText.text = "";
        dialogText.color = new Color(0, 0, 0, 1);

        dialogText.isRightToLeftText = false;

        var sb = new System.Text.StringBuilder();

        foreach (char c in text)
        {
            sb.Append(c);
            dialogText.text = sb.ToString();
            yield return new WaitForSeconds(0.05f);
        }
    }

    // 다이어로그 내려가며 사라진 후 → 아이템 슬롯 등장
    private IEnumerator HideDialogAndShowItem()
    {
        isDialogActive = false;

        dialogImage.rectTransform.DOAnchorPosY(-500f, 0.5f).SetEase(Ease.InBack);
        dialogImage.DOFade(0f, 0.5f);
        shopNPC.rectTransform.DOAnchorPosY(-500f, 0.5f).SetEase(Ease.InBack);
        shopNPC.DOFade(0f, 0.5f);
        dialogText.DOFade(0f, 0.5f);

        yield return new WaitForSeconds(0.6f);

        dialogImage.gameObject.SetActive(false);
        shopNPC.gameObject.SetActive(false);
        dialogText.gameObject.SetActive(false);

        // 슬롯 활성화 후 아이템 등장
        itemSlot.SetActive(true);
        FirstRerollItem();
        StartCoroutine(RollItemCoroutine());
    }

    public void FirstRerollItem()
    {
        // 슬롯 UI 초기화
        TMP_Text nameText = itemSlot.transform.Find("ItemName").GetComponent<TMP_Text>();
        TMP_Text descText = itemSlot.transform.Find("ItemDescription").GetComponent<TMP_Text>();
        Image icon = itemSlot.transform.Find("ItemIcon").GetComponent<Image>();
        Button buyBtn = itemSlot.transform.Find("BuyButton").GetComponent<Button>();

        // 처음에는 투명하게 만듦
        SetAlpha(icon, 0f);
        SetAlpha(nameText, 0f);
        SetAlpha(descText, 0f);

        // 버튼 비활성화 (애니메이션 중 클릭 방지)
        buyBtn.interactable = false;
        declineButton.interactable = false;
        buyBtn.onClick.RemoveAllListeners();
        declineButton.onClick.RemoveAllListeners();

        // 패널 활성화
        gameObject.SetActive(true);
    }

    private IEnumerator RollItemCoroutine()
    {
        float duration = 1f;
        float timer = 0f;
        Image icon = itemSlot.transform.Find("ItemIcon").GetComponent<Image>();

        while (timer < duration)
        {
            int rand = Random.Range(0, allItems.Count);
            icon.sprite = allItems[rand].icon;
            icon.transform.localScale = Vector3.one;
            SetAlpha(icon, 1f);

            timer += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        // 최종 아이템 선택
        int finalRand = Random.Range(0, allItems.Count);
        selectedItem = allItems[finalRand];

        TMP_Text nameText = itemSlot.transform.Find("ItemName").GetComponent<TMP_Text>();
        TMP_Text descText = itemSlot.transform.Find("ItemDescription").GetComponent<TMP_Text>();
        Button buyBtn = itemSlot.transform.Find("BuyButton").GetComponent<Button>();

        // 최종 아이템 정보로 업데이트
        icon.sprite = selectedItem.icon;
        icon.transform.localScale = Vector3.zero;
        SetAlpha(icon, 0f);
        nameText.text = selectedItem.itemName;
        descText.text = selectedItem.description;
        SetAlpha(nameText, 0f);
        SetAlpha(descText, 0f);

        // 최종 애니메이션 시작
        Sequence seq = DOTween.Sequence();
        seq.Append(icon.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
        seq.Join(icon.DOFade(1f, 0.3f));
        seq.Join(nameText.DOFade(1f, 0.3f));
        seq.Join(descText.DOFade(1f, 0.3f));

        // 애니메이션 완료 후 버튼 활성화 및 리스너 연결
        seq.OnComplete(() =>
        {
            buyBtn.interactable = true;
            declineButton.interactable = true;
            buyBtn.onClick.AddListener(() => OnAccept());
            declineButton.onClick.AddListener(OnDecline);
        });
    }

    private void OnAccept()
    {
        Debug.Log($"[수락] {selectedItem.itemName}");

        // 🔥 아이콘 커졌다 돌아오는 애니메이션 추가
        Transform iconT = itemSlot.transform.Find("ItemIcon");
        Sequence seq = DOTween.Sequence();
        seq.Append(iconT.DOScale(1.3f, 0.2f).SetEase(Ease.OutBounce));
        seq.Append(iconT.DOScale(1f, 0.2f).SetDelay(0.3f));

        seq.OnComplete(() =>
        {
            ApplyItemEffect(selectedItem);
            OnButtonExitClick();
        });
    }

    private void OnDecline()
    {
        Debug.Log("[거절] 아이템 선택 없이 종료");
        OnButtonExitClick();
    }

    private void SetAlpha(Graphic g, float alpha)
    {
        Color c = g.color;
        c.a = alpha;
        g.color = c;
    }

    void ApplyItemEffect(ItemStats item)
    {
        int index = GameManager.Instance.debuffs.IndexOf(item);
        GameObject gmObj = GameObject.Find("GameManager");
        BulletSpawner bulletSpawner = gmObj?.GetComponent<BulletSpawner>();
        GameObject playerObj = GameObject.Find("Player");
        PlayerHeal playerHeal = playerObj?.GetComponent<PlayerHeal>();
        JoystickDirectionIndicator jumpPower = playerObj?.GetComponent<JoystickDirectionIndicator>();
        SlowSkill slowSkill = gmObj?.GetComponent<SlowSkill>();

        switch (index)
        {
            case 0:
                if (bulletSpawner != null)
                    bulletSpawner.bulletsPerShot += 2;
                break;
            case 1:
                if (bulletSpawner != null)
                {
                    if (!bulletSpawner.useFireball)
                        bulletSpawner.useFireball = true;
                    else
                        bulletSpawner.fireballDotMultiplier += 0.1f;
                }
                break;
            case 2:
                if (playerHeal != null)
                {
                    if (!playerHeal.hpHeal)
                        playerHeal.hpHeal = true;
                    else
                        playerHeal.hpHealAmount += 12;
                }
                break;
            case 3:
                if (bulletSpawner != null)
                {
                    if (!bulletSpawner.slowSkillActive)
                        bulletSpawner.slowSkillActive = true;
                    else if (slowSkill != null)
                        slowSkill.slowDuration += 0.5f;
                }
                break;
            default:
                Debug.LogWarning("알 수 없는 디버프 인덱스");
                break;
        }

        GameManager.Instance.playerStats.maxHP -= GameManager.Instance.playerStats.maxHP * 0.1f;
        if (GameManager.Instance.playerStats.currentHP > GameManager.Instance.playerStats.maxHP)
            GameManager.Instance.playerStats.currentHP = GameManager.Instance.playerStats.maxHP;
    }

    public void OnButtonExitClick()
    {
        if (shopPanel != null)
        {
            shopPanel.DOKill();
            CanvasGroup canvasGroup = shopPanel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
                canvasGroup.DOFade(0f, 0.7f);
            shopPanel.DOAnchorPosY(1500f, 0.7f);
            if (shopUI != null)
            {
                Canvas canvas = shopUI.GetComponent<Canvas>();
                if (canvas != null)
                    canvas.sortingOrder = -1;
            }
        }

        //WaveManager wm = FindFirstObjectByType<WaveManager>();
        //if (wm != null)
        //{
        //    GameManager.Instance.ChangeStateToGame();
        //    wm.StartSpawnLoop();
        //}
    }

    public void OpenPanel()
    {
        isDialogActive = false;
        selectedItem = null;

        dialogImage.gameObject.SetActive(true);
        dialogImage.rectTransform.anchoredPosition = new Vector2(0f, -223f);
        dialogImage.color = new Color(1, 1, 1, 1);

        shopNPC.gameObject.SetActive(true);
        shopNPC.rectTransform.anchoredPosition = new Vector2(0f, 0f);
        shopNPC.color = new Color(1, 1, 1, 1);

        dialogText.gameObject.SetActive(true);
        dialogText.text = "";
        dialogText.color = new Color(0, 0, 0, 1);

        itemSlot.SetActive(false);

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
                canvas.sortingOrder = 100;
        }

        StopAllCoroutines();
        StartCoroutine(ShowDialogCoroutine(currentDialog));
    }
}