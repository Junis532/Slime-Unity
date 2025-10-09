using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerAnimation : MonoBehaviour
{
    [System.Serializable]
    public enum State { Idle, Move, Stop } // Stop은 "원샷"

    [Header("애니메이션 스프라이트")]
    public List<Sprite> idleSprites;
    public List<Sprite> moveSprites;
    public List<Sprite> stopSprites; // 원샷

    [Header("속도 설정")]
    [Tooltip("Idle/Move 프레임 간격(초)")]
    public float frameRate = 0.1f;
    [Tooltip("Stop 원샷 기본 프레임 간격(초)")]
    public float stopFrameRate = 0.05f;

    [Header("Stop 오버드라이브(초반 프레임만 더 빠르게)")]
    [Tooltip("Stop 시작 시 초반 몇 프레임을 더 빠르게 넘길지")]
    public bool stopOverdrive = true;
    [Tooltip("오버드라이브 적용할 프레임 수(1프레임=다음 프레임으로 넘기기까지의 간격)")]
    public int stopOverdriveFrameCount = 2;
    [Tooltip("오버드라이브 배수(프레임 간격에 곱해짐). 1보다 작을수록 더 빠름. 예: 0.5 = 2배 빠름")]
    public float stopOverdriveScale = 0.5f;

    [Header("Stop 이후 Move/Idle 버스트(짧게 더 빠르게)")]
    public bool moveBurstAfterStop = true;
    [Tooltip("버스트 지속 시간(초)")]
    public float moveBurstDuration = 0.12f;
    [Tooltip("버스트 배수(Idle/Move 프레임 간격에 곱해짐). 1보다 작을수록 더 빠름")]
    public float moveBurstFrameRateScale = 0.66f;

    [Header("Stop 실행 옵션")]
    [Tooltip("Stop은 항상 언스케일드 델타로 진행(Time.timeScale=0이어도 재생)")]
    public bool stopUseUnscaledTimeAlways = true;
    [Tooltip("예상 지속시간 + 이 초를 넘기면 강제 복구")]
    public float stopHardTimeoutExtra = 0.5f;

    [Header("렌더러/애니메이터")]
    public SpriteRenderer targetRenderer;          // 비우면 자동탐색
    public bool disableAnimatorDuringStop = true;
    public Animator optionalAnimator;

    [Header("Stop 오버레이")]
    public bool useOverlayForStop = true;
    public int overlaySortingOffset = 10;
    [Tooltip("오버레이 전용 Sorting Layer 설정 (비우면 메인 렌더러와 동일)")]
    public string overlaySortingLayerName = "";
    [Tooltip("오버레이 전용 Order in Layer 설정 (overlaySortingOffset 대신 사용)")]
    public bool useCustomOverlayOrder = false;
    public int customOverlayOrder = 0;

    [Header("Move 상태 이펙트(선택)")]
    public GameObject effectPrefab;
    public float effectSpawnInterval = 1f;
    public float effectLifeTime = 0.3f;

    // 내부 상태
    private State currentState;
    private List<Sprite> currentSprites;
    private float timer;
    private int currentFrame;

    // Stop 제어
    private bool isPlayingStopOnce = false;
    private Coroutine stopCo;
    private SpriteRenderer overlayRenderer;
    private float stopHardTimeoutAt = 0f;

    // Stop 이후 버스트 타이밍
    private float moveBurstUntil = 0f;

    // 이펙트
    private float effectTimer;

    void Reset()
    {
        targetRenderer = GetComponent<SpriteRenderer>();
        if (optionalAnimator == null) optionalAnimator = GetComponent<Animator>();
    }

    void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>(true);
        if (optionalAnimator == null)
            optionalAnimator = GetComponent<Animator>();
    }

    void Start()
    {
        PlayAnimation(State.Idle, true);
    }

    void Update()
    {
        // Stop 원샷 중엔 코루틴이 담당
        if (isPlayingStopOnce)
        {
            // 하드 타임아웃: 예기치 못한 상황에서도 무조건 복구
            if (Time.unscaledTime >= stopHardTimeoutAt)
                AbortStopAndRecover("Stop hard-timeout");
            return;
        }

        if (currentSprites == null || currentSprites.Count == 0) return;

        // Idle/Move 루프(버스트가 켜져 있으면 프레임 간격을 더 짧게)
        float effectiveFrameRate = frameRate;
        if (moveBurstAfterStop && Time.unscaledTime < moveBurstUntil)
            effectiveFrameRate *= Mathf.Clamp(moveBurstFrameRateScale, 0.05f, 10f);

        timer += Time.deltaTime;
        if (timer >= effectiveFrameRate)
        {
            timer = 0f;
            currentFrame = (currentFrame + 1) % currentSprites.Count;
            if (targetRenderer != null)
                targetRenderer.sprite = currentSprites[currentFrame];
        }

        // Move 이펙트
        if (currentState == State.Move && effectPrefab != null)
        {
            effectTimer += Time.deltaTime;
            if (effectTimer >= effectSpawnInterval)
            {
                effectTimer = 0f;
                var fx = Instantiate(effectPrefab, transform.position, Quaternion.identity);
                Destroy(fx, effectLifeTime);
            }
        }
    }

    public void PlayAnimation(State newState, bool force = false)
    {
        // Stop 중에는 강제 전환(force)만 허용(Stop 자체는 예외)
        if (isPlayingStopOnce && !force && newState != State.Stop)
            return;

        if (!force && newState == currentState && newState != State.Stop)
            return;

        if (newState == State.Stop)
        {
            StartStopOnce();
            return;
        }

        currentState = newState;
        currentFrame = 0;
        timer = 0f;

        switch (newState)
        {
            case State.Idle: currentSprites = idleSprites; break;
            case State.Move: currentSprites = moveSprites; break;
        }

        if (targetRenderer != null && currentSprites != null && currentSprites.Count > 0)
            targetRenderer.sprite = currentSprites[0];
    }

    /// 외부: 이동→정지 순간 호출
    public void OnStopMoving()
    {
        StartStopOnce();
    }

    // ===== Stop 원샷 =====

    private void StartStopOnce()
    {
        if (stopSprites == null || stopSprites.Count == 0)
        {
            // 스프라이트 없으면 바로 Idle
            PlayAnimation(State.Idle, true);
            return;
        }

        if (stopCo != null) StopCoroutine(stopCo);

        isPlayingStopOnce = true;
        currentState = State.Stop;

        // 예상 지속시간(보수적으로 기본 프레임 간격 기준) + 여유
        float expected = Mathf.Max(0.0001f, stopFrameRate) * stopSprites.Count;
        stopHardTimeoutAt = Time.unscaledTime + expected + stopHardTimeoutExtra;

        if (disableAnimatorDuringStop && optionalAnimator != null)
            optionalAnimator.enabled = false;

        if (useOverlayForStop)
        {
            EnsureOverlayRenderer();          // 자식으로 생성/재사용
            SyncOverlayFromSource();          // flip/정렬/색/스케일 동기화
            if (targetRenderer != null) targetRenderer.enabled = false;
        }

        stopCo = StartCoroutine(StopOnceRoutine_Tick());
    }

    // 언스케일드/스케일드 모두 안전한 수동 틱 방식 + 초반 오버드라이브
    private IEnumerator StopOnceRoutine_Tick()
    {
        SpriteRenderer r = useOverlayForStop ? overlayRenderer : targetRenderer;

        // 첫 프레임
        if (r != null) r.sprite = stopSprites[0];

        // 다음 프레임을 언제 그릴지(가변 간격)
        int frameIndex = 1; // 0은 이미 그림
        float nextAt = (stopUseUnscaledTimeAlways ? Time.unscaledTime : Time.time) + GetStopIntervalForIndex(frameIndex);

        while (frameIndex < stopSprites.Count)
        {
            // 타임아웃 안전망
            if (Time.unscaledTime >= stopHardTimeoutAt)
                break;

            float now = stopUseUnscaledTimeAlways ? Time.unscaledTime : Time.time;

            if (now >= nextAt)
            {
                if (r != null) r.sprite = stopSprites[frameIndex++];
                // 다음 프레임 간격(오버드라이브 적용 여부 고려)
                nextAt += GetStopIntervalForIndex(frameIndex);
            }

            if (useOverlayForStop) SyncOverlayFromSource(); // 런타임 flip/정렬 변화 추적
            yield return null;
        }

        // 정리 & Idle 복귀 + (옵션) Move/Idle 버스트 시작
        RecoverAfterStop();

        if (moveBurstAfterStop)
            moveBurstUntil = Time.unscaledTime + Mathf.Max(0f, moveBurstDuration);

        PlayAnimation(State.Idle, true);
    }

    // index(=보여줄 프레임 인덱스)에 따라 간격을 동적으로 반환
    private float GetStopIntervalForIndex(int frameIndex)
    {
        // frameIndex: 1부터 시작(프레임0을 그린 이후 다음 프레임으로 넘어갈 시간)
        if (!stopOverdrive) return Mathf.Max(0.0001f, stopFrameRate);

        float step = stopFrameRate;
        if (frameIndex >= 1 && frameIndex <= stopOverdriveFrameCount)
            step *= Mathf.Clamp(stopOverdriveScale, 0.01f, 1f); // 1보다 작으면 더 빠름

        return Mathf.Max(0.0001f, step);
    }

    private void RecoverAfterStop()
    {
        if (useOverlayForStop)
        {
            if (overlayRenderer != null) overlayRenderer.enabled = false;
            if (targetRenderer != null) targetRenderer.enabled = true;
        }
        if (disableAnimatorDuringStop && optionalAnimator != null)
            optionalAnimator.enabled = true;

        isPlayingStopOnce = false;
        stopCo = null;
    }

    private void AbortStopAndRecover(string reason)
    {
        if (stopCo != null) StopCoroutine(stopCo);
        RecoverAfterStop();
        PlayAnimation(State.Idle, true);
    }

    /// 오버레이 생성(자식으로) + 로컬 트랜스폼 초기화
    private SpriteRenderer EnsureOverlayRenderer()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>(true);

        if (overlayRenderer == null)
        {
            Transform srcT = (targetRenderer != null ? targetRenderer.transform : transform);
            var go = new GameObject("StopOverlay");
            go.transform.SetParent(srcT, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            overlayRenderer = go.AddComponent<SpriteRenderer>();
        }

        overlayRenderer.enabled = true;
        SyncOverlayFromSource();
        return overlayRenderer;
    }

    /// 메인 렌더러의 현재 상태를 오버레이에 복사
    private void SyncOverlayFromSource()
    {
        if (targetRenderer == null || overlayRenderer == null) return;

        overlayRenderer.transform.localPosition = Vector3.zero;
        overlayRenderer.transform.localRotation = Quaternion.identity;
        overlayRenderer.transform.localScale = Vector3.one;

        // Sorting Layer 설정
        if (!string.IsNullOrEmpty(overlaySortingLayerName))
        {
            overlayRenderer.sortingLayerName = overlaySortingLayerName;
        }
        else
        {
            overlayRenderer.sortingLayerID = targetRenderer.sortingLayerID;
        }

        // Order in Layer 설정
        if (useCustomOverlayOrder)
        {
            overlayRenderer.sortingOrder = customOverlayOrder;
        }
        else
        {
            overlayRenderer.sortingOrder = targetRenderer.sortingOrder + overlaySortingOffset;
        }

        overlayRenderer.material = targetRenderer.sharedMaterial;
        overlayRenderer.color = targetRenderer.color;
        overlayRenderer.flipX = targetRenderer.flipX;
        overlayRenderer.flipY = targetRenderer.flipY;
        overlayRenderer.maskInteraction = targetRenderer.maskInteraction;
        overlayRenderer.spriteSortPoint = targetRenderer.spriteSortPoint;
        overlayRenderer.drawMode = targetRenderer.drawMode;
    }

    void OnDisable()
    {
        if (isPlayingStopOnce)
            AbortStopAndRecover("OnDisable");
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>(true);
        if (optionalAnimator == null)
            optionalAnimator = GetComponent<Animator>();

        // 파라미터 가드
        stopOverdriveFrameCount = Mathf.Max(0, stopOverdriveFrameCount);
        stopOverdriveScale = Mathf.Max(0.01f, stopOverdriveScale);
        moveBurstDuration = Mathf.Max(0f, moveBurstDuration);
        moveBurstFrameRateScale = Mathf.Max(0.01f, moveBurstFrameRateScale);
    }
#endif
}
