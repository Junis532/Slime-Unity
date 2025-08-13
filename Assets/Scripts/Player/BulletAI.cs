//using UnityEngine;
//using DG.Tweening;
//using System.Collections;

//public class BulletAI : MonoBehaviour
//{
//    public float moveSpeed = 20f;
//    public float followDuration = 0.3f;

//    private SlowSkill slowSkill;
//    private BulletSpawner bulletSpawner;

//    private Transform target;
//    private bool isFollowingPlayer = true;
//    private Coroutine moveCoroutine;
//    private Collider2D myCollider;
//    private bool isDestroying = false;

//    //[Header("🔍 추적 이펙트 프리팹")]
//    //public GameObject trackingEffectPrefab;
//    //private GameObject trackingEffectInstance;

//    private Vector3 fixedDirection; // 타겟 추적 시작 시 고정된 방향

//    public void InitializeBullet(Vector3 startPosition, float startAngle)
//    {
//        transform.position = startPosition;
//        transform.rotation = Quaternion.Euler(0, 0, startAngle);
//        isFollowingPlayer = true;
//    }

//    public void SyncSetRotation(float angle)
//    {
//        if (isFollowingPlayer)
//            transform.rotation = Quaternion.Euler(0, 0, angle);
//    }

//    void Awake()
//    {
//        myCollider = GetComponent<Collider2D>();
//        slowSkill = Object.FindFirstObjectByType<SlowSkill>();
//        bulletSpawner = Object.FindFirstObjectByType<BulletSpawner>();
//    }

//    void OnEnable()
//    {
//        transform.DOKill();
//        isDestroying = false;
//        CancelInvoke();

//        if (moveCoroutine != null)
//        {
//            StopCoroutine(moveCoroutine);
//            moveCoroutine = null;
//        }

//        isFollowingPlayer = true;
//        target = null;

//        if (myCollider != null)
//            myCollider.enabled = false;

//        transform.localScale = Vector3.zero;

//        Invoke(nameof(DestroySelf), 10f);

//        transform.DOScale(0.5f, 0.3f).SetEase(Ease.OutBack).OnComplete(() =>
//        {
//            if (!gameObject.activeInHierarchy) return; // 오브젝트가 비활성화 상태면 실행 X

//            if (myCollider != null)
//                myCollider.enabled = true;

//            AudioManager.Instance.PlaySFX(AudioManager.Instance.arrowSound);

//            StartCoroutine(DelayedSwitchToEnemy(followDuration));
//        });
//    }

//    IEnumerator DelayedSwitchToEnemy(float delay)
//    {
//        yield return new WaitForSeconds(delay);

//        if (!gameObject.activeInHierarchy) yield break;

//        SwitchToEnemy();
//    }

//    void SwitchToEnemy()
//    {
//        isFollowingPlayer = false;
//        FindClosestTarget();

//        if (target != null)
//        {
//            // 타겟 위치로부터 방향 고정
//            fixedDirection = (target.position - transform.position).normalized;

//            moveCoroutine = StartCoroutine(MoveTowardsTarget());
//        }
//        else
//        {
//            DestroySelf();
//        }
//    }

//    IEnumerator MoveTowardsTarget()
//    {
//        // 고정된 방향으로 회전 한 번만
//        float angle = Mathf.Atan2(fixedDirection.y, fixedDirection.x) * Mathf.Rad2Deg;
//        transform.rotation = Quaternion.Euler(0, 0, angle);

//        while (target != null && target.gameObject.activeInHierarchy && !isDestroying)
//        {
//            transform.position += fixedDirection * moveSpeed * Time.deltaTime;
//            yield return null;
//        }

//        DestroySelf();
//    }

//    void FindClosestTarget()
//    {
//        string[] enemyTags = { "Enemy", "DashEnemy", "LongRangeEnemy", "PotionEnemy" };
//        float closestDist = Mathf.Infinity;
//        Transform closest = null;

//        foreach (string tag in enemyTags)
//        {
//            GameObject[] enemies = GameObject.FindGameObjectsWithTag(tag);
//            foreach (GameObject enemy in enemies)
//            {
//                float dist = Vector3.Distance(transform.position, enemy.transform.position);
//                if (dist < closestDist)
//                {
//                    closestDist = dist;
//                    closest = enemy.transform;
//                }
//            }
//        }

//        target = closest;
//    }

//    void OnTriggerEnter2D(Collider2D other)
//    {
//        if (isDestroying) return;

//        // 벽(Obstacle) 충돌 처리
//        if (other.CompareTag("Obstacle"))
//        {
//            transform.DOKill();
//            moveSpeed = 0f;

//            if (moveCoroutine != null)
//            {
//                StopCoroutine(moveCoroutine);
//                moveCoroutine = null;
//            }

//            if (myCollider != null)
//                myCollider.enabled = false;

//            //if (trackingEffectInstance != null)
//            //{
//            //    Destroy(trackingEffectInstance);
//            //    trackingEffectInstance = null;
//            //}

//            Invoke(nameof(DestroySelf), 1.5f);
//            return;
//        }

//        // 적 충돌 처리
//        if (other.CompareTag("Enemy") || other.CompareTag("DashEnemy") ||
//            other.CompareTag("LongRangeEnemy") || other.CompareTag("PotionEnemy"))
//        {
//            EnemyHP hp = other.GetComponent<EnemyHP>();
//            if (hp != null)
//                hp.TakeDamage();

//            if (bulletSpawner != null && bulletSpawner.slowSkillActive && slowSkill != null)
//            {
//                EnemyBase enemyBase = other.GetComponent<EnemyBase>();
//                if (enemyBase != null)
//                {
//                    slowSkill.ApplySlow(enemyBase);
//                }
//            }

//            DestroySelf();
//        }
//    }

//    void DestroySelf()
//    {
//        if (isDestroying) return;
//        isDestroying = true;

//        CancelInvoke();
//        transform.DOKill();

//        if (moveCoroutine != null)
//        {
//            StopCoroutine(moveCoroutine);
//            moveCoroutine = null;
//        }

//        //if (trackingEffectInstance != null)
//        //{
//        //    Destroy(trackingEffectInstance);
//        //    trackingEffectInstance = null;
//        //}

//        GameManager.Instance.poolManager.ReturnToPool(gameObject);
//    }
//}

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

    public void InitializeBullet(Vector3 startPosition, float startAngle)
    {
        transform.position = startPosition;
        transform.rotation = Quaternion.Euler(0, 0, startAngle);
        if (myCollider != null) myCollider.enabled = true;

        SwitchToEnemy(); // 생성 즉시 발사
        Invoke(nameof(DestroySelf), 10f);
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
            if (hp != null) hp.TakeDamage();

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
