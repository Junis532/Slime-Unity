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
            // 스킬 사용 중이면 충돌 무시
            if (ignorePlayerWhenUsingSkill &&
                GameManager.Instance.joystickDirectionIndicator.IsUsingSkill)
            {
                return;
            }

            int damage = GameManager.Instance.longRangeEnemyStats.attack;

            // 넉백 방향 계산을 위해 현재 오브젝트(투사체)의 위치를 '적 위치'로 전달합니다.
            // 이 위치를 기준으로 PlayerDamaged.cs가 넉백 방향을 계산합니다.
            Vector3 enemyPosition = transform.position;

            // 수정된 PlayerDamaged.TakeDamage(데미지, 적 위치) 형식으로 호출
            // 기존의 collision과 contactPoint 인수는 제거됩니다.
            GameManager.Instance.playerDamaged.TakeDamage(damage, enemyPosition);

            // 플레이어에게 피해를 줬으니 투사체 파괴
            Destroy(gameObject);
        }
        // 장애물 충돌 시 파괴
        else if (destroyOnObstacle && collision.CompareTag("Obstacle"))
        {
            Destroy(gameObject);
        }
        // LaserNot 태그를 가진 오브젝트 충돌 시 파괴
        else if (destroyOnObstacle && collision.CompareTag("LaserNot"))
        {
            Destroy(gameObject);
        }
    }

}
