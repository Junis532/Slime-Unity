using UnityEngine;

public class DoorController : MonoBehaviour
{
    private Collider2D col;
    //private Animator animator;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        //animator = GetComponent<Animator>();
    }

    // 문 닫힘: 플레이어 막기 → Trigger 끄기, Collider 활성화
    public void CloseDoor()
    {
        if (col != null)
        {
            //col.enabled = true;
            col.isTrigger = false;  // 충돌 처리
        }
        //if (animator != null) animator.SetTrigger("Close");
    }

    // 문 열림: 플레이어 통과 → Trigger 켜기, Collider 유지
    public void OpenDoor()
    {
        if (col != null)
        {
            //col.enabled = true;
            col.isTrigger = true;   // 통과 가능
        }
        //if (animator != null) animator.SetTrigger("Open");
    }
}
