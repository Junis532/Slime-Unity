using UnityEngine;
using DG.Tweening;
using System.Collections;

public class FireballAI : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float followDuration = 0.3f;

    public float duration = 5f;
    public float interval = 1f;
    private int damagePerTick;

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

        // ✅ 공격력 기반 데미지 Init
        damagePerTick = Mathf.FloorToInt(GameManager.Instance.playerStats.attack * 1.5f);
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
            {
                StartCoroutine(ApplyDotDamage(hp));
            }

            DestroySelf();
        }
    }

    IEnumerator ApplyDotDamage(EnemyHP hp)
    {
        float elapsed = 0f;

        while (elapsed < duration && hp != null)
        {
            hp.SkillTakeDamage(damagePerTick);
            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }
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
