using UnityEngine;
using DG.Tweening;
using System.Collections;

public class BulletAI : MonoBehaviour
{
    public float moveSpeed = 20f;
    public float followDuration = 0.3f;
    public bool slow = false;

    private Transform target;
    private bool isFollowingPlayer = true;
    private Coroutine moveCoroutine;
    private Collider2D myCollider;
    private bool isDestroying = false;

    [Header("🔍 추적 이펙트 프리팹")]
    public GameObject trackingEffectPrefab;
    private GameObject trackingEffectInstance;

    public void InitializeBullet(Vector3 startPosition, float startAngle)
    {
        transform.position = startPosition;
        transform.rotation = Quaternion.Euler(0, 0, startAngle);
        isFollowingPlayer = true;
    }

    public void SyncSetRotation(float angle)
    {
        if (isFollowingPlayer)
            transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    void Awake()
    {
        myCollider = GetComponent<Collider2D>();
    }

    void OnEnable()
    {
        transform.DOKill();
        isDestroying = false;
        CancelInvoke();

        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }

        isFollowingPlayer = true;
        target = null;

        if (myCollider != null)
            myCollider.enabled = false;

        transform.localScale = Vector3.zero;

        Invoke(nameof(DestroySelf), 10f);

        transform.DOScale(0.5f, 0.3f).SetEase(Ease.OutBack).OnComplete(() =>
        {
            if (!gameObject.activeInHierarchy) return; // 🔐 오브젝트가 비활성화 상태면 실행 X

            if (myCollider != null)
                myCollider.enabled = true;

            StartCoroutine(DelayedSwitchToEnemy(followDuration));
        });

    }

    IEnumerator DelayedSwitchToEnemy(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (!gameObject.activeInHierarchy) yield break;

        SwitchToEnemy();
    }

    void SwitchToEnemy()
    {
        isFollowingPlayer = false;
        FindClosestTarget();

        if (target != null)
        {
            if (trackingEffectPrefab != null)
            {
                Vector3 offset = new Vector3(0f, -0.1f, 0f);
                trackingEffectInstance = Instantiate(trackingEffectPrefab, target.position + offset, Quaternion.identity);
                trackingEffectInstance.transform.SetParent(target);
            }

            moveCoroutine = StartCoroutine(MoveTowardsTarget());
        }
        else
        {
            DestroySelf();
        }
    }

    IEnumerator MoveTowardsTarget()
    {
        while (target != null && target.gameObject.activeInHierarchy && !isDestroying)
        {
            Vector3 direction = (target.position - transform.position).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
            transform.position += direction * moveSpeed * Time.deltaTime;
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

        if (other.CompareTag("Enemy") || other.CompareTag("DashEnemy") ||
            other.CompareTag("LongRangeEnemy") || other.CompareTag("PotionEnemy"))
        {
            EnemyHP hp = other.GetComponent<EnemyHP>();
            if (hp != null)
                hp.TakeDamage();

            // ✅ 슬로우 적용
            if (slow)
            {
                EnemyBase enemyBase = other.GetComponent<EnemyBase>();
                if (enemyBase != null)
                {
                    StartCoroutine(SlowEnemy(enemyBase, 0.5f));
                }
            }

            DestroySelf();
        }
    }

    // BulletAI 슬로우 코루틴에서 로그 추가 및 안전성 체크

    IEnumerator SlowEnemy(EnemyBase enemy, float slowRatio)
    {
        if (enemy == null)
        {
            Debug.LogWarning("SlowEnemy: enemy is null at start");
            yield break;
        }

        float original = enemy.originalSpeed;

        Debug.Log($"SlowEnemy: Applying slow. originalSpeed={original}, slowRatio={slowRatio}");

        enemy.SetSpeed(original * slowRatio);

        yield return new WaitForSeconds(1f);

        Debug.Log("SlowEnemy: Restoring original speed");
        enemy.SetSpeed(original);
    }



    void DestroySelf()
    {
        if (isDestroying) return;
        isDestroying = true;

        CancelInvoke();
        transform.DOKill();

        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }

        if (trackingEffectInstance != null)
        {
            Destroy(trackingEffectInstance);
            trackingEffectInstance = null;
        }

        GameManager.Instance.poolManager.ReturnToPool(gameObject);
    }
}
