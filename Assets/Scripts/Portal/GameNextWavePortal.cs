using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

public class GameNextWavePortal : MonoBehaviour
{
    public Image loadingImage;
    public float fadeDuration = 0.1f;

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

    private IEnumerator StartNextWaveWithFade()
    {
        if (loadingCanvas != null)
            loadingCanvas.sortingOrder = 10;  // 페이드인 시 최상위 레이어

        GameManager.Instance.playerController.canMove = false;

        yield return loadingImage.DOFade(1f, fadeDuration).WaitForCompletion();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.position = new Vector3(-9, 0, 0);
        }

        yield return new WaitForSeconds(0.5f);

        //GameManager.Instance.waveManager.StartNextWave();

        yield return loadingImage.DOFade(0f, fadeDuration);

        if (loadingCanvas != null)
            loadingCanvas.sortingOrder = -1;  // 페이드아웃 후 원래값으로
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            StartCoroutine(StartNextWaveWithFade());
        }
    }
}
