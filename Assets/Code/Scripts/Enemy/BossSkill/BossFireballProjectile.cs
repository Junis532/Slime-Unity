using UnityEngine;
using System.Collections; // DOTween을 위해 System.Collections 사용

public class BossFireballProjectile : MonoBehaviour
{
    public float speed = 10f;
    public float lifeTime = 2f;
    //private int damage;

    private Vector2 direction;

    [Header("Trail Options")]
    public bool addTrail = true;
    public float trailTime = 0.3f;
    public float trailStartWidth = 0.2f;
    public Gradient trailGradient; // Inspector에서 설정하거나 Init에서 기본값 사용

    private TrailRenderer tr;

    public void Init(Vector2 dir)
    {
        direction = dir.normalized;

        // 스프라이트가 이동 방향을 바라보도록 회전 (90도 오프셋)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        // 화염구 스프라이트는 보통 위(Y+)를 향하고 있으므로, 회전 보정이 필요합니다.
        // 현재 코드는 X+를 기준으로 회전하므로, 90도 오프셋이 필요할 수도 있습니다.
        // 여기서는 오프셋 없이 방향 벡터와 맞춥니다.
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // 트레일 초기화
        if (addTrail)
        {
            EnsureTrail();
        }

        // 데미지 계산
        //damage = Mathf.FloorToInt(GameManager.Instance.boss1Stats.attack * 2.5f);

        Destroy(gameObject, lifeTime);
    }

    // 트레일 렌더러를 설정하는 유틸리티 메서드
    private void EnsureTrail()
    {
        tr = GetComponent<TrailRenderer>();
        if (!tr) tr = gameObject.AddComponent<TrailRenderer>();

        // 공통 설정
        tr.time = trailTime;
        tr.startWidth = trailStartWidth;
        tr.endWidth = 0.0f; // 꼬리 쪽은 얇아지도록
        tr.minVertexDistance = 0.02f;
        tr.sortingLayerName = "Foreground";
        tr.sortingOrder = 10; // (보스보다 높은 순서)

        // 머티리얼 설정 (Additive 셰이더를 사용하여 발광 효과)
        var sh = Shader.Find("Particles/Additive");
        if (sh == null) sh = Shader.Find("Sprites/Default"); // 폴백
        if (!tr.material || tr.material.shader != sh) tr.material = new Material(sh);

        // 색상 그라데이션 설정
        if (trailGradient == null || trailGradient.colorKeys.Length == 0)
        {
            // 기본값: 주황/빨강 (화염구 느낌) → 투명
            trailGradient = new Gradient();
            trailGradient.SetKeys(
                new[] {
                    new GradientColorKey(new Color(1f, 0.8f, 0.2f, 1f), 0f), // 시작(밝은 주황)
                    new GradientColorKey(new Color(1f, 0.3f, 0.1f, 1f), 0.5f) // 중간(빨강)
                },
                new[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f) // 끝(투명)
                }
            );
        }
        tr.colorGradient = trailGradient;
    }

    void Update()
    {
        // Rigidbody2D를 사용하지 않고 직접 위치 이동
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // 스킬 사용 중이면 충돌 무시
            if (GameManager.Instance.joystickDirectionIndicator.IsUsingSkill)
            {
                Debug.Log("스킬 사용 중이라 몬스터 데미지 무시");
                return;
            }

            int damage = GameManager.Instance.boss1Stats.attack;

            // 넉백 방향 계산을 위해 현재 보스의 위치를 '적 위치'로 전달
            Vector3 enemyPosition = transform.position;

            // 플레이어 데미지 처리
            GameManager.Instance.playerDamaged.TakeDamage(damage, enemyPosition);

            // 🔥 FireBoss에 플레이어 히트 알리기
            FireBoss boss = Object.FindFirstObjectByType<FireBoss>();
            if (boss != null)
            {
                boss.OnPlayerHit();
                Debug.Log("플레이어 맞아서 스킬 종료");
            }

            // 화염구 제거
            Destroy(gameObject);
        }
    }

}