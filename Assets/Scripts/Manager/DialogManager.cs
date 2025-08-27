//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;
//using System.Collections;
//using System.Collections.Generic;

//public class DialogManager : MonoBehaviour
//{
//    public static DialogManager Instance;

//    [System.Serializable]
//    public struct DialogData
//    {
//        [TextArea(3, 5)]
//        public string dialog;
//    }

//    [System.Serializable]
//    public class DialogPage
//    {
//        public List<DialogData> dialogs;
//    }

//    [Header("상점주인 대사 페이지들 (1페이지 = 여러 대사)")]
//    public List<DialogPage> shopDialogPages;

//    public Image imageDialog;
//    public TextMeshProUGUI textDialog;

//    private int currentPageIndex = 0;
//    private int currentLineIndex = 0;

//    private Coroutine typingCoroutine;
//    private bool isTyping = false;

//    private void Awake()
//    {
//        Instance = this;
//    }

//    private void Update()
//    {
//        if (GameManager.Instance.IsShop() && Input.GetMouseButtonDown(0))
//        {
//            NextLine();
//        }
//    }

//    public void StartShopDialog()
//    {
//        if (shopDialogPages.Count == 0)
//        {
//            Debug.LogWarning("상점 대사 페이지가 없습니다.");
//            return;
//        }

//        currentPageIndex = Random.Range(0, shopDialogPages.Count);
//        currentLineIndex = 0;

//        imageDialog.gameObject.SetActive(true);
//        textDialog.gameObject.SetActive(true);

//        ShowCurrentLine();
//    }

//    void ShowCurrentLine()
//    {
//        DialogPage page = shopDialogPages[currentPageIndex];

//        if (currentLineIndex >= page.dialogs.Count)
//        {
//            EndDialog();
//            return;
//        }

//        if (typingCoroutine != null)
//        {
//            StopCoroutine(typingCoroutine);
//        }

//        typingCoroutine = StartCoroutine(TypeLine(page.dialogs[currentLineIndex].dialog));
//    }

//    IEnumerator TypeLine(string line)
//    {
//        isTyping = true;
//        textDialog.text = "";

//        foreach (char c in line)
//        {
//            textDialog.text += c;
//            yield return new WaitForSeconds(0.07f);  // 글자당 딜레이
//        }

//        isTyping = false;
//    }

//    public void NextLine()
//    {
//        if (isTyping)
//        {
//            StopCoroutine(typingCoroutine);
//            DialogPage page = shopDialogPages[currentPageIndex];
//            textDialog.text = page.dialogs[currentLineIndex].dialog;
//            isTyping = false;
//        }
//        else
//        {
//            currentLineIndex++;
//            ShowCurrentLine();
//        }
//    }

//    void EndDialog()
//    {
//        imageDialog.gameObject.SetActive(false);
//        textDialog.gameObject.SetActive(false);
//        Debug.Log("[DialogManager] 대화 종료");
//    }
//}
