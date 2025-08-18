using UnityEngine;
using DG.Tweening;
using System.Collections;

public class FireballAI : MonoBehaviour
{
    [Header("이동 관련 설정")]
    public float moveSpeed = 10f;
    public float followDuration = 0.3f;

    [Header("DOT 관련 설정")]
    public float duration = 5f;     // DOT 지속시간
    public float interval = 1f;     // DOT 한 틱 간격
    private int damagePerTick;      // 틱당 데미지

    private Transform target;
    private bool isFollowingPlayer = true;
    private Coroutine moveCoroutine;
    private Collider2D myCollider;
    private bool isDestroying = false;

    [Header("추적 이펙트 프리팹")]
    public GameObject trackingEffectPrefab;
    private GameObject trackingEffectInstance;

    private bool isApplyingDot = false; // 중복 DOT 방지
    private SpriteRenderer spriteRenderer;

    // 초기화
    public void InitializeBullet(Vector3 startPosition, float startAngle)
    {
        transform.position = startPosition;
        transform.rotation = Quaternion.Euler(0, 0, startAngle);

        // 공격력 기반 DOT 설정
        damagePerTick = Mathf.RoundToInt(GameManager.Instance.playerStats.attack * 0.5f);
        if (damagePerTick <= 0) damagePerTick = 1;

        if (myCollider != null) myCollider.enabled = true;

        // ✅ 생성 즉시 적 탐색 + 발사
        SwitchToEnemy();

        // 안전 장치 (10초 후 자동 삭제)
        Invoke(nameof(DestroySelf), 10f);
    }

    public void SyncSetRotation(float angle)
    {
        if (isFollowingPlayer)
            transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    void Awake()
    {
        myCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        transform.DOKill();
        isDestroying = false;
        isApplyingDot = false;
        CancelInvoke();

        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }

        isFollowingPlayer = false; // ✅ 바로 적으로 날아가므로 false
        target = null;

        if (myCollider != null)
            myCollider.enabled = false;

        if (spriteRenderer != null)
            spriteRenderer.color = new Color(1, 1, 1, 1);

        transform.localScale = Vector3.zero;
        Invoke(nameof(DestroySelf), 10f);

        transform.DOScale(0.5f, 0.3f).SetEase(Ease.OutBack).OnComplete(() =>
        {
            if (!gameObject.activeInHierarchy) return;

            if (myCollider != null)
                myCollider.enabled = true;
            SwitchToEnemy();
        });
    }

    void SwitchToEnemy()
    {
        isFollowingPlayer = false;
        FindClosestTarget();

        if (target != null)
        {
            moveCoroutine = StartCoroutine(MoveTowardsTarget());
        }
        else
        {
            DestroySelf();
        }
    }

    IEnumerator MoveTowardsTarget()
    {
        // 🔥 딜레이 없이 바로 방향 잡고 돌진
        Vector3 direction = (target.position - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        while (target != null && target.gameObject.activeInHierarchy && !isDestroying)
        {
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
        if (isDestroying || isApplyingDot) return;

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
                isApplyingDot = true;
                StartCoroutine(ApplyDotDamageAndDestroy(hp));
            }

            // ✅ Fireball을 보이지 않게 (DOT는 유지됨)
            if (spriteRenderer != null)
                spriteRenderer.color = new Color(1, 1, 1, 0);

            if (myCollider != null)
                myCollider.enabled = false;

            if (moveCoroutine != null)
            {
                StopCoroutine(moveCoroutine);
                moveCoroutine = null;
            }
        }
    }

    IEnumerator ApplyDotDamageAndDestroy(EnemyHP hp)
    {
        float elapsed = 0f;

        if (hp == null || hp.gameObject == null) yield break;
        if (!hp.gameObject.activeInHierarchy) yield break;

        // 첫 도트 즉시 적용
        hp.FireballTakeDamage(damagePerTick);

        while (elapsed + interval < duration)
        {
            yield return new WaitForSeconds(interval);

            if (hp == null || hp.gameObject == null) yield break;
            if (!hp.gameObject.activeInHierarchy) yield break;

            hp.FireballTakeDamage(damagePerTick);

            elapsed += interval;
        }

        DestroySelf();
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
