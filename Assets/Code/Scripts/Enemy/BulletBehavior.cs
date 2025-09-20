using UnityEngine;

/// <summary>
/// 총알 움직임과 시간 기반 소멸 관리 - Pool 적용
/// </summary>
public class BulletBehavior : MonoBehaviour
{
    private Vector2 direction;
    private float speed;
    private float lifeTime;
    private float timer = 0f;

    // 총알 세팅
    public void Initialize(Vector2 dir, float spd, float lifetime)
    {
        direction = dir;
        speed = spd;
        lifeTime = lifetime;
        timer = 0f;                // 풀 재사용 대비!
    }

    void OnEnable()
    {
        timer = 0f;   // 풀에서 재활성화 시 타이머 초기화
    }

    void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime);

        timer += Time.deltaTime;
        if (timer >= lifeTime)
        {
            PoolManager.Instance.ReturnToPool(gameObject); // Destroy -> ReturnToPool
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") || collision.CompareTag("Wall"))
        {
            PoolManager.Instance.ReturnToPool(gameObject); // Destroy -> ReturnToPool
        }
    }
}
