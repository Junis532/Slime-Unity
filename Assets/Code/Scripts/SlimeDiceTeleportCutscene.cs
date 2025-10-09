using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

[DisallowMultipleComponent]
public class SlimeDiceTeleportCutscene : MonoBehaviour
{
    public enum CameraSnapMode { None, SlimeBeforeTeleport, TargetAfterTeleport }
    public enum CameraZMode { Preserve, Override }
    public enum AnchorMode { RendererBottomCenter, ColliderBottomCenter, FeetTransform }
    public enum DiceVanishAction { None, Disable, Destroy }

    // 말풍선 배치용
    public enum BubbleAnchor { Above, Right, Left, AutoSide }

    [Header("Trigger")]
    public string triggerTag = "Dice";
    public bool oneShot = true;

    [Header("필수 참조")]
    public SpriteRenderer slimeRenderer;
    public Transform targetPoint;      // 도착 마커(바닥 기준)
    public Transform cameraTransform;  // 메인 카메라
    public Image whiteOverlay;         // 풀스크린 흰색 이미지(a=0)

    [Header("타이밍")]
    public float whitenDuration = 0.20f;
    public float flashInDuration = 0.12f;      // 0→0.8
    public float fillToWhiteDuration = 0.18f;  // 0.8→1.0
    public float holdWhiteDuration = 0.10f;    // 텔레포트 직전 유지

    [Header("텔레포트/낙하")]
    public float airHeight = 1.2f;             // 도착점 위 공중 시작 높이
    public float fallEasePower = 2.2f;         // 낙하 가속감(>=1)
    public float fallMinTime = 0.18f;          // 최소 낙하시간
    public bool hardSnapAfterLand = true;     // 착지 직후 오차 제거

    [Header("착지 & 통통")]
    public float landingSquash = 0.15f;
    public float squashDuration = 0.08f;
    public int hopCount = 2;
    public Vector2 hopDirection = Vector2.right;
    public float hopDistance = 1.2f;
    public float firstHopHeight = 1.0f;
    public float hopDuration = 0.32f;
    public float hopDamping = 0.6f;

    [Header("카메라 스냅")]
    public CameraSnapMode cameraSnap = CameraSnapMode.TargetAfterTeleport;
    public Vector2 cameraXYOffset = Vector2.zero;
    public CameraZMode cameraZMode = CameraZMode.Preserve;
    public float cameraZOverride = -10f;
    public bool cameraUseLocal = false;

    [Header("정밀 정렬(핵심)")]
    public AnchorMode anchorMode = AnchorMode.FeetTransform;
    public Transform feetAnchor;               // 발 기준점(권장)
    public Vector2 teleportOffset = Vector2.zero;

    [Header("픽셀 퍼펙트")]
    public bool pixelSnap = true;

    // ===== 떨어지는 모션 시각효과(선택) =====
    [Header("Fall Motion FX")]
    public bool enableFallStretch = true;
    public float fallStretchAmount = 0.18f;
    public float fallStretchSharpness = 10f;

    [Tooltip("그림자 스프라이트(없으면 비워도 안전)")]
    public SpriteRenderer shadowRenderer;
    public bool enableShadowFX = true;
    public Vector2 shadowBaseScale = new Vector2(0.9f, 0.35f);
    public float shadowScaleByHeight = 0.6f;
    public Vector2 shadowAlphaMinMax = new Vector2(0.15f, 0.6f);
    public float shadowYOffset = 0.02f;

    [Header("착지 흔들림(선택)")]
    public bool enableLandShake = true;
    public float shakeAmplitude = 0.05f;
    public float shakeDuration = 0.10f;

    // ===== 트리거 시 주사위 제거 =====
    [Header("On Trigger: Dice vanish")]
    public DiceVanishAction diceVanish = DiceVanishAction.Destroy;
    [Tooltip("주사위 사라지는 시간 지연(초)")]
    public float diceVanishDelay = 0.30f;

    // ===== 페이드인 직후 스프라이트 교체 =====
    [Header("Sprite Swap (페이드인 직후)")]
    public Sprite spriteAfterFade;         // α=1 직후로 교체할 스프라이트
    public bool keepChangedSprite = true;// false면 컷씬 끝에 원래 스프라이트로 복귀

    // ===== Animator 대신: frames[0] 교체 옵션 =====
    [Header("Patch Frame Arrays")]
    public bool replaceFirstFrameInArrays = true;
    public MonoBehaviour[] frameArrayOwners;
    public bool revertFramesAfter = false;

    // ===== 페이드/교체 시, 스프라이트 덮어쓰는 컨트롤러 잠깐 OFF (선택) =====
    [Header("Animation Control (선택)")]
    public Behaviour[] animationControllers; // Animator, SlimeWalk 등
    public bool autoDisableAnimator = true;  // Animator 자동 감지
    public bool reactivateControllersAfter = true;

    // ===== 말풍선 “…“: 프리팹 생성 방식 =====
    [Header("Speech Bubble (Prefab)")]
    [Tooltip("‘…’ 말풍선 프리팹(내부에 SpriteRenderer 하나 포함 권장)")]
    public GameObject bubblePrefab;
    public Sprite[] bubbleFrames;          // ‘…’ 순서대로 프레임들
    public float bubbleFPS = 5f;           // 천천히 1사이클
    public int bubbleLoops = 1;            // 요청: 1사이클
    public float bubbleHoldLast = 0.4f;    // 마지막 프레임 유지
    public bool bubbleSpawnAsChild = true; // 보통 슬라임 자식으로
    public bool bubbleCopySortingFromSlime = true;
    public int bubbleSortingOrderOffset = 1;
    public bool bubbleFlipWithSlime = true; // Above 모드에서만 좌우 반전 적용
    public bool bubbleDestroyAfter = true;

    [Header("Bubble Placement")]
    [Tooltip("Above=머리 위, Right/Left=옆, AutoSide=flipX에 따라 자동")]
    public BubbleAnchor bubbleAnchor = BubbleAnchor.AutoSide;
    [Tooltip("옆에 붙일 때 X 패딩(월드 단위)")]
    public float bubbleSidePad = 0.35f;
    [Tooltip("옆 배치 시 수직 바이어스(0=중앙, 0.2=위로 살짝)")]
    [Range(-0.5f, 0.5f)] public float bubbleHeightBias = 0.15f;
    [Tooltip("추가 Y 보정(Above에서만 주로 사용)")]
    public Vector2 bubbleOffset = new Vector2(0f, 0.9f);
    [Tooltip("카메라 앞 z, 2D는 보통 0")]
    public float bubbleZ = 0f;
    public bool bubbleForceLayerFromSlime = true;

    [Header("Start Game (같은 씬에서만)")]
    public bool startGameAfterBubble = false; // 씬 점프 안 할 때만 사용
    public UnityEvent onStartGame;

    // ===== 인게임 씬 전환 =====
    [Header("Scene Transition")]
    [Tooltip("말풍선 1사이클이 끝난 직후 자동 전환")]
    public bool enableSceneJump = true;
    public string nextSceneName = "InGame";
    public bool preloadDuringCutscene = true;
    public float sceneFadeToWhite = 0.12f;
    public float sceneWhiteHold = 0.03f;

    [Header("기타")]
    public bool keepWhiteAfter = false;
    public bool useUnscaledTime = false;
    public bool debugLogs = false;

    // 내부
    bool _running;
    Color _originColor;
    Vector3 _originScale;
    Sprite _originSprite;
    Collider2D _col;

    readonly Dictionary<Behaviour, bool> _prevEnabled = new Dictionary<Behaviour, bool>();
    readonly List<Behaviour> _controllersRuntime = new List<Behaviour>();
    struct FramePatch { public object owner; public FieldInfo field; public Sprite[] backup; }
    readonly List<FramePatch> _patched = new List<FramePatch>();

    AsyncOperation _preloadOp; // 씬 프리로드 핸들

    void Reset() => slimeRenderer = GetComponent<SpriteRenderer>();

    void Awake()
    {
        if (!slimeRenderer) slimeRenderer = GetComponent<SpriteRenderer>();
        _originScale = transform.localScale;
        _originSprite = slimeRenderer ? slimeRenderer.sprite : null;
        _col = GetComponent<Collider2D>();
        if (shadowRenderer) shadowRenderer.gameObject.SetActive(false);

        // 컨트롤러 목록 구성
        _controllersRuntime.Clear();
        if (animationControllers != null)
            foreach (var b in animationControllers)
                if (b && !_controllersRuntime.Contains(b)) _controllersRuntime.Add(b);
        if (autoDisableAnimator)
        {
            var anim = GetComponent<Animator>();
            if (anim && !_controllersRuntime.Contains(anim)) _controllersRuntime.Add(anim);
        }

        // 프리로드는 컷씬 시작 후에 진행 (Awake에서 하지 않음)
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!string.IsNullOrEmpty(triggerTag) && other.CompareTag(triggerTag))
        {
            if (debugLogs) Debug.Log($"[Cutscene] 트리거 발동: {other.name}");
            StartCoroutine(VanishDiceAfterDelay(other.gameObject));
            if (_running) 
            {
                if (debugLogs) Debug.Log("[Cutscene] 이미 실행 중 - 스킵");
                return;
            }
            if (debugLogs) Debug.Log("[Cutscene] 컷씬 시작");
            StartCoroutine(RunCutscene());
            if (oneShot) _running = true;
        }
    }

    IEnumerator VanishDiceAfterDelay(GameObject dice)
    {
        if (diceVanish == DiceVanishAction.None) yield break;
        if (diceVanishDelay > 0f) yield return Wait(diceVanishDelay);
        if (dice) HandleDiceVanish(dice);
    }
    void HandleDiceVanish(GameObject dice)
    {
        if (diceVanish == DiceVanishAction.Disable) dice.SetActive(false);
        else if (diceVanish == DiceVanishAction.Destroy) Destroy(dice);
    }

    IEnumerator RunCutscene()
    {
        if (!slimeRenderer || !cameraTransform || !targetPoint || !whiteOverlay)
        { Debug.LogError("[Cutscene] 필수 참조가 비었습니다."); yield break; }

        // 컷씬 시작 후 프리로드 (안전한 타이밍)
        if (enableSceneJump && preloadDuringCutscene && !string.IsNullOrEmpty(nextSceneName) && _preloadOp == null)
        {
            yield return new WaitForSeconds(0.1f); // 약간의 지연
            try
            {
                _preloadOp = SceneManager.LoadSceneAsync(nextSceneName);
                if (_preloadOp != null) 
                {
                    _preloadOp.allowSceneActivation = false;
                    if (debugLogs) Debug.Log($"[Cutscene] 컷씬 중 씬 프리로드 시작: {nextSceneName}, allowSceneActivation = false");
                }
                else
                {
                    if (debugLogs) Debug.LogWarning($"[Cutscene] 씬 프리로드 실패: {nextSceneName}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Cutscene] 씬 프리로드 오류: {ex.Message}");
                _preloadOp = null;
            }
        }

        _originColor = slimeRenderer.color;
        _originSprite = slimeRenderer.sprite;

        // 1) 슬라임 흰색
        yield return LerpColor(slimeRenderer, _originColor, Color.white, whitenDuration);

        // 2) 화면 번쩍 → 흰색 채우기
        yield return FadeOverlay(0f, 0.8f, flashInDuration);
        yield return FadeOverlay(0.8f, 1f, fillToWhiteDuration);

        // 컨트롤러 OFF + 스프라이트/프레임 교체
        DisableControllers();
        if (spriteAfterFade)
        {
            slimeRenderer.sprite = spriteAfterFade;
            if (replaceFirstFrameInArrays) PatchFramesFirst(spriteAfterFade);
        }

        // 3) 카메라 스냅
        if (cameraSnap != CameraSnapMode.None)
        {
            Vector3 camTargetRoot = (cameraSnap == CameraSnapMode.SlimeBeforeTeleport)
                ? transform.position
                : GetRootPosForAnchorAt(GetTargetAnchorXY());
            SnapCamera(camTargetRoot);
        }

        // 4) 텔레포트 → 공중 앵커로 스냅
        Vector2 targetAnchor = GetTargetAnchorXY();
        Vector2 airAnchor = targetAnchor + Vector2.up * airHeight;
        transform.position = PixelSnap(GetRootPosForAnchorAt(airAnchor));
        SnapAnchorExactlyTo(airAnchor);

        // 유지
        yield return Wait(holdWhiteDuration);

        // 5) 낙하(페이드아웃 동기)
        float groundRootY = GetRootPosForAnchorAt(targetAnchor).y;
        float fallTime = ComputeFallTime(transform.position.y, groundRootY, fallMinTime);

        if (shadowRenderer) { shadowRenderer.gameObject.SetActive(true); UpdateShadow(groundRootY); }
        var fadeCo = StartCoroutine(FadeOverlay(1f, 0f, fallTime));
        yield return FallToRootY_WithVisuals(groundRootY, fallTime, fallEasePower);

        if (hardSnapAfterLand) SnapAnchorExactlyTo(targetAnchor);
        yield return fadeCo;

        // 6) 착지 리액션 + 통통
        yield return SquashOnce(landingSquash, squashDuration);
        if (enableLandShake) yield return TinyShake(shakeAmplitude, shakeDuration);

        Vector2 dir = (hopDirection.sqrMagnitude < 0.0001f) ? Vector2.right : hopDirection.normalized;
        float dist = hopDistance, h = firstHopHeight;
        for (int i = 0; i < hopCount; i++)
        {
            Vector3 s = transform.position;
            Vector3 e = s + (Vector3)(dir * dist);
            yield return ParabolicHop_WithShadow(s, e, h, hopDuration);
            yield return SquashOnce(landingSquash * 0.6f, squashDuration * 0.8f);
            dist *= hopDamping; h *= hopDamping;
        }

        // 7) “…“ 말풍선(1사이클 끝난 ‘직후’ 자동 전환 처리)
        yield return PlayBubbleSequence_Prefab();

        // 자동 전환을 끈 경우, 같은 씬에서만 게임 시작 이벤트
        if (!enableSceneJump && startGameAfterBubble)
            onStartGame?.Invoke();

        // 선택적 원복 (씬 안 바꿀 때)
        if (!keepChangedSprite && _originSprite) slimeRenderer.sprite = _originSprite;
        if (revertFramesAfter) RestorePatchedFrames();
        if (!keepWhiteAfter) yield return LerpColor(slimeRenderer, slimeRenderer.color, _originColor, 0.15f);
        if (reactivateControllersAfter) RestoreControllers();

        transform.localScale = _originScale;
        if (shadowRenderer) shadowRenderer.gameObject.SetActive(false);
        if (debugLogs) Debug.Log("[Cutscene] Completed (no scene jump).");
    }

    // ===== “…“ 말풍선: 프리팹 생성 → 1사이클 끝난 ‘직후’ 씬 전환 =====
    IEnumerator PlayBubbleSequence_Prefab()
    {
        if (!bubblePrefab)
        {
            Debug.LogError("[Bubble] bubblePrefab이 비어 있습니다.");
            yield break;
        }

        Transform parent = bubbleSpawnAsChild ? transform : null;
        GameObject bubble = null;

        try
        {
            bubble = Instantiate(bubblePrefab, Vector3.zero, Quaternion.identity, parent);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[Bubble] Instantiate 실패: " + ex.Message);
            yield break;
        }
        if (!bubble) { Debug.LogError("[Bubble] Instantiate 결과 null"); yield break; }

        bubble.name = bubblePrefab.name + " (Runtime)";
        bubble.SetActive(true);

        if (bubbleForceLayerFromSlime && slimeRenderer)
            SetLayerRecursively(bubble, slimeRenderer.gameObject.layer);

        SpriteRenderer br = bubble.GetComponentInChildren<SpriteRenderer>(true);
        if (!br)
        {
            if (debugLogs) Debug.LogWarning("[Bubble] SpriteRenderer가 없어 자동 추가");
            br = bubble.AddComponent<SpriteRenderer>();
        }
        br.enabled = true;

        if (bubbleCopySortingFromSlime && slimeRenderer)
        {
            br.sortingLayerID = slimeRenderer.sortingLayerID;
            br.sortingOrder = slimeRenderer.sortingOrder + bubbleSortingOrderOffset;
        }

        // 최초 위치 세팅(만화풍 옆 배치 지원)
        PositionBubbleOnce(br);

        // 프레임 애니메이션(요청: 느린 1사이클)
        int frames = (bubbleFrames != null) ? bubbleFrames.Length : 0;
        float dtPerFrame = (bubbleFPS <= 0f) ? 0.15f : 1f / bubbleFPS;

        if (frames > 0)
        {
            int loops = Mathf.Max(1, bubbleLoops);
            for (int l = 0; l < loops; l++)
            {
                for (int i = 0; i < frames; i++)
                {
                    br.sprite = bubbleFrames[i];
                    PositionBubbleOnce(br); // 프레임마다 위치 보정(카메라/스케일 영향 대비)
                    yield return Wait(dtPerFrame);
                }
            }
            if (bubbleHoldLast > 0f) yield return Wait(bubbleHoldLast);
        }
        else
        {
            yield return Wait(Mathf.Max(0.3f, bubbleHoldLast));
        }

        if (bubbleDestroyAfter) Destroy(bubble);
        else bubble.SetActive(false);

        // ★ 여기에서만 자동 씬 전환(켜진 경우)
        if (debugLogs) Debug.Log($"[Cutscene] 말풍선 완료 후 씬 전환 체크: enableSceneJump={enableSceneJump}, nextSceneName='{nextSceneName}'");
        
        if (enableSceneJump && !string.IsNullOrEmpty(nextSceneName))
        {
            if (debugLogs) Debug.Log($"[Cutscene] 씬 전환 시작: {nextSceneName}");
            
            if (whiteOverlay)
            {
                float curA = whiteOverlay.color.a;
                yield return FadeOverlay(curA, 1f, sceneFadeToWhite);
                if (sceneWhiteHold > 0f) yield return Wait(sceneWhiteHold);
            }

            if (_preloadOp != null) 
            {
                if (debugLogs) Debug.Log("[Cutscene] 프리로드된 씬 활성화");
                _preloadOp.allowSceneActivation = true;
            }
            else 
            {
                if (debugLogs) Debug.Log($"[Cutscene] 새로운 씬 로드: {nextSceneName}");
                SceneManager.LoadSceneAsync(nextSceneName);
            }

            yield break;
        }
        else
        {
            if (debugLogs) Debug.Log("[Cutscene] 씬 전환 조건 불만족 - 자동 전환 안함");
        }
    }

    // ===== 말풍선 위치: 만화풍 옆 배치 지원 =====
    void PositionBubbleOnce(SpriteRenderer br)
    {
        if (!br || !slimeRenderer) return;

        Bounds b = slimeRenderer.bounds; // 월드 좌표
        Vector3 pos = br.transform.position;

        bool flip = slimeRenderer.flipX;
        BubbleAnchor mode = bubbleAnchor;
        if (mode == BubbleAnchor.AutoSide)
            mode = flip ? BubbleAnchor.Left : BubbleAnchor.Right;

        // 수직: 바운즈 내 보간 (중앙~살짝 위)
        float yLerp = Mathf.Clamp01(0.5f + bubbleHeightBias);
        float baseY = Mathf.Lerp(b.min.y, b.max.y, yLerp);

        switch (mode)
        {
            case BubbleAnchor.Above:
                pos.x = b.center.x + bubbleOffset.x;
                pos.y = b.max.y + bubbleOffset.y;
                break;

            case BubbleAnchor.Right:
                pos.x = b.max.x + bubbleSidePad + bubbleOffset.x;
                pos.y = baseY + bubbleOffset.y;
                break;

            case BubbleAnchor.Left:
                pos.x = b.min.x - bubbleSidePad + bubbleOffset.x;
                pos.y = baseY + bubbleOffset.y;
                break;
        }

        // Above 모드에서만 flip 반영(옆 배치는 좌우 고정이 자연스러움)
        if (mode == BubbleAnchor.Above && bubbleFlipWithSlime && slimeRenderer.flipX)
        {
            float cx = b.center.x;
            pos.x = (2f * cx) - pos.x;
        }

        pos.z = bubbleZ;
        br.transform.position = PixelSnap(pos);
    }

    // ===== 수동 전환 API (enableSceneJump=false 일 때 이벤트로 호출) =====
    public void RequestSceneJumpNow()
    {
        if (!gameObject.activeInHierarchy) return;
        StartCoroutine(SceneJumpRoutine());
    }
    IEnumerator SceneJumpRoutine()
    {
        if (whiteOverlay)
        {
            float curA = whiteOverlay.color.a;
            yield return FadeOverlay(curA, 1f, sceneFadeToWhite);
            if (sceneWhiteHold > 0f) yield return Wait(sceneWhiteHold);
        }

        if (_preloadOp != null) _preloadOp.allowSceneActivation = true;
        else if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadSceneAsync(nextSceneName);
    }

    // ===== frames[0] 패치 =====
    void PatchFramesFirst(Sprite newSprite)
    {
        _patched.Clear();
        var targets = new List<MonoBehaviour>();
        if (frameArrayOwners != null && frameArrayOwners.Length > 0)
        { foreach (var m in frameArrayOwners) if (m) targets.Add(m); }
        else { targets.AddRange(GetComponents<MonoBehaviour>()); }

        foreach (var beh in targets)
        {
            var fi = beh.GetType().GetField("frames", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fi == null || fi.FieldType != typeof(Sprite[])) continue;
            var arr = fi.GetValue(beh) as Sprite[];
            if (arr == null || arr.Length == 0) continue;

            _patched.Add(new FramePatch { owner = beh, field = fi, backup = (Sprite[])arr.Clone() });
            arr[0] = newSprite; fi.SetValue(beh, arr);
            if (debugLogs) Debug.Log($"[PatchFrames] {beh.GetType().Name}.frames[0] -> {newSprite.name}");
        }
        if (slimeRenderer && newSprite) slimeRenderer.sprite = newSprite;
    }
    void RestorePatchedFrames()
    {
        foreach (var p in _patched)
            if (p.owner != null && p.field != null && p.backup != null) p.field.SetValue(p.owner, p.backup);
        _patched.Clear();
    }

    // ===== 앵커/정렬 =====
    Vector2 GetTargetAnchorXY()
    {
        Vector3 p = targetPoint.position;
        return new Vector2(p.x + teleportOffset.x, p.y + teleportOffset.y);
    }
    Vector2 GetCurrentAnchorXY()
    {
        switch (anchorMode)
        {
            case AnchorMode.FeetTransform:
                if (feetAnchor) return feetAnchor.position;
                goto case AnchorMode.RendererBottomCenter;
            case AnchorMode.RendererBottomCenter:
                if (slimeRenderer) { var b = slimeRenderer.bounds; return new Vector2(b.center.x, b.min.y); }
                break;
            case AnchorMode.ColliderBottomCenter:
                if (_col) { var b = _col.bounds; return new Vector2(b.center.x, b.min.y); }
                break;
        }
        return transform.position;
    }
    Vector3 GetRootPosForAnchorAt(Vector2 anchorXY)
    {
        Vector2 now = GetCurrentAnchorXY();
        Vector2 delta = anchorXY - now;
        return PixelSnap(transform.position + (Vector3)delta);
    }
    void SnapAnchorExactlyTo(Vector2 anchorXY)
    {
        Vector2 now = GetCurrentAnchorXY();
        Vector2 delta = anchorXY - now;
        transform.position = PixelSnap(transform.position + (Vector3)delta);
    }

    // ===== 카메라/픽셀 스냅 =====
    void SnapCamera(Vector3 worldRoot)
    {
        float z = (cameraZMode == CameraZMode.Override) ? cameraZOverride : cameraTransform.position.z;
        Vector3 p = new Vector3(worldRoot.x + cameraXYOffset.x, worldRoot.y + cameraXYOffset.y, z);
        if (cameraUseLocal && cameraTransform.parent)
            cameraTransform.localPosition = cameraTransform.parent.InverseTransformPoint(p);
        else
            cameraTransform.position = p;
    }
    Vector3 PixelSnap(Vector3 v)
    {
        if (!pixelSnap || slimeRenderer == null || slimeRenderer.sprite == null) return v;
        float ppu = Mathf.Max(1f, slimeRenderer.sprite.pixelsPerUnit);
        v.x = Mathf.Round(v.x * ppu) / ppu;
        v.y = Mathf.Round(v.y * ppu) / ppu;
        return v;
    }

    // ===== 낙하/시각효과 =====
    float ComputeFallTime(float startY, float groundY, float minTime)
    {
        float dy = Mathf.Abs(startY - groundY);
        float t = Mathf.Sqrt(dy) * 0.18f;
        return Mathf.Max(minTime, t);
    }

    IEnumerator FallToRootY_WithVisuals(float rootTargetY, float time, float easePow)
    {
        Vector3 start = transform.position;
        if (Mathf.Abs(start.y - rootTargetY) <= 0.0001f)
        { transform.position = PixelSnap(new Vector3(start.x, rootTargetY, start.z)); yield break; }

        time = Mathf.Max(0.01f, time);
        float t = 0f, stretchY = 1f;
        while (t < 1f)
        {
            float dt = Delta();
            t += dt / time;

            float k = Mathf.Pow(Mathf.Clamp01(t), Mathf.Max(1f, easePow));
            float y = Mathf.Lerp(start.y, rootTargetY, k);
            transform.position = PixelSnap(new Vector3(start.x, y, start.z));

            if (enableFallStretch)
            {
                float hNow = Mathf.Max(0f, transform.position.y - rootTargetY);
                float n = Mathf.Clamp01(hNow / Mathf.Max(0.001f, airHeight));
                float targetSY = 1f + fallStretchAmount * n;
                stretchY = Mathf.Lerp(stretchY, targetSY, 1f - Mathf.Exp(-fallStretchSharpness * dt));
                float sx = 1f - (stretchY - 1f) * 0.6f;
                transform.localScale = new Vector3(_originScale.x * sx, _originScale.y * stretchY, 1f);
            }

            UpdateShadow(rootTargetY);
            yield return null;
        }
        transform.position = PixelSnap(new Vector3(start.x, rootTargetY, start.z));
        transform.localScale = _originScale;
        UpdateShadow(rootTargetY);
    }

    IEnumerator ParabolicHop_WithShadow(Vector3 from, Vector3 to, float height, float time)
    {
        time = Mathf.Max(0.01f, time);
        float t = 0f; float groundY = to.y;
        while (t < 1f)
        {
            float dt = Delta();
            t += dt / time;
            float u = Mathf.Clamp01(t);
            Vector3 pos = Vector3.Lerp(from, to, u);
            pos.y = Mathf.Lerp(from.y, to.y, u) + 4f * height * u * (1f - u);
            transform.position = PixelSnap(pos);

            if (enableFallStretch)
            {
                float hNow = Mathf.Max(0f, transform.position.y - groundY);
                float n = Mathf.Clamp01(hNow / Mathf.Max(0.001f, height));
                float sY = 1f + fallStretchAmount * 0.4f * n;
                float sX = 1f - (sY - 1f) * 0.5f;
                transform.localScale = new Vector3(_originScale.x * sX, _originScale.y * sY, 1f);
            }

            UpdateShadow(groundY);
            yield return null;
        }
        transform.position = PixelSnap(to);
        transform.localScale = _originScale;
        UpdateShadow(groundY);
    }

    // ===== 안전한 그림자 갱신 =====
    void UpdateShadow(float groundY)
    {
        if (!enableShadowFX) return;
        if (shadowRenderer == null) return;
        var tr = shadowRenderer.transform;
        if (!tr) return;

        if (!shadowRenderer.gameObject.activeSelf)
            shadowRenderer.gameObject.SetActive(true);

        Vector3 p = tr.position;
        p.x = transform.position.x;
        p.y = groundY + shadowYOffset;
        tr.position = PixelSnap(p);

        float h = Mathf.Max(0f, transform.position.y - groundY);
        float denom = Mathf.Max(0.001f, airHeight);
        float n = Mathf.Clamp01(h / denom);

        float scaleMul = 1f - Mathf.Clamp01(shadowScaleByHeight) * n;
        tr.localScale = new Vector3(
            shadowBaseScale.x * scaleMul,
            shadowBaseScale.y * scaleMul,
            1f
        );

        var c = shadowRenderer.color;
        c.a = Mathf.Lerp(shadowAlphaMinMax.y, shadowAlphaMinMax.x, n);
        shadowRenderer.color = c;
    }

    IEnumerator TinyShake(float amp, float dur)
    {
        if (amp <= 0f || dur <= 0f) yield break;
        Vector3 basePos = cameraTransform.position;
        float t = 0f;
        while (t < dur)
        {
            t += Delta();
            float k = 1f - (t / dur);
            Vector2 r = Random.insideUnitCircle * amp * k;
            cameraTransform.position = new Vector3(basePos.x + r.x, basePos.y + r.y, basePos.z);
            yield return null;
        }
        cameraTransform.position = basePos;
    }

    // ===== 애니 컨트롤러 관리 =====
    void DisableControllers()
    {
        _prevEnabled.Clear();
        foreach (var b in _controllersRuntime)
        {
            if (!b) continue;
            _prevEnabled[b] = b.enabled;
            b.enabled = false;
        }
    }
    void RestoreControllers()
    {
        foreach (var kv in _prevEnabled)
        {
            if (!kv.Key) continue;
            kv.Key.enabled = kv.Value;
        }
        _prevEnabled.Clear();
    }

    // ===== 공용 유틸 =====
    IEnumerator LerpColor(SpriteRenderer sr, Color from, Color to, float time)
    {
        if (time <= 0f) { sr.color = to; yield break; }
        float t = 0f;
        while (t < 1f) { t += Delta() / time; sr.color = Color.Lerp(from, to, Mathf.Clamp01(t)); yield return null; }
        sr.color = to;
    }
    IEnumerator FadeOverlay(float fromA, float toA, float time)
    {
        if (!whiteOverlay) yield break;
        if (time <= 0f) { SetOverlayAlpha(toA); yield break; }
        var c = whiteOverlay.color; float t = 0f;
        while (t < 1f) { t += Delta() / time; c.a = Mathf.Lerp(fromA, toA, Mathf.Clamp01(t)); whiteOverlay.color = c; yield return null; }
        c.a = toA; whiteOverlay.color = c;
    }
    void SetOverlayAlpha(float a)
    {
        if (!whiteOverlay) return; var c = whiteOverlay.color; c.a = a; whiteOverlay.color = c;
    }
    IEnumerator SquashOnce(float amount, float time)
    {
        amount = Mathf.Clamp(amount, 0f, 0.35f); time = Mathf.Max(0.01f, time);
        Vector3 baseS = _originScale, squashed = new Vector3(baseS.x * (1f + amount), baseS.y * (1f - amount), 1f);
        float half = time * 0.5f, t = 0f;
        while (t < 1f) { t += Delta() / half; transform.localScale = Vector3.Lerp(baseS, squashed, Mathf.SmoothStep(0, 1, Mathf.Clamp01(t))); yield return null; }
        t = 0f;
        while (t < 1f) { t += Delta() / half; transform.localScale = Vector3.Lerp(squashed, baseS, Mathf.SmoothStep(0, 1, Mathf.Clamp01(t))); yield return null; }
        transform.localScale = baseS;
    }
    IEnumerator ParabolicHop(Vector3 from, Vector3 to, float height, float time)
    {
        time = Mathf.Max(0.01f, time); float t = 0f;
        while (t < 1f)
        {
            t += Delta() / time; float u = Mathf.Clamp01(t);
            Vector3 pos = Vector3.Lerp(from, to, u);
            pos.y = Mathf.Lerp(from.y, to.y, u) + 4f * height * u * (1f - u);
            transform.position = PixelSnap(pos);
            yield return null;
        }
        transform.position = PixelSnap(to);
    }
    IEnumerator Wait(float s)
    {
        if (s <= 0f) yield break;
        if (useUnscaledTime) yield return new WaitForSecondsRealtime(s);
        else yield return new WaitForSeconds(s);
    }
    float Delta() => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

    // 자식들까지 Unity 레이어를 재귀적으로 맞춰주는 유틸
    void SetLayerRecursively(GameObject go, int layer)
    {
        if (!go) return;
        go.layer = layer;
        foreach (Transform child in go.transform)
        {
            if (child) SetLayerRecursively(child.gameObject, layer);
        }
    }


#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!slimeRenderer || !targetPoint) return;
        Vector2 targ = GetTargetAnchorXY();
        Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(targ, 0.06f);
        Vector2 cur = Application.isPlaying ? GetCurrentAnchorXY()
                    : new Vector2(slimeRenderer.bounds.center.x, slimeRenderer.bounds.min.y);
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(cur, 0.06f);
    }
#endif
}
