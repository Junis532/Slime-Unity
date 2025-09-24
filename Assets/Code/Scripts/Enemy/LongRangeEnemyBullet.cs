using UnityEngine;

public class LongRangeEnemyBullet : MonoBehaviour
{
    public bool destroyOnObstacle = false;
    public bool ignorePlayerWhenUsingSkill = true;

    private Collider2D myCollider;
    private Collider2D playerCollider;

    void Awake()
    {
        myCollider = GetComponent<Collider2D>();

        // 🔹 Player 태그로 찾아서 Collider 가져오기
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerCollider = playerObj.GetComponent<Collider2D>();
        }
    }

    void Update()
    {
        if (ignorePlayerWhenUsingSkill && GameManager.Instance.joystickDirectionIndicator != null && playerCollider != null)
        {
            if (GameManager.Instance.joystickDirectionIndicator.IsUsingSkill)
            {
                Physics2D.IgnoreCollision(myCollider, playerCollider, true);
            }
            else
            {
                Physics2D.IgnoreCollision(myCollider, playerCollider, false);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (ignorePlayerWhenUsingSkill &&
                GameManager.Instance.joystickDirectionIndicator.IsUsingSkill)
            {
                return; // 충돌 무시
            }

            // ✅ 이제는 PlayerDamaged 쪽에 위임
            int damage = GameManager.Instance.longRangeEnemyStats.attack;
            GameManager.Instance.playerDamaged.TakeDamage(damage);

            Destroy(gameObject);
        }
        else if (destroyOnObstacle && collision.CompareTag("Obstacle"))
        {
            Destroy(gameObject);
        }
        else if (destroyOnObstacle && collision.CompareTag("LaserNot"))
        {
            Destroy(gameObject);
        }
    }

}
