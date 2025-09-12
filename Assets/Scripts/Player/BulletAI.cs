using UnityEngine;
using System.Collections;

public class BulletAI : MonoBehaviour
{
    public float moveSpeed = 15f;
    private Transform target;
    private bool isDestroying = false;
    private Coroutine moveCoroutine;
    private Collider2D myCollider;
    private BulletSpawner bulletSpawner;

    private Vector3 fixedDirection;
    public bool followEnemy = true;

    [Header("이펙트 프리팹")]
    public GameObject hitEffectPrefab; // obstacle 충돌 시 생성될 이펙트

    public void ResetBullet()
    {
        isDestroying = false;
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }
    }

    public void InitializeBullet(Vector3 startPosition, float startAngle, bool follow = true)
    {
        transform.position = startPosition;
        transform.rotation = Quaternion.Euler(0, 0, startAngle);
        followEnemy = follow;

        if (myCollider != null) myCollider.enabled = true;

        if (moveCoroutine != null) StopCoroutine(moveCoroutine);

        if (followEnemy)
            SwitchToEnemy();
        else
        {
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

    void Awake()
    {
        myCollider = GetComponent<Collider2D>();
        bulletSpawner = Object.FindFirstObjectByType<BulletSpawner>();
    }

    void SwitchToEnemy()
    {
        FindClosestTarget();
        if (target != null)
        {
            fixedDirection = (target.position - transform.position).normalized;
            moveCoroutine = StartCoroutine(MoveTowardsTarget());
        }
        else
        {
            DestroySelf();
        }
    }

    IEnumerator MoveTowardsTarget()
    {
        float angle = Mathf.Atan2(fixedDirection.y, fixedDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        while (target != null && target.gameObject.activeInHierarchy && !isDestroying)
        {
            transform.position += fixedDirection * moveSpeed * Time.deltaTime;
            yield return null;
        }
        DestroySelf();
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
            if (moveCoroutine != null) StopCoroutine(moveCoroutine);

            // ✅ 충돌 위치에 이펙트 생성
            if (hitEffectPrefab != null)
            {
                GameObject effect = Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
                Destroy(effect, 0.3f); // 0.3초 후 삭제
            }

            Invoke(nameof(DestroySelf), 0.1f); // 화살은 바로 풀로 반환
            return;
        }

        if (other.CompareTag("Enemy") || other.CompareTag("DashEnemy") ||
            other.CompareTag("LongRangeEnemy") || other.CompareTag("PotionEnemy"))
        {
            EnemyHP hp = other.GetComponent<EnemyHP>();
            if (hp != null) hp.TakeDamage();
            Boss1HP bossHP = other.GetComponent<Boss1HP>();
            if (bossHP != null) bossHP.TakeDamage();

            if (bulletSpawner != null && bulletSpawner.slowSkillActive)
            {
                EnemyBase enemyBase = other.GetComponent<EnemyBase>();
                if (enemyBase != null)
                {
                    Object.FindFirstObjectByType<SlowSkill>()?.ApplySlow(enemyBase);
                }
            }
            DestroySelf();
        }
    }

    void DestroySelf()
    {
        if (isDestroying) return;
        isDestroying = true;

        if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        GameManager.Instance.poolManager.ReturnToPool(gameObject);
    }
}
