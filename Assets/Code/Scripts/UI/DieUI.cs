using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class DieUI : MonoBehaviour
{
    public Button leaveButton;
    public Button retryButton;
    EventSystem usedEvent;

    void Start()
    {
        usedEvent = FindAnyObjectByType<EventSystem>();
        usedEvent.enabled = true;
        leaveButton.onClick.AddListener(OnLeaveClicked);
        retryButton.onClick.AddListener(OnRetryClicked);
    }

    void OnLeaveClicked()
    {
        LoadingManager.LoadScene("MainMenu");
    }

    void OnRetryClicked()
    {
        usedEvent.enabled = false;
        LoadingManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
