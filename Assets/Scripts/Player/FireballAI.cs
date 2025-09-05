using UnityEngine;
using System.Collections;

public class FireballAI : MonoBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 10f;
    private Transform target;
    private Coroutine moveCoroutine;
    private Collider2D myCollider;
    private bool isDestroying = false;

    private Vector3 fixedDirection;
    public bool followEnemy = true;

    [Header("DOT 설정")]
    public float duration = 5f;
    public float interval = 1f;
    private int damagePerTick;

    void Awake()
    {
        myCollider = GetComponent<Collider2D>();
    }

    // 초기화
    public void InitializeBullet(Vector3 startPosition, float startAngle, bool follow = true)
    {
        transform.position = startPosition;
        transform.rotation = Quaternion.Euler(0, 0, startAngle);
        followEnemy = follow;

        // 공격력 기반 DOT
        damagePerTick = Mathf.RoundToInt(GameManager.Instance.playerStats.attack * 0.5f);
        if (damagePerTick <= 0) damagePerTick = 1;

        if (myCollider != null) myCollider.enabled = true;

        if (followEnemy)
        {
            SwitchToEnemy();
        }
        else
        {
            // 직선 이동
            float angleRad = startAngle * Mathf.Deg2Rad;
            fixedDirection = new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0);
            moveCoroutine = StartCoroutine(MoveStraight());
        }

        Invoke(nameof(DestroySelf), 10f);
    }

    IEnumerator MoveStraight()
    {
        while (!isDestroying)
        {
            transform.position += fixedDirection * moveSpeed * Time.deltaTime;
            yield return null;
        }
    }

    void SwitchToEnemy()
    {
        FindClosestTarget();
        if (target != null)
        {
            fixedDirection = (target.position - transform.position).normalized;
            float angle = Mathf.Atan2(fixedDirection.y, fixedDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
            moveCoroutine = StartCoroutine(MoveStraight()); // 적 방향으로 직선 이동
        }
        else
        {
            DestroySelf();
        }
    }

    void FindClosestTarget()
    {
        string[] enemyTags = { "Enemy", "DashEnemy", "LongRangeEnemy", "PotionEnemy" };
        float closestDist = Mathf.Infinity;
        Transform closest = null;

        foreach (string tag in enemyTags)
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject enemy in enemies)
            {
                float dist = Vector3.Distance(transform.position, enemy.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = enemy.transform;
                }
            }
        }

        target = closest;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isDestroying) return;

        if (other.CompareTag("Obstacle"))
        {
            AudioManager.Instance.PlaySFX(AudioManager.Instance.arrowWall);
            moveSpeed = 0f;
            if (moveCoroutine != null) StopCoroutine(moveCoroutine);
            DestroySelf();
            return;
        }

        if (other.CompareTag("Enemy") || other.CompareTag("DashEnemy") ||
            other.CompareTag("LongRangeEnemy") || other.CompareTag("PotionEnemy"))
        {
            EnemyHP hp = other.GetComponent<EnemyHP>();
            if (hp != null)
            {
                // DOT만 적용 (Fireball은 계속 날아감)
                StartCoroutine(ApplyDotDamage(hp));
            }

            Boss1HP bossHP = other.GetComponent<Boss1HP>();
            if (bossHP != null)
            {
                StartCoroutine(ApplyDotDamageToBoss(bossHP));
            }
        }

    }

    IEnumerator ApplyDotDamage(EnemyHP hp)
    {
        float elapsed = 0f;
        if (hp == null) yield break;

        hp.FireballTakeDamage(damagePerTick); // 첫 즉시 데미지
        elapsed += interval;

        while (elapsed < duration)
        {
            yield return new WaitForSeconds(interval);
            if (hp == null || !hp.gameObject.activeInHierarchy || hp.currentHP <= 0)
                break;

            hp.FireballTakeDamage(damagePerTick);
            elapsed += interval;
        }

        // DOT 끝나면 제거 (이동 중지)
        DestroySelf();
    }

    // Boss1용 DOT
    IEnumerator ApplyDotDamageToBoss(Boss1HP bossHP)
    {
        float elapsed = 0f;
        if (bossHP == null) yield break;

        // 첫 도트 즉시 적용
        bossHP.FireballTakeDamage(damagePerTick);
        elapsed += interval;

        while (elapsed < duration)
        {
            yield return new WaitForSeconds(interval);

            if (bossHP == null || !bossHP.gameObject.activeInHierarchy || bossHP.currentHP <= 0)
                break;

            bossHP.FireballTakeDamage(damagePerTick);
            elapsed += interval;
        }
    }

    void DestroySelf()
    {
        if (isDestroying) return;
        isDestroying = true;

        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);

        GameManager.Instance.poolManager.ReturnToPool(gameObject);
    }
}
