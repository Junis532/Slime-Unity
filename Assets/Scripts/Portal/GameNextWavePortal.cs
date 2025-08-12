using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

public class GameNextWavePortal : MonoBehaviour
{
    private bool waveStarted = false;
    private bool playerInside = false;
    private float stayTimer = 0f;
    public float requiredStayTime = 3f;

    public Image loadingImage;  // 씬에 존재하는 이미지 (프리팹 내부 아님)
    public float fadeDuration = 1f;

    private Canvas loadingCanvas;

    void Start()
    {
        if (loadingImage == null)
        {
            GameObject go = GameObject.FindWithTag("LoadingImage");
            if (go != null)
                loadingImage = go.GetComponent<Image>();
            else
                Debug.LogWarning("LoadingImage 태그의 이미지 오브젝트를 찾지 못했습니다!");
        }

        if (loadingImage != null)
        {
            loadingCanvas = loadingImage.GetComponentInParent<Canvas>();
            if (loadingCanvas == null)
                Debug.LogWarning("LoadingImage의 부모에 Canvas 컴포넌트가 없습니다!");

            Color c = loadingImage.color;
            c.a = 0f;
            loadingImage.color = c;

            if (loadingCanvas != null)
                loadingCanvas.sortingOrder = -1;
        }
    }

    private void Update()
    {
        if (waveStarted || !playerInside) return;

        stayTimer += Time.deltaTime;

        if (stayTimer >= requiredStayTime)
        {
            StartCoroutine(StartNextWaveWithFade());
        }
    }

    private IEnumerator StartNextWaveWithFade()
    {
        if (loadingCanvas != null)
            loadingCanvas.sortingOrder = 10;

        yield return loadingImage.DOFade(1f, fadeDuration).WaitForCompletion();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.position = new Vector3(-9, 0, 0);
        }

        waveStarted = true;


        // 게임 상태가 여전히 "Game"이면 waveStarted를 false로 되돌림 (기존 코드 유지)
        if (GameManager.Instance.CurrentState == "Game")
        {
            waveStarted = false;
        }

        Debug.Log("플레이어가 3초간 포탈 안에 머물러 다음 웨이브 시작!");

        yield return new WaitForSeconds(1f);

        GameManager.Instance.waveManager.StartNextWave();

        yield return loadingImage.DOFade(0f, fadeDuration);

        if (loadingCanvas != null)
            loadingCanvas.sortingOrder = -1;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = true;
            stayTimer = 0f;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
            stayTimer = 0f;
        }
    }
}
