using UnityEngine;
using System.Collections.Generic;

public class WindWallKnockback : MonoBehaviour
{
    [Header("적에게 줄 피해량 (플레이어 공격력 비례)")]
    public float damageMultiplier = 0.1f;
    private int damage;

    [Header("바람막 지속시간")]
    public float lifetime = 3f;

    [Header("지속 데미지 간격(초)")]
    public float damageInterval = 0.5f;  // 0.5초마다 데미지

    // 적별 데미지 쿨타임 관리용
    private Dictionary<Collider2D, float> damageTimers = new Dictionary<Collider2D, float>();

    void Start()
    {
        damage = Mathf.FloorToInt(GameManager.Instance.playerStats.attack * damageMultiplier);
        Destroy(gameObject, lifetime);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsEnemy(collision.collider))
        {
            EnemyHP hp = collision.collider.GetComponent<EnemyHP>();
            if (hp != null)
            {
                hp.SkillTakeDamage(damage);
            }

            damageTimers[collision.collider] = 0f;
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (IsEnemy(collision.collider))
        {
            if (!damageTimers.ContainsKey(collision.collider))
                damageTimers[collision.collider] = 0f;

            damageTimers[collision.collider] += Time.deltaTime;

            if (damageTimers[collision.collider] >= damageInterval)
            {
                EnemyHP hp = collision.collider.GetComponent<EnemyHP>();
                if (hp != null)
                {
                    hp.SkillTakeDamage(damage);
                }

                damageTimers[collision.collider] = 0f;
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (damageTimers.ContainsKey(collision.collider))
        {
            damageTimers.Remove(collision.collider);
        }
    }

    // 적 태그만 판별하는 함수
    private bool IsEnemy(Collider2D other)
    {
        return other.CompareTag("Enemy") ||
               other.CompareTag("DashEnemy") ||
               other.CompareTag("LongRangeEnemy") ||
               other.CompareTag("PotionEnemy");
    }
}
