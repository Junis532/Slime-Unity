using DG.Tweening;
using System.Collections;
using UnityEngine;

public class ExplosionDronEnemy : EnemyBase
{
    private bool isLive = true;

    private SpriteRenderer spriter;
    private EnemyAnimation enemyAnimation;

    private Vector2 currentVelocity;
    private Vector2 currentDirection;

    public float smoothTime = 0.1f;
    public float explosionRange = 1.5f; // 폭발 범위
    public GameObject explosionEffectPrefab; // 폭발 이펙트

    void Start()
    {
        spriter = GetComponent<SpriteRenderer>();
        enemyAnimation = GetComponent<EnemyAnimation>();

        originalSpeed = GameManager.Instance.enemyStats.speed;
        speed = originalSpeed;
    }

    void Update()
    {
        if (!isLive) return;

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        Vector2 dirVec = (player.transform.position - transform.position);
        float distanceToPlayer = dirVec.magnitude;

        if (distanceToPlayer <= explosionRange)
        {
            Explode(player.transform.position);
            return;
        }

        Vector2 inputVec = dirVec.normalized;
        currentDirection = Vector2.SmoothDamp(currentDirection, inputVec, ref currentVelocity, smoothTime);
        Vector2 nextVec = currentDirection * speed * Time.deltaTime;
        transform.Translate(nextVec);

        // 방향 반전 및 애니메이션 처리
        if (currentDirection.magnitude > 0.01f)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (currentDirection.x < 0 ? -1 : 1);
            transform.localScale = scale;

            enemyAnimation.PlayAnimation(EnemyAnimation.State.Move);
        }
        else
        {
            enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);
        }
    }

    private void Explode(Vector3 position)
    {
        if (!isLive) return;
        isLive = false;

        // 폭발 이펙트 생성 및 0.3초 후 제거
        if (explosionEffectPrefab != null)
        {
            GameObject effect = Instantiate(explosionEffectPrefab, position, Quaternion.identity);
            Destroy(effect, 0.3f);
        }

        // ✅ 이제는 PlayerDamaged 쪽에 위임
        int damage = GameManager.Instance.enemyStats.attack;
        GameManager.Instance.playerDamaged.TakeDamage(damage);

        Destroy(gameObject);
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 플레이어와 충돌해도 바로 폭발할 수 있음 (옵션)
        if (!isLive) return;

        if (collision.CompareTag("Player"))
        {
            Explode(transform.position);
        }
    }
}
