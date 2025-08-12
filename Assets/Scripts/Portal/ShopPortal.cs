using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

public class ShopPortal : MonoBehaviour
{
    private bool shopWarp = false;
    private bool playerInside = false;
    private float stayTimer = 0f;
    public float requiredStayTime = 3f;

    public Image loadingImage; // 씬에 존재하는 이미지, 프리팹 내부가 아님
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

            // 처음엔 sortingOrder 0으로 세팅
            if (loadingCanvas != null)
                loadingCanvas.sortingOrder = -1;
        }
    }

    private void Update()
    {
        if (shopWarp || !playerInside) return;

        stayTimer += Time.deltaTime;

        if (stayTimer >= requiredStayTime)
        {
            shopWarp = true;
            StartCoroutine(WarpWithFade());
        }
    }

    private IEnumerator WarpWithFade()
    {
        if (loadingCanvas != null)
            loadingCanvas.sortingOrder = 10;  // 페이드인 시 상위 레이어로

        yield return loadingImage.DOFade(1f, fadeDuration).WaitForCompletion();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.position = new Vector3(-51.5f, 0f, 0);
            GameManager.Instance.ChangeStateToShop();
            Debug.Log("플레이어가 3초간 포탈 안에 머물러 상점 지역으로 이동!");
        }

        yield return new WaitForSeconds(0.7f);

        yield return loadingImage.DOFade(0f, fadeDuration).WaitForCompletion();

        if (loadingCanvas != null)
            loadingCanvas.sortingOrder = -1;  // 페이드아웃 후 원래대로

        Destroy(gameObject);
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
