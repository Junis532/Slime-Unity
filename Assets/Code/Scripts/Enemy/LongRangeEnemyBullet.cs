using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LongRangeEnemyBullet : MonoBehaviour
{
    [Header("Collision")]
    public bool destroyOnObstacle = false;
    public bool ignorePlayerWhenUsingSkill = true;

    [Header("이펙트 프리팹")]
    [Tooltip("장애물 충돌 시 생성될 이펙트")]
    public GameObject hitEffectPrefab; // ✅ 추가

    private Collider2D myCollider;
    private Collider2D playerCollider;
    private Rigidbody2D rb;
    private SpriteRenderer bulletSR;

    // ─────────────────────────────────────────────
    // Ghost Trail(잔상) – TrailRenderer 없이 구현
    // ─────────────────────────────────────────────
    [Header("Ghost Trail (No TrailRenderer)")]
    public bool enableGhostTrail = true;
    public float ghostSpawnInterval = 0.045f;
    public float ghostLifetime = 0.25f;
    [Range(0f, 1f)] public float ghostStartAlpha = 0.6f;
    [Range(0f, 1f)] public float ghostEndAlpha = 0.0f;
    public float ghostStartScale = 1.0f;
    public float ghostEndScale = 0.75f;
    public float minVelocitySqrForGhost = 0.01f;
    public int maxGhostPool = 24;
    public Material ghostMaterialOverride;
    public bool useUnscaledTimeForGhost = false;
    public bool burstOnFire = true;

    private float ghostTimer = 0f;
    private List<GameObject> ghostPool;
    private int ghostPoolIndex = 0;
    private bool ghostActive = false;

    void Awake()
    {
        myCollider = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
        bulletSR = GetComponentInChildren<SpriteRenderer>();

        // Player Collider 찾기
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            playerCollider = playerObj.GetComponent<Collider2D>();

        if (enableGhostTrail)
            InitGhostPool();
    }

    void OnEnable()
    {
        ghostTimer = 0f;
        ghostActive = enableGhostTrail;

        if (enableGhostTrail && ghostPool != null)
        {
            for (int i = 0; i < ghostPool.Count; i++)
                ghostPool[i].SetActive(false);
            ghostPoolIndex = 0;
        }
    }

    void OnDisable()
    {
        ghostActive = false;
        if (ghostPool != null)
        {
            for (int i = 0; i < ghostPool.Count; i++)
                ghostPool[i].SetActive(false);
        }
    }

    void Update()
    {
        // 스킬 사용 중엔 플레이어 충돌 무시
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
        // ─────────────────────────────────────────────
        // ▣ 플레이어 충돌
        // ─────────────────────────────────────────────
        if (collision.CompareTag("Player"))
        {
            if (ignorePlayerWhenUsingSkill && GameManager.Instance.joystickDirectionIndicator.IsUsingSkill)
                return;

            int damage = GameManager.Instance.longRangeEnemyStats.attack;
            Vector3 enemyPosition = transform.position;

            GameManager.Instance.playerDamaged.TakeDamage(damage, enemyPosition);
            Destroy(gameObject);
            return;
        }

        // ─────────────────────────────────────────────
        // ▣ 장애물 충돌 시 이펙트 생성 후 제거
        // ─────────────────────────────────────────────
        if (destroyOnObstacle && (collision.CompareTag("Obstacle") || collision.CompareTag("LaserNot")))
        {
            // ✅ 이펙트 생성
            if (hitEffectPrefab != null)
            {
                GameObject effect = Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
                Destroy(effect, 0.3f); // 이펙트 0.3초 후 자동 삭제
            }

            Destroy(gameObject); // 총알 제거
            return;
        }
    }

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
            g.transform.SetParent(null);
            var sr = g.AddComponent<SpriteRenderer>();

            sr.sprite = bulletSR != null ? bulletSR.sprite : null;
            sr.sortingLayerID = bulletSR != null ? bulletSR.sortingLayerID : 0;
            sr.sortingOrder = (bulletSR != null ? bulletSR.sortingOrder : 0) - 1;

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

        ghost.transform.position = transform.position;
        ghost.transform.rotation = transform.rotation;
        ghost.transform.localScale = transform.lossyScale * ghostStartScale;

        var gsr = ghost.GetComponent<SpriteRenderer>();
        if (gsr != null)
        {
            if (bulletSR != null)
            {
                gsr.sprite = bulletSR.sprite;
                Color c = bulletSR.color;
                c.a = ghostStartAlpha;
                gsr.color = c;

                gsr.sortingLayerID = bulletSR.sortingLayerID;
                gsr.sortingOrder = bulletSR.sortingOrder - 1;

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

        var fader = ghost.GetComponent<_GhostFader>();
        if (fader == null) fader = ghost.AddComponent<_GhostFader>();
        fader.Begin(ghostLifetime, ghostStartAlpha, ghostEndAlpha, ghostStartScale, ghostEndScale, useUnscaledTimeForGhost);
    }
}

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
        t = 0f;
        running = true;
    }

    void Update()
    {
        if (!running) return;

        float dt = unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
        t += dt;

        float u = Mathf.Clamp01(t / life);

        if (sr != null)
        {
            Color c = sr.color;
            c.a = Mathf.Lerp(a0, a1, u);
            sr.color = c;
        }

        float s = Mathf.Lerp(s0, s1, u);
        transform.localScale = Vector3.one * s;

        if (u >= 1f)
        {
            running = false;
            gameObject.SetActive(false);
        }
    }
}
