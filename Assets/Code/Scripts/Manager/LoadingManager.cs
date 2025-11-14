using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingManager : MonoBehaviour
{
    static string nextScene;

    [SerializeField]
    Image progressBar;

    public static void LoadScene(string sceneName)
    {
        Time.timeScale = 1f;
        nextScene = sceneName;
        SceneManager.LoadScene("Loading");
    }

    void Start()
    {
        StartCoroutine(LoadSceneProgress());
    }

    IEnumerator LoadSceneProgress()
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(nextScene);
        op.allowSceneActivation = false;

        float timer = 0f;

        while (!op.isDone)
        {
            yield return null;
            timer += Time.unscaledDeltaTime;

            if (op.progress < 0.9f)
            {
                progressBar.fillAmount = Mathf.Clamp01(timer / 2.5f); // 2.5초 동안 0.9까지 증가
            }
            else
            {
                progressBar.fillAmount = Mathf.Lerp(0.9f, 1f, (timer - 2.5f) / 1f); // 이후 1초 동안 1.0까지 보정

                if (progressBar.fillAmount >= 1f)
                {
                    op.allowSceneActivation = true;
                }
            }
        }
    }
}
