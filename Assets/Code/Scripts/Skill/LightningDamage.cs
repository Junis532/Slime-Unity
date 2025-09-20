using UnityEngine;

public class LightningDamage : MonoBehaviour
{
    private int damage;

    [Header("맞았을 때 표시할 이펙트 프리팹")]
    public GameObject hitEffectPrefab;

    // 데미지 초기화 (게임매니저에서 공격력 가져와서 계산)
    public void Init()
    {
        damage = Mathf.FloorToInt(GameManager.Instance.playerStats.attack * 2.5f);
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
                Debug.Log($"Lightning hit {other.name}, dealt {damage} damage.");
            }

            if (hitEffectPrefab != null)
            {
                GameObject effect = Instantiate(hitEffectPrefab, other.transform.position, Quaternion.identity);
                Destroy(effect, 0.3f);
            }
        }
    }
}
