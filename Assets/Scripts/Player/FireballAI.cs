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
            moveSpeed = 0f;
            if (moveCoroutine != null) StopCoroutine(moveCoroutine);
            // 투사체가 장애물 위치에서 바로 삭제되도록
            DestroySelf();
            return;
        }

        if (other.CompareTag("Enemy") || other.CompareTag("DashEnemy") ||
            other.CompareTag("LongRangeEnemy") || other.CompareTag("PotionEnemy"))
        {
            EnemyHP hp = other.GetComponent<EnemyHP>();
            if (hp != null)
            {
                // DOT 적용 후 삭제
                StartCoroutine(ApplyDotDamageAndDestroy(hp));
            }

            if (myCollider != null)
                myCollider.enabled = false;

            if (moveCoroutine != null)
                StopCoroutine(moveCoroutine);

            // 자식 오브젝트 모두 비활성화
            foreach (Transform child in transform)
                child.gameObject.SetActive(false);

            // 스프라이트 투명 처리
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0f);
        }
    }


    IEnumerator ApplyDotDamageAndDestroy(EnemyHP hp)
    {
        float elapsed = 0f;
        if (hp == null) yield break;

        // 첫 도트 즉시 적용
        hp.FireballTakeDamage(damagePerTick);
        elapsed += interval;

        while (elapsed < duration)
        {
            yield return new WaitForSeconds(interval);

            // 적이 null이거나 비활성화, 혹은 체력이 0 이하이면 즉시 종료
            if (hp == null || !hp.gameObject.activeInHierarchy || hp.currentHP <= 0)
                break;

            hp.FireballTakeDamage(damagePerTick);
            elapsed += interval;
        }

        DestroySelf(); // DOT 끝나거나 적이 죽으면 Fireball 삭제
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
