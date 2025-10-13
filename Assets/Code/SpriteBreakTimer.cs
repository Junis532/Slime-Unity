using UnityEngine;
using System.Collections;
using UnityEngine.Tilemaps;

/// 바닥/타일을 "부서지는" 연출로 처리하되,
/// - 파괴 대신 알파만 낮추고(선택)
/// - 콜라이더를 꺼서 실제로 '구멍'이 되게 하고(선택)
/// - 같은 영역에 Trigger를 생성하여 플레이어가 떨어지면 즉사(지금은 Debug.Log)
/// - 스프라이트와 타일맵을 모두 지원(자동 감지)
[RequireComponent(typeof(Transform))]
public class BreakableGround2D : MonoBehaviour
{
    public enum Outcome { FadeOnlyKeep, DisableObject, DestroyObject }

    [Header("지연(타이머) 설정")]
    [Tooltip("추가 대기시간(항상 먼저 기다림)")]
    public float extraWaitTime = 0f;
    [Tooltip("시작 후 추가로 기다릴 시간(유저가 조절)")]
    public float startDelay = 1.5f;
    public bool autoStartOnEnable = true;
    public bool useUnscaledTime = false;

    [Header("경고(깜빡임) 옵션")]
    public bool flashBeforeBreak = true;
    [Tooltip("끝나기 몇 초 전부터 깜빡임 시작")]
    public float flashStartAt = 0.5f;
    public float flashInterval = 0.1f;
    [Range(0f, 1f)] public float flashMinAlpha = 0.3f;

    [Header("결과/연출")]
    public Outcome outcome = Outcome.FadeOnlyKeep;     // 기본: 페이드만
    [Tooltip("페이드 목표 알파 (FadeOnlyKeep 모드에서 사용)")]
    [Range(0f, 1f)] public float targetAlphaAfter = 0f; // ✅ 완전 투명 기본값으로 변경
    [Tooltip("페이드/파괴 연출 시간")]
    public float effectDuration = 0.8f;

    [Header("구멍 옵션")]
    [Tooltip("구멍으로 만들기: 콜라이더 비활성 + Trigger 생성")]
    public bool makeHoleAfter = true;
    [Tooltip("구멍 시 모든 Collider2D 비활성화")]
    public bool disableSolidColliders = true;
    [Tooltip("구멍 트리거로 죽일 태그")]
    public string killTag = "Player";

    [Header("스프라이트 분리 연출(선택)")]
    public bool splitVisualForSprite = false;  // true면 스프라이트 반쪽 분리(가벼운 버전)
    public float splitForce = 1.5f;
    public float rotationSpeed = 120f;

    // 내부 상태
    private SpriteRenderer sr;             // 있을 수도
    private TilemapRenderer tmRenderer;    // 있을 수도
    private Tilemap tilemap;               // 있을 수도
    private Coroutine routine;
    private bool isRunning;

    void Awake()
    {
        // 컴포넌트 감지(둘 중 하나만 있어도 됨)
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
        // 깜빡임 복구
        SetAlpha(1f);
    }

    private IEnumerator BreakSequence()
    {
        isRunning = true;

        // 1) 항상 extraWaitTime 먼저
        if (extraWaitTime > 0f)
            yield return WaitForSecondsSmart(extraWaitTime);

        // 2) startDelay 카운트다운 + (선택) 깜빡임
        float t = 0f, lastFlash = 0f;
        float delay = Mathf.Max(0f, startDelay);
        float flashStartTime = Mathf.Max(0f, delay - Mathf.Max(0f, flashStartAt));
        while (t < delay)
        {
            float dt = DeltaTime();
            t += dt;

            if (flashBeforeBreak && t >= flashStartTime)
            {
                lastFlash += dt;
                if (lastFlash >= flashInterval)
                {
                    lastFlash = 0f;
                    // 토글식 깜빡임
                    float current = GetAlpha();
                    float next = (current < 1f) ? 1f : flashMinAlpha;
                    SetAlpha(next);
                }
            }
            yield return null;
        }
        // 깜빡임 원상복구
        SetAlpha(1f);

        // 3) 구멍 동작: 플레이어 즉사(로그), 콜라이더 끄기, 구멍 트리거 생성
        if (makeHoleAfter)
        {
            TryKillPlayerLog();                 // 지금은 로그만
            if (disableSolidColliders) ToggleAllColliders(false);
            CreateHoleTriggerFromBounds();      // 트리거 생성
        }

        // 4) 결과 연출
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
                    // 가벼운 반쪽 분리 연출(타일맵은 생략)
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

    // --- Utilities ---

    private IEnumerator WaitForSecondsSmart(float seconds)
    {
        if (useUnscaledTime)
        {
            float end = Time.unscaledTime + seconds;
            while (Time.unscaledTime < end) yield return null;
        }
        else
        {
            yield return new WaitForSeconds(seconds);
        }
    }

    private float DeltaTime() => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

    private float GetAlpha()
    {
        if (sr) return sr.color.a;
        if (tilemap) return tilemap.color.a;
        if (tmRenderer) return tmRenderer.material && tmRenderer.material.HasProperty("_Color")
            ? tmRenderer.material.color.a : 1f;
        return 1f;
    }

    private void SetAlpha(float a)
    {
        a = Mathf.Clamp01(a);
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

    // ✅ 완전 0까지 보장: 마지막에 SetAlpha(target)로 고정
    private IEnumerator FadeToAlpha(float target, float duration)
    {
        float start = GetAlpha();
        if (Mathf.Approximately(duration, 0f))
        {
            SetAlpha(target);
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += DeltaTime();
            float k = Mathf.Clamp01(t / duration);
            float a = Mathf.Lerp(start, target, k);
            SetAlpha(a);
            yield return null;
        }

        // 💯 완전 0까지 보장
        SetAlpha(target);
    }

    private void ToggleAllColliders(bool enable)
    {
        var c2d = GetComponentsInChildren<Collider2D>(true);
        foreach (var c in c2d) c.enabled = enable;
        // 3D는 거의 없겠지만 방어적으로
        var c3d = GetComponentsInChildren<Collider>(true);
        foreach (var c in c3d) c.enabled = enable;
    }

    private Bounds GetWorldBounds()
    {
        if (sr) return sr.bounds;
        if (tmRenderer) return tmRenderer.bounds;
        return new Bounds(transform.position, Vector3.zero);
    }

    private void CreateHoleTriggerFromBounds()
    {
        Bounds b = GetWorldBounds();
        var hole = new GameObject($"{name}_VoidZone");
        hole.layer = gameObject.layer; // 동일 레이어로
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

    // 스프라이트 간단 분리(가벼운 버전: 절반 생성 없이 연출만)
    private IEnumerator SpriteQuickSplitAndFade(float duration)
    {
        if (!sr) yield break;

        // 시각적 복제 2개 (왼쪽/오른쪽)
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

        // 원본 숨김
        var c = sr.color; c.a = 0f; sr.color = c;

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

[RequireComponent(typeof(BoxCollider2D))]
public class VoidKillZone2D : MonoBehaviour
{
    [Tooltip("플레이어 태그")]
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
