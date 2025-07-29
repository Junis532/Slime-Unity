using UnityEngine;

public class ShopEnter : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("플레이어가 상점 영역에 진입함. 상점 상태로 변경합니다.");
            GameManager.Instance.ChangeStateToShop();
        }
    }
}
