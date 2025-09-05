using UnityEngine;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenu;
    public Button pauseButton;
    public Button resumeButton;
    public Button leaveButton;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (pauseButton != null)
        {
            pauseButton.onClick.AddListener(ToggleUI);
        }
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(ToggleUI);
        }
        if (leaveButton != null)
        {
            leaveButton.onClick.AddListener(OnLeaveClicked);
            leaveButton.onClick.AddListener(ToggleUI);
        }
    }

    void ToggleUI()
    {
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(!pauseMenu.activeSelf);

        }
        if (pauseMenu.activeSelf)
        {
            Time.timeScale = 0f; // Pause the game
            GameManager.Instance.playerController.canMove = false;
        }
        else
        {
            Time.timeScale = 1f; // Resume the game
            GameManager.Instance.playerController.canMove = true;
        }
    }

    void OnLeaveClicked()
    {
        LoadingManager.LoadScene("MainMenu");
    }
}
