using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

public class NextWavePortal : MonoBehaviour
{
    private bool waveStarted = false;
    private bool playerInside = false;
    private float stayTimer = 0f;
    public float requiredStayTime = 3f;

    public Image loadingImage;  // 씬에 존재하는 로딩 이미지 (프리팹 내부 아님)
    public float fadeDuration = 0.5f;

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
                loadingCanvas.sortingOrder = -1;  // 기본값 낮게 세팅
        }
    }

    private void Update()
    {
        if (waveStarted || !playerInside) return;

        stayTimer += Time.deltaTime;

        if (stayTimer >= requiredStayTime)
        {
            waveStarted = true;
            StartCoroutine(StartNextWaveWithFade());
        }
    }

    private IEnumerator StartNextWaveWithFade()
    {
        if (loadingCanvas != null)
            loadingCanvas.sortingOrder = 10;  // 페이드인 시 최상위 레이어

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
            waveStarted = false;  // 다음 웨이브 시작 후 다시 false로 설정
        }

        yield return new WaitForSeconds(0.7f);

        GameManager.Instance.waveManager.StartNextWave();

        yield return loadingImage.DOFade(0f, fadeDuration);

        if (loadingCanvas != null)
            loadingCanvas.sortingOrder = -1;  // 페이드아웃 후 원래값으로


        Destroy(gameObject);  // 포탈 삭제
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = true;
            stayTimer = 0f; // 타이머 초기화
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
            stayTimer = 0f; // 나가면 타이머 리셋
        }
    }
}
