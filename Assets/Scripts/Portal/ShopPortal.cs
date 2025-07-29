using UnityEngine;

public class ShopPortal : MonoBehaviour
{
    private bool shopWarp = false;
    private bool playerInside = false;
    private float stayTimer = 0f;
    public float requiredStayTime = 3f;

    private void Update()
    {
        if (shopWarp || !playerInside) return;

        stayTimer += Time.deltaTime;

        if (stayTimer >= requiredStayTime)
        {
            shopWarp = true;
            WarpToShop();
        }
        
    }

    public void WarpToShop()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.Translate(-101.5f, 0, 0); // 상점 지역으로 이동  
            Debug.Log("플레이어가 3초간 포탈 안에 머물러 상점 지역으로 이동!");
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = true;
            stayTimer = 0f; // 시간 초기화
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
