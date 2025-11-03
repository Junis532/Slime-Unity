using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerAnimation : MonoBehaviour
{
    [System.Serializable]
    public enum State { Idle, Move, Stop }

    [Header("애니메이션 스프라이트")]
    public List<Sprite> idleSprites;
    public List<Sprite> moveSprites;
    public List<Sprite> stopSprites;

    [Header("속도 설정")]
    public float frameRate = 0.1f;
    public float stopFrameRate = 0.05f;

    [Header("Stop 오버드라이브")]
    public bool stopOverdrive = true;
    public int stopOverdriveFrameCount = 2;
    public float stopOverdriveScale = 0.5f;

    [Header("Stop 이후 Move/Idle 버스트")]
    public bool moveBurstAfterStop = true;
    public float moveBurstDuration = 0.12f;
    public float moveBurstFrameRateScale = 0.66f;

    [Header("Stop 실행 옵션")]
    public bool stopUseUnscaledTimeAlways = true;
    public float stopHardTimeoutExtra = 0.5f;

    [Header("렌더러/애니메이터")]
    public SpriteRenderer targetRenderer;
    public List<SpriteRenderer> extraRenderers = new List<SpriteRenderer>();
    public bool disableAnimatorDuringStop = true;
    public Animator optionalAnimator;

    [Header("Stop 오버레이")]
    public bool useOverlayForStop = true;
    public int overlaySortingOffset = 10;
    public string overlaySortingLayerName = "";
    public bool useCustomOverlayOrder = false;
    public int customOverlayOrder = 0;

    [Header("Move 상태 이펙트")]
    public GameObject effectPrefab;
    public float effectSpawnInterval = 1f;
    public float effectLifeTime = 0.3f;

    private State currentState;
    private List<Sprite> currentSprites;
    private float timer;
    private int currentFrame;

    private bool isPlayingStopOnce = false;
    private Coroutine stopCo;
    private List<SpriteRenderer> overlayRenderers = new List<SpriteRenderer>();
    private float stopHardTimeoutAt = 0f;
    private float moveBurstUntil = 0f;
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

        if (!extraRenderers.Contains(targetRenderer))
            extraRenderers.Insert(0, targetRenderer);
    }

    void Start()
    {
        PlayAnimation(State.Idle, true);
    }

    void Update()
    {
        if (isPlayingStopOnce)
        {
            if (Time.unscaledTime >= stopHardTimeoutAt)
                AbortStopAndRecover("Stop hard-timeout");
            return;
        }

        if (currentSprites == null || currentSprites.Count == 0) return;

        float effectiveFrameRate = frameRate;
        if (moveBurstAfterStop && Time.unscaledTime < moveBurstUntil)
            effectiveFrameRate *= Mathf.Clamp(moveBurstFrameRateScale, 0.05f, 10f);

        timer += Time.deltaTime;
        if (timer >= effectiveFrameRate)
        {
            timer = 0f;
            currentFrame = (currentFrame + 1) % currentSprites.Count;
            SetSprite(currentSprites[currentFrame]);
        }

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

        if (currentSprites != null && currentSprites.Count > 0)
            SetSprite(currentSprites[0]);
    }

    public void OnStopMoving()
    {
        StartStopOnce();
    }

    private void StartStopOnce()
    {
        if (stopSprites == null || stopSprites.Count == 0)
        {
            PlayAnimation(State.Idle, true);
            return;
        }

        if (stopCo != null) StopCoroutine(stopCo);

        isPlayingStopOnce = true;
        currentState = State.Stop;
        float expected = Mathf.Max(0.0001f, stopFrameRate) * stopSprites.Count;
        stopHardTimeoutAt = Time.unscaledTime + expected + stopHardTimeoutExtra;

        if (disableAnimatorDuringStop && optionalAnimator != null)
            optionalAnimator.enabled = false;

        // Stop 첫 프레임 바로 세팅
        if (targetRenderer != null)
            targetRenderer.sprite = stopSprites[0];

        stopCo = StartCoroutine(StopOnceRoutine_Tick());
    }


    private IEnumerator StopOnceRoutine_Tick()
    {
        int frameIndex = 1; // 첫 프레임은 이미 세팅됨
        float nextAt = (stopUseUnscaledTimeAlways ? Time.unscaledTime : Time.time) + GetStopIntervalForIndex(frameIndex);

        while (frameIndex < stopSprites.Count)
        {
            if (Time.unscaledTime >= stopHardTimeoutAt) break;

            float now = stopUseUnscaledTimeAlways ? Time.unscaledTime : Time.time;
            if (now >= nextAt)
            {
                SetSprite(stopSprites[frameIndex++]);
                nextAt += GetStopIntervalForIndex(frameIndex);
            }

            if (useOverlayForStop)
                for (int i = 0; i < overlayRenderers.Count; i++)
                    SyncOverlayFromSource(overlayRenderers[i], extraRenderers[i]);

            yield return null;
        }

        RecoverAfterStop();
        if (moveBurstAfterStop)
            moveBurstUntil = Time.unscaledTime + Mathf.Max(0f, moveBurstDuration);

        PlayAnimation(State.Idle, true);
    }

    private float GetStopIntervalForIndex(int frameIndex)
    {
        if (!stopOverdrive) return Mathf.Max(0.0001f, stopFrameRate);

        float step = stopFrameRate;
        if (frameIndex >= 1 && frameIndex <= stopOverdriveFrameCount)
            step *= Mathf.Clamp(stopOverdriveScale, 0.01f, 1f);

        return Mathf.Max(0.0001f, step);
    }

    private void RecoverAfterStop()
    {
        if (useOverlayForStop)
        {
            foreach (var r in overlayRenderers)
                r.enabled = false;
            foreach (var r in extraRenderers)
                r.enabled = true;
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

    private void SetSprite(Sprite sprite)
    {
        foreach (var r in extraRenderers)
        {
            if (r != null)
                r.sprite = sprite;
        }
    }

    private void EnsureOverlayRenderers()
    {
        overlayRenderers.Clear();
        for (int i = 0; i < extraRenderers.Count; i++)
        {
            var r = extraRenderers[i];
            if (r == null) continue;

            GameObject go = new GameObject(r.name + "_StopOverlay");
            go.transform.SetParent(r.transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            var overlay = go.AddComponent<SpriteRenderer>();
            overlay.enabled = true;
            overlayRenderers.Add(overlay);
        }
    }

    private void SyncOverlayFromSource(SpriteRenderer overlay, SpriteRenderer source)
    {
        if (overlay == null || source == null) return;

        overlay.transform.localPosition = Vector3.zero;
        overlay.transform.localRotation = Quaternion.identity;
        overlay.transform.localScale = Vector3.one;

        overlay.sortingLayerName = string.IsNullOrEmpty(overlaySortingLayerName) ? source.sortingLayerName : overlaySortingLayerName;
        overlay.sortingOrder = useCustomOverlayOrder ? customOverlayOrder : source.sortingOrder + overlaySortingOffset;

        overlay.material = source.sharedMaterial;
        overlay.color = source.color;
        overlay.flipX = source.flipX;
        overlay.flipY = source.flipY;
        overlay.maskInteraction = source.maskInteraction;
        overlay.spriteSortPoint = source.spriteSortPoint;
        overlay.drawMode = source.drawMode;
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

        stopOverdriveFrameCount = Mathf.Max(0, stopOverdriveFrameCount);
        stopOverdriveScale = Mathf.Max(0.01f, stopOverdriveScale);
        moveBurstDuration = Mathf.Max(0f, moveBurstDuration);
        moveBurstFrameRateScale = Mathf.Max(0.01f, moveBurstFrameRateScale);
    }
#endif
}
