using UnityEngine;

public class LongRangeEnemyBullet : MonoBehaviour
{
    // 이걸 true로 하면 Obstacle 태그에 닿으면 총알이 사라짐
    public bool destroyOnObstacle = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // 데미지 주기
            GameManager.Instance.playerStats.currentHP -= GameManager.Instance.longRangeEnemyStats.attack;
            GameManager.Instance.playerDamaged.PlayDamageEffect(); // 플레이어 데미지 이펙트 재생

            // 체력이 0 이하이면 죽음 처리
            if (GameManager.Instance.playerStats.currentHP <= 0)
            {
                GameManager.Instance.playerStats.currentHP = 0;
                //GameManager.Instance.PlayerDie?.Invoke(); // 함수가 있다면 호출
            }

            Destroy(gameObject);
        }
        else if (destroyOnObstacle && collision.CompareTag("Obstacle"))
        {
            // Obstacle 태그에 닿았을 때 총알 삭제
            Destroy(gameObject);
        }
    }
}
