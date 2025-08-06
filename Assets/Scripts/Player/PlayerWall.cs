using UnityEngine;

public class PlayerWall : MonoBehaviour
{
    private Collider2D playerCollider;
    private Collider2D wallCollider;

    void Start()
    {
        playerCollider = GetComponent<Collider2D>();
        if (playerCollider == null)
        {
            Debug.LogError("Player collider missing on player!");
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        // 벽 콜라이더인지 검사 (태그 또는 Layer로 구분하는 게 좋음)
        if (other.CompareTag("Wall"))
        {
            wallCollider = other;

            Bounds playerBounds = playerCollider.bounds;
            Bounds wallBounds = wallCollider.bounds;

            Vector3[] playerCorners = new Vector3[4]
            {
                new Vector3(playerBounds.min.x, playerBounds.min.y),
                new Vector3(playerBounds.min.x, playerBounds.max.y),
                new Vector3(playerBounds.max.x, playerBounds.min.y),
                new Vector3(playerBounds.max.x, playerBounds.max.y)
            };

            bool allInside = true;
            foreach (var corner in playerCorners)
            {
                if (!wallBounds.Contains(corner))
                {
                    allInside = false;
                    break;
                }
            }

            if (allInside)
            {

                if (GameManager.Instance.joystickDirectionIndicator.IsUsingSkill)
                {
                    Debug.Log("스킬 사용 중이라 낭떨어지 무시");
                    return;
                }
                    Debug.Log("Player is fully inside the wall!");

                PlayerController playerCtrl = GetComponent<PlayerController>();
                if (playerCtrl != null)
                {
                    playerCtrl.canMove = false;
                }

                if (GameManager.Instance != null && GameManager.Instance.playerStats != null)
                {
                    GameManager.Instance.playerStats.currentHP = 0;
                }
            }
        }
    }
}
