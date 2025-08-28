using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class deBuffEvent : MonoBehaviour
{
    [Header("디버프 데이터")]
    public List<ItemStats> allItems;

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

    // 다이어로그 한 글자씩 출력
    private IEnumerator ShowDialogCoroutine(string text)
    {
        isDialogActive = true;
        dialogImage.gameObject.SetActive(true);
        dialogImage.color = new Color(1, 1, 1, 1);
        shopNPC.gameObject.SetActive(true);
        shopNPC.color = new Color(1, 1, 1, 1);
        dialogText.text = "";
        dialogText.color = new Color(0, 0, 0, 1);

        foreach (char c in text)
        {
            dialogText.text += c;
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
    }

    public void FirstRerollItem()
    {
        // 랜덤 아이템 1개 선택
        int rand = Random.Range(0, allItems.Count);
        selectedItem = allItems[rand];

        // 슬롯 UI 업데이트
        TMP_Text nameText = itemSlot.transform.Find("ItemName").GetComponent<TMP_Text>();
        TMP_Text descText = itemSlot.transform.Find("ItemDescription").GetComponent<TMP_Text>();
        Image icon = itemSlot.transform.Find("ItemIcon").GetComponent<Image>();
        Button buyBtn = itemSlot.transform.Find("BuyButton").GetComponent<Button>();

        nameText.text = selectedItem.itemName;
        descText.text = selectedItem.description;
        icon.sprite = selectedItem.icon;

        // BuyButton 클릭 시 아이템 구매 처리
        buyBtn.onClick.RemoveAllListeners();
        buyBtn.onClick.AddListener(() => OnAccept());

        // 처음엔 투명하게 만들고 애니메이션 등장
        icon.transform.localScale = Vector3.zero;
        SetAlpha(icon, 0f);
        SetAlpha(nameText, 0f);
        SetAlpha(descText, 0f);

        Sequence seq = DOTween.Sequence();
        seq.Append(icon.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
        seq.Join(icon.DOFade(1f, 0.3f));
        seq.Join(nameText.DOFade(1f, 0.3f));
        seq.Join(descText.DOFade(1f, 0.3f));

        // 거절 버튼 연결
        declineButton.onClick.RemoveAllListeners();
        declineButton.onClick.AddListener(OnDecline);

        // 패널 활성화
        gameObject.SetActive(true);
    }

    private void OnAccept()
    {
        Debug.Log($"[수락] {selectedItem.itemName}");
        ApplyItemEffect(selectedItem);
        OnButtonExitClick();
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
        if (item == GameManager.Instance.debuff1)
        {
            var bulletSpawner = GameObject.Find("GameManager")?.GetComponent<BulletSpawner>();
            if (bulletSpawner != null)
                bulletSpawner.bulletsPerShot += 2;

        }
        else if (item == GameManager.Instance.debuff2)
        {
            var bulletSpawner = GameObject.Find("GameManager")?.GetComponent<BulletSpawner>();
            if (bulletSpawner != null)
            {
                if (!bulletSpawner.useFireball)
                    bulletSpawner.useFireball = true;
                else
                    bulletSpawner.fireballDotMultiplier += 0.1f;
            }

        }
        else if (item == GameManager.Instance.debuff3)
        {
            var playerHeal = GameObject.Find("Player")?.GetComponent<PlayerHeal>();
            if (playerHeal != null)
            {
                if (!playerHeal.hpHeal)
                    playerHeal.hpHeal = true;
                else
                    playerHeal.hpHealAmount += 12;
            }

        }
        else if (item == GameManager.Instance.debuff4)
        {
            var bulletSpawner = GameObject.Find("GameManager")?.GetComponent<BulletSpawner>();
            if (bulletSpawner != null)
            {
                if (!bulletSpawner.slowSkillActive)
                    bulletSpawner.slowSkillActive = true;
                else
                {
                    var slowSkill = GameObject.Find("GameManager")?.GetComponent<SlowSkill>();
                    if (slowSkill != null)
                        slowSkill.slowDuration += 0.5f;
                }
            }
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

            GameManager.Instance.playerController.canMove = true;
        }
    }
}
