using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class BtnType : MonoBehaviour/*, IPointerEnterHandler, IPointerExitHandler*/
{
    public BTNType currentType;
    public Transform buttonScale;
    Vector3 defaultScale;

    public CanvasGroup mainGroup;
    public CanvasGroup optionGroup;

    bool isSound;

    private void Start()
    {
        defaultScale = buttonScale.localScale;
    }

    public void OnBtnClick()
    {
        switch (currentType)
        {
            case BTNType.Start:
                LoadingManager.LoadScene("StartCutSceneReal");
                break;

            case BTNType.Option:
                CanvasGroupOn(optionGroup);
                CanvasGroupOff(mainGroup);
                break;

            case BTNType.Sound:
                if (isSound)
                    Debug.Log("사운드 OFF");
                else
                    Debug.Log("사운드 ON");
                isSound = !isSound;
                break;

            case BTNType.Back:
                CanvasGroupOn(mainGroup);
                CanvasGroupOff(optionGroup);
                break;

            case BTNType.Quit:
                Application.Quit();
                Debug.Log("게임 종료");
                break;

            case BTNType.GameStart:
                SceneManager.LoadScene("StartCutSceneReal");
                break;

            case BTNType.GameLeave:
                SceneManager.LoadScene("MainMenu");
                break;
        }
    }

    public void CanvasGroupOn(CanvasGroup cg)
    {
        cg.alpha = 1;
        cg.interactable = true;
        cg.blocksRaycasts = true;
    }

    public void CanvasGroupOff(CanvasGroup cg)
    {
        cg.alpha = 0;
        cg.interactable = false;
        cg.blocksRaycasts = false;
    }

    //public void OnPointerEnter(PointerEventData eventData)
    //{
    //    buttonScale.localScale = defaultScale * 1.2f;
    //}

    //public void OnPointerExit(PointerEventData eventData)
    //{
    //    buttonScale.localScale = defaultScale;
    //}
}
