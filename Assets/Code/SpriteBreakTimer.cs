using UnityEngine;
using System.Collections;
using UnityEngine.Tilemaps;

// Debug 충돌 방지
using Debug = UnityEngine.Debug;

/// 바닥/타일을 "붉게 변하며 꺼지는" 버전으로 처리
[RequireComponent(typeof(Transform))]
public class BreakableGround2D : MonoBehaviour
{
    public enum Outcome { FadeOnlyKeep, DisableObject, DestroyObject }

    [Header("지연(타이머) 설정")]
    public float extraWaitTime = 0f;
    public float startDelay = 1.5f;
    public bool autoStartOnEnable = true;
    public bool useUnscaledTime = false;

    [Header("붉게 변하는 경고 연출")]
    [Tooltip("빨간색으로 변하는 연출을 사용할지")]
    public bool useRedWarning = true;
    [Tooltip("빨간색 전환 시작 시점 (남은 시간 비율 0~1)")]
    [Range(0f, 1f)] public float redStartRatio = 0.5f;
    [Tooltip("빨간색 전환 속도 배율")]
    public float redTransitionSpeed = 2f;
    [Tooltip("최대 붉은 정도 (1=완전빨강)")]
    [Range(0f, 1f)] public float redIntensity = 0.8f;

    [Header("결과/연출")]
    public Outcome outcome = Outcome.FadeOnlyKeep;
    [Range(0f, 1f)] public float targetAlphaAfter = 0f;
    public float effectDuration = 0.8f;

    [Header("구멍 옵션")]
    public bool makeHoleAfter = true;
    public bool disableSolidColliders = true;
    public string killTag = "Player";

    [Header("스프라이트 분리 연출(선택)")]
    public bool splitVisualForSprite = false;
    public float splitForce = 1.5f;
    public float rotationSpeed = 120f;

    // 내부 컴포넌트
    private SpriteRenderer sr;
    private TilemapRenderer tmRenderer;
    private Tilemap tilemap;
    private Coroutine routine;
    private bool isRunning;

    private Color baseColor;
    private bool hasColorCache;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        tmRenderer = GetComponent<TilemapRenderer>();
        tilemap = GetComponent<Tilemap>();
    }

    void OnEnable()
    {
        if (autoStartOnEnable)
            StartBreak();
    }

    [ContextMenu("Start Break")]
    public void StartBreak()
    {
        if (isRunning) return;
        routine = StartCoroutine(BreakSequence());
    }

    [ContextMenu("Cancel Break")]
    public void CancelBreak()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
        isRunning = false;
        SetColorAlpha(1f);
        if (hasColorCache) SetColor(baseColor);
    }

    private IEnumerator BreakSequence()
    {
        isRunning = true;
        CacheColor();

        if (extraWaitTime > 0f)
            yield return WaitForSecondsSmart(extraWaitTime);

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, startDelay);
        float redStartTime = duration * (1f - redStartRatio);

        while (elapsed < duration)
        {
            float dt = DeltaTime();
            elapsed += dt;

            float remain = Mathf.Max(0f, duration - elapsed);

            // 서서히 붉어지는 구간
            if (useRedWarning && elapsed >= redStartTime)
            {
                float t = Mathf.InverseLerp(redStartTime, duration, elapsed);
                t = Mathf.Pow(t, redTransitionSpeed);
                ApplyRedOverlay(t);
            }

            yield return null;
        }

        // 완전 빨갛게
        if (useRedWarning) ApplyRedOverlay(1f);

        // 1) 구멍 만들기
        if (makeHoleAfter)
        {
            TryKillPlayerLog();
            if (disableSolidColliders) ToggleAllColliders(false);
            CreateHoleTriggerFromBounds();
        }

        // 2) 결과 연출
        switch (outcome)
        {
            case Outcome.FadeOnlyKeep:
                yield return FadeToAlpha(targetAlphaAfter, effectDuration);
                break;
            case Outcome.DisableObject:
                yield return FadeToAlpha(0f, effectDuration);
                gameObject.SetActive(false);
                break;
            case Outcome.DestroyObject:
                if (sr && splitVisualForSprite)
                {
                    yield return SpriteQuickSplitAndFade(effectDuration);
                    Destroy(gameObject);
                }
                else
                {
                    yield return FadeToAlpha(0f, effectDuration);
                    Destroy(gameObject);
                }
                break;
        }

        isRunning = false;
    }

    //───────────────────────────────
    // 유틸
    //───────────────────────────────
    private IEnumerator WaitForSecondsSmart(float seconds)
    {
        if (useUnscaledTime)
        {
            float end = Time.unscaledTime + seconds;
            while (Time.unscaledTime < end) yield return null;
        }
        else yield return new WaitForSeconds(seconds);
    }

    private float DeltaTime() => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

    private void CacheColor()
    {
        if (sr) baseColor = sr.color;
        else if (tilemap) baseColor = tilemap.color;
        else if (tmRenderer && tmRenderer.material && tmRenderer.material.HasProperty("_Color"))
            baseColor = tmRenderer.material.color;
        else baseColor = Color.white;
        hasColorCache = true;
    }

    private void SetColor(Color c)
    {
        if (sr) sr.color = c;
        else if (tilemap) tilemap.color = c;
        else if (tmRenderer && tmRenderer.material && tmRenderer.material.HasProperty("_Color"))
            tmRenderer.material.color = c;
    }

    private void SetColorAlpha(float a)
    {
        if (sr)
        {
            var c = sr.color; c.a = a; sr.color = c;
        }
        if (tilemap)
        {
            var c = tilemap.color; c.a = a; tilemap.color = c;
        }
        else if (tmRenderer && tmRenderer.material && tmRenderer.material.HasProperty("_Color"))
        {
            var c = tmRenderer.material.color; c.a = a; tmRenderer.material.color = c;
        }
    }

    private void ApplyRedOverlay(float t)
    {
        if (!hasColorCache) CacheColor();
        Color target = Color.Lerp(baseColor, new Color(1f, 0f, 0f, baseColor.a), redIntensity * t);
        SetColor(target);
    }

    private IEnumerator FadeToAlpha(float target, float duration)
    {
        float start = GetAlpha();
        float t = 0f;
        while (t < duration)
        {
            t += DeltaTime();
            float k = Mathf.Clamp01(t / duration);
            SetColorAlpha(Mathf.Lerp(start, target, k));
            yield return null;
        }
        SetColorAlpha(target);
    }

    private float GetAlpha()
    {
        if (sr) return sr.color.a;
        if (tilemap) return tilemap.color.a;
        if (tmRenderer) return tmRenderer.material && tmRenderer.material.HasProperty("_Color")
            ? tmRenderer.material.color.a : 1f;
        return 1f;
    }

    private void ToggleAllColliders(bool enable)
    {
        foreach (var c in GetComponentsInChildren<Collider2D>(true)) c.enabled = enable;
        foreach (var c in GetComponentsInChildren<Collider>(true)) c.enabled = enable;
    }

    private Bounds GetWorldBounds()
    {
        if (sr) return sr.bounds;
        if (tmRenderer) return tmRenderer.bounds;
        return new Bounds(transform.position, Vector3.one * 1f);
    }

    private void CreateHoleTriggerFromBounds()
    {
        Bounds b = GetWorldBounds();
        var hole = new GameObject($"{name}_VoidZone");
        hole.layer = gameObject.layer;
        hole.transform.SetParent(transform, worldPositionStays: true);
        hole.transform.position = b.center;

        var box = hole.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(b.size.x, b.size.y);

        var zone = hole.AddComponent<VoidKillZone2D>();
        zone.playerTag = killTag;

        Debug.Log($"[BreakableGround2D] 구멍 트리거 생성: {hole.name}, size={box.size}");
    }

    private void TryKillPlayerLog()
    {
        GameObject player = null;
        try { player = GameObject.FindGameObjectWithTag(killTag); } catch { }
        if (player != null)
            Debug.Log($"[BreakableGround2D] (로그) 플레이어 즉사 처리: {player.name}");
        else
            Debug.Log("[BreakableGround2D] (로그) 플레이어를 찾지 못함. killTag 확인.");
    }

    private IEnumerator SpriteQuickSplitAndFade(float duration)
    {
        if (!sr) yield break;
        var leftObj = new GameObject(name + "_L");
        var rightObj = new GameObject(name + "_R");
        leftObj.transform.SetPositionAndRotation(transform.position, transform.rotation);
        rightObj.transform.SetPositionAndRotation(transform.position, transform.rotation);
        leftObj.transform.localScale = rightObj.transform.localScale = transform.localScale;

        var lsr = leftObj.AddComponent<SpriteRenderer>();
        var rsr = rightObj.AddComponent<SpriteRenderer>();
        lsr.sprite = rsr.sprite = sr.sprite;
        lsr.color = rsr.color = sr.color;
        lsr.sortingLayerID = rsr.sortingLayerID = sr.sortingLayerID;
        lsr.sortingOrder = rsr.sortingOrder = sr.sortingOrder;

        var c0 = sr.color; c0.a = 0f; sr.color = c0;

        Vector3 leftDir = (Vector3.left + Vector3.up * 0.15f).normalized;
        Vector3 rightDir = (Vector3.right + Vector3.up * 0.15f).normalized;

        float t = 0f;
        while (t < duration)
        {
            float dt = DeltaTime();
            t += dt;
            float a = Mathf.Lerp(1f, 0f, Mathf.Clamp01(t / duration));

            leftObj.transform.position += leftDir * splitForce * dt;
            rightObj.transform.position += rightDir * splitForce * dt;
            leftObj.transform.Rotate(Vector3.forward, rotationSpeed * dt);
            rightObj.transform.Rotate(Vector3.forward, -rotationSpeed * dt);

            var lc = lsr.color; lc.a = a; lsr.color = lc;
            var rc = rsr.color; rc.a = a; rsr.color = rc;

            yield return null;
        }

        Destroy(leftObj);
        Destroy(rightObj);
    }
}

/// 플레이어 즉사 트리거
[RequireComponent(typeof(BoxCollider2D))]
public class VoidKillZone2D : MonoBehaviour
{
    public string playerTag = "Player";

    void Reset()
    {
        var box = GetComponent<BoxCollider2D>();
        box.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        Debug.Log($"[VoidKillZone2D] 플레이어 즉사 트리거: {other.name}");
        GameManager.Instance.playerStats.currentHP = 0;
    }
}
