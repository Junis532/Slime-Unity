using UnityEngine;

public class FireballProjectile : MonoBehaviour
{
    public float speed = 10f;
    public float lifeTime = 2f;
    private int damage;

    private Vector2 direction;

    public void Init(Vector2 dir)
    {
        direction = dir.normalized;

        // Fireball 생성 시 현재 공격력을 기반으로 데미지 계산
        damage = Mathf.FloorToInt(GameManager.Instance.playerStats.attack * 2.5f);

        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy") || other.CompareTag("DashEnemy") ||
            other.CompareTag("LongRangeEnemy") || other.CompareTag("PotionEnemy"))
        {
            EnemyHP hp = other.GetComponent<EnemyHP>();
            if (hp != null)
            {
                hp.SkillTakeDamage(damage);
                Debug.Log($"Fireball hit {other.name}, dealt {damage} damage.");
            }
        }
    }
}
