using UnityEngine;

public class MeteorDamage : MonoBehaviour
{
    private int damage;

    [Header("맞았을 때 표시할 이펙트 프리팹")]
    public GameObject hitEffectPrefab;

    public void Init()
    {
        damage = Mathf.FloorToInt(GameManager.Instance.playerStats.attack * 2f);
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

                // 이펙트 생성 후 0.3초 뒤 제거
                if (hitEffectPrefab != null)
                {
                    GameObject effect = Instantiate(hitEffectPrefab, other.transform.position, Quaternion.identity);
                    Destroy(effect, 0.3f);
                }
            }
        }
    }
}
