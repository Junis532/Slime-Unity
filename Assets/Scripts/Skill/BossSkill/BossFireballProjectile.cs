using UnityEngine;

public class BossFireballProjectile : MonoBehaviour
{
    public float speed = 10f;
    public float lifeTime = 2f;
    //private int damage;

    private Vector2 direction;

    public void Init(Vector2 dir)
    {
        direction = dir.normalized;

        // 스프라이트가 이동 방향을 바라보도록 회전 (90도 오프셋)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // 데미지 계산
        //damage = Mathf.FloorToInt(GameManager.Instance.boss1Stats.attack * 2.5f);

        Destroy(gameObject, lifeTime);
    }



    void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {

        if (collision.CompareTag("Player"))
        {
            int damage = GameManager.Instance.boss1Stats.attack;
            GameManager.Instance.playerStats.currentHP -= damage;
            GameManager.Instance.playerDamaged.PlayDamageEffect(); // 플레이어 데미지 이펙트 재생

            if (GameManager.Instance.playerStats.currentHP <= 0)
            {
                GameManager.Instance.playerStats.currentHP = 0;
                // 죽음 처리 함수 호출 가능
            }
        }
    }
}
