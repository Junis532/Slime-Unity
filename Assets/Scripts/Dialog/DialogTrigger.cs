using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogTrigger : MonoBehaviour
{
    [System.Serializable]
    public struct DialogData
    {
        [TextArea(3, 5)]
        public string dialog;
    }

    [Header("대사 리스트")]
    public List<DialogData> dialogs;

    [Header("UI 참조")]
    public Image dialogBackground;
    public TextMeshProUGUI dialogText;

    private int currentLineIndex = 0;
    private Coroutine typingCoroutine;

    [Header("자동 전환 딜레이 (초)")]
    public float autoNextDelay = 2f;

    private void Start()
    {
        StartDialog();
    }

    public void StartDialog()
    {
        if (dialogs == null || dialogs.Count == 0)
        {
            Debug.LogWarning("대사 내용이 없습니다.");
            return;
        }

        currentLineIndex = 0;

        if (dialogBackground != null) dialogBackground.gameObject.SetActive(true);
        if (dialogText != null) dialogText.gameObject.SetActive(true);

        ShowCurrentLine();
    }

    void ShowCurrentLine()
    {
        if (currentLineIndex >= dialogs.Count)
        {
            currentLineIndex = 0; // 다시 처음부터 반복
            // 또는 EndDialog(); 호출해서 종료 가능
        }

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeLine(dialogs[currentLineIndex].dialog));
    }

    IEnumerator TypeLine(string line)
    {
        dialogText.text = "";

        foreach (char c in line)
        {
            dialogText.text += c;
            yield return new WaitForSeconds(0.05f);
        }


        yield return new WaitForSeconds(autoNextDelay);

        currentLineIndex++;
        ShowCurrentLine();
    }

    
}
