using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LongRangeEnemyBullet : MonoBehaviour
{
    [Header("Collision")]
    public bool destroyOnObstacle = false;
    public bool ignorePlayerWhenUsingSkill = true;

    private Collider2D myCollider;
    private Collider2D playerCollider;
    private Rigidbody2D rb;
    private SpriteRenderer bulletSR;

    // ─────────────────────────────────────────────
    // Ghost Trail(잔상) – TrailRenderer 없이 구현
    // ─────────────────────────────────────────────
    [Header("Ghost Trail (No TrailRenderer)")]
    [Tooltip("잔상 트레일 사용 여부")]
    public bool enableGhostTrail = true;

    [Tooltip("잔상 생성 간격(초)")]
    public float ghostSpawnInterval = 0.045f;

    [Tooltip("잔상 생존 시간(초)")]
    public float ghostLifetime = 0.25f;

    [Tooltip("잔상 시작/끝 알파")]
    [Range(0f, 1f)] public float ghostStartAlpha = 0.6f;
    [Range(0f, 1f)] public float ghostEndAlpha = 0.0f;

    [Tooltip("잔상 시작/끝 스케일 배율")]
    public float ghostStartScale = 1.0f;
    public float ghostEndScale = 0.75f;

    [Tooltip("잔상 생성 최소 속도 제곱값(속도가 너무 느리면 생성 X)")]
    public float minVelocitySqrForGhost = 0.01f;

    [Tooltip("동시에 유지할 잔상 최대 개수(풀 크기)")]
    public int maxGhostPool = 24;

    [Tooltip("잔상에 적용할 머티리얼(비워두면 Bullet의 머티리얼/디폴트 사용)")]
    public Material ghostMaterialOverride;

    [Tooltip("TimeScale 영향을 받지 않게 할지")]
    public bool useUnscaledTimeForGhost = false;

    [Tooltip("발사 직후 즉시 잔상 1개 생성할지")]
    public bool burstOnFire = true;

    private float ghostTimer = 0f;
    private List<GameObject> ghostPool;
    private int ghostPoolIndex = 0;
    private bool ghostActive = false;

    void Awake()
    {
        myCollider = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
        bulletSR = GetComponentInChildren<SpriteRenderer>(); // 자식에 둘 수도 있으니 InChildren

        // Player 태그로 찾아서 Collider 가져오기
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerCollider = playerObj.GetComponent<Collider2D>();
        }

        // 고스트 풀 준비
        if (enableGhostTrail)
            InitGhostPool();
    }

    void OnEnable()
    {
        // 잔상 타이머 초기화
        ghostTimer = 0f;
        ghostActive = enableGhostTrail;

        // 풀 객체 초기화
        if (enableGhostTrail && ghostPool != null)
        {
            for (int i = 0; i < ghostPool.Count; i++)
            {
                ghostPool[i].SetActive(false);
            }
            ghostPoolIndex = 0;
        }
    }

    void OnDisable()
    {
        ghostActive = false;
        // 잔상들 비활성화(풀 유지)
        if (ghostPool != null)
        {
            for (int i = 0; i < ghostPool.Count; i++)
                ghostPool[i].SetActive(false);
        }
    }

    void Update()
    {
        // 스킬 사용 중엔 플레이어와 충돌 무시
        if (ignorePlayerWhenUsingSkill && GameManager.Instance.joystickDirectionIndicator != null && playerCollider != null)
        {
            bool usingSkill = GameManager.Instance.joystickDirectionIndicator.IsUsingSkill;
            Physics2D.IgnoreCollision(myCollider, playerCollider, usingSkill);
        }

        // 고스트 트레일 생성
        if (ghostActive && bulletSR != null)
        {
            float dt = useUnscaledTimeForGhost ? Time.unscaledDeltaTime : Time.deltaTime;
            ghostTimer += dt;

            bool movingEnough = (rb != null) ? (rb.linearVelocity.sqrMagnitude > minVelocitySqrForGhost) : true;

            if (movingEnough && ghostTimer >= ghostSpawnInterval)
            {
                ghostTimer = 0f;
                SpawnGhost();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // 스킬 사용 중이면 충돌 무시
            if (ignorePlayerWhenUsingSkill &&
                GameManager.Instance.joystickDirectionIndicator.IsUsingSkill)
            {
                return;
            }

            int damage = GameManager.Instance.longRangeEnemyStats.attack;

            // 넉백 방향 계산을 위해 적 위치 = 현재 투사체 위치 전달
            Vector3 enemyPosition = transform.position;

            GameManager.Instance.playerDamaged.TakeDamage(damage, enemyPosition);

            Destroy(gameObject);
        }
        // 장애물 충돌 시 파괴
        else if (destroyOnObstacle && collision.CompareTag("Obstacle"))
        {
            Destroy(gameObject);
        }
        // LaserNot 태그 충돌 시 파괴
        else if (destroyOnObstacle && collision.CompareTag("LaserNot"))
        {
            Destroy(gameObject);
        }
    }

    // ─────────────────────────────────────────────
    // Public: 발사 직후 외부에서 호출하면 즉시 잔상 1개 + 활성화
    // ─────────────────────────────────────────────
    public void OnFired()
    {
        ghostActive = enableGhostTrail;
        if (burstOnFire && enableGhostTrail)
            SpawnGhost();
    }

    // ─────────────────────────────────────────────
    // Ghost Trail 구현부
    // ─────────────────────────────────────────────
    private void InitGhostPool()
    {
        if (ghostPool != null) return;

        ghostPool = new List<GameObject>(maxGhostPool);
        for (int i = 0; i < maxGhostPool; i++)
        {
            GameObject g = new GameObject("BulletGhost");
            g.transform.SetParent(null); // 풀은 씬 루트에 둠
            var sr = g.AddComponent<SpriteRenderer>();

            // 기본 설정: 총알 스프라이트 복제될 예정(스폰 때 복사)
            sr.sprite = bulletSR != null ? bulletSR.sprite : null;
            sr.sortingLayerID = bulletSR != null ? bulletSR.sortingLayerID : 0;
            sr.sortingOrder = (bulletSR != null ? bulletSR.sortingOrder : 0) - 1; // 총알보다 살짝 뒤

            if (ghostMaterialOverride != null)
                sr.material = ghostMaterialOverride;
            else if (bulletSR != null && bulletSR.sharedMaterial != null)
                sr.material = bulletSR.sharedMaterial;

            g.SetActive(false);
            ghostPool.Add(g);
        }
    }

    private void SpawnGhost()
    {
        if (ghostPool == null || ghostPool.Count == 0) return;

        GameObject ghost = ghostPool[ghostPoolIndex];
        ghostPoolIndex = (ghostPoolIndex + 1) % ghostPool.Count;

        // 위치/회전/스케일 복사
        ghost.transform.position = transform.position;
        ghost.transform.rotation = transform.rotation;
        ghost.transform.localScale = transform.lossyScale * ghostStartScale;

        // 스프라이트/색상 복사
        var gsr = ghost.GetComponent<SpriteRenderer>();
        if (gsr != null)
        {
            if (bulletSR != null)
            {
                gsr.sprite = bulletSR.sprite;
                // 컬러 알파만 덮어쓰기
                Color c = bulletSR.color;
                c.a = ghostStartAlpha;
                gsr.color = c;

                // 정렬/레이어 동기화(총알보다 살짝 뒤에 그리려면 -1 유지)
                gsr.sortingLayerID = bulletSR.sortingLayerID;
                gsr.sortingOrder = bulletSR.sortingOrder - 1;

                // 머티리얼
                if (ghostMaterialOverride != null)
                    gsr.material = ghostMaterialOverride;
                else if (bulletSR.sharedMaterial != null)
                    gsr.material = bulletSR.sharedMaterial;
            }
            else
            {
                Color c = gsr.color;
                c.a = ghostStartAlpha;
                gsr.color = c;
            }
        }

        ghost.SetActive(true);

        // 기존에 돌고 있던 페이드 코루틴이 있다면 멈추고 다시 시작
        //(간단하게 컴포넌트 붙여서 관리)
        var fader = ghost.GetComponent<_GhostFader>();
        if (fader == null) fader = ghost.AddComponent<_GhostFader>();
        fader.Begin(ghostLifetime, ghostStartAlpha, ghostEndAlpha, ghostStartScale, ghostEndScale, useUnscaledTimeForGhost);
    }
}

/// <summary>
/// 잔상 페이드/축소를 담당하는 경량 컴포넌트(각 고스트 객체에 붙음)
/// </summary>
public class _GhostFader : MonoBehaviour
{
    private SpriteRenderer sr;
    private float life;
    private float a0, a1, s0, s1;
    private bool unscaled;

    private float t;
    private bool running;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public void Begin(float lifetime, float alphaStart, float alphaEnd, float scaleStart, float scaleEnd, bool useUnscaled)
    {
        life = Mathf.Max(0.01f, lifetime);
        a0 = Mathf.Clamp01(alphaStart);
        a1 = Mathf.Clamp01(alphaEnd);
        s0 = scaleStart;
        s1 = scaleEnd;
        unscaled = useUnscaled;

        t = 0f;
        running = true;
        enabled = true;
    }

    void OnEnable()
    {
        // 재활용 시 초기화
        t = 0f;
        running = true;
    }

    void Update()
    {
        if (!running) return;

        float dt = unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
        t += dt;

        float u = Mathf.Clamp01(t / life);

        // 알파/스케일 보간
        if (sr != null)
        {
            Color c = sr.color;
            c.a = Mathf.Lerp(a0, a1, u);
            sr.color = c;
        }

        float s = Mathf.Lerp(s0, s1, u);
        transform.localScale = Vector3.one * s * 1f; // 스케일은 스폰 시 worldScale 반영했으니 여기선 배율만

        if (u >= 1f)
        {
            running = false;
            gameObject.SetActive(false);
        }
    }
}
