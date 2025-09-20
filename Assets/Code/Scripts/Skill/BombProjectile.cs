using UnityEngine;

public class BombProjectile : MonoBehaviour
{
    public float speed = 4f;
    public float lifeTime = 2.5f;

    public float explosionRadius = 2f;               // 폭발 반경
    public GameObject explosionEffect;               // 폭발 이펙트 프리팹 (옵션)

    private int damage;
    private Vector2 direction;

    public void Init(Vector2 dir)
    {
        direction = dir.normalized;

        // 플레이어 공격력 기반 데미지 설정 (예: 3배)
        damage = Mathf.FloorToInt(GameManager.Instance.playerStats.attack * 0.5f);

        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (IsEnemyTag(other.tag))
        {
            Explode();
        }
    }

    void Explode()
    {
        // 폭발 이펙트 생성
        if (explosionEffect != null)
        {
            Instantiate(explosionEffect, transform.position, Quaternion.identity);
        }

        // 반경 내의 적 탐색
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (Collider2D hit in hits)
        {
            if (IsEnemyTag(hit.tag))
            {
                EnemyHP hp = hit.GetComponent<EnemyHP>();
                if (hp != null)
                {
                    hp.SkillTakeDamage(damage);
                    Debug.Log($"Bomb explosion hit {hit.name}, dealt {damage} damage.");
                }
            }
        }

        Destroy(gameObject); // 본체 제거
    }

    bool IsEnemyTag(string tag)
    {
        return tag == "Enemy" || tag == "DashEnemy" || tag == "LongRangeEnemy" || tag == "PotionEnemy";
    }

    void OnDrawGizmosSelected()
    {
        // Scene 뷰에서 폭발 반경 시각화
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
