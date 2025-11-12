using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine.EventSystems;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using JetBrains.Annotations;

#endif

#if UNITY_AI_NAVIGATION || UNITY_2019_1_OR_NEWER
using UnityEngine.AI;
#endif

public class DialogueManager : MonoBehaviour
{
    [Header("출력해줄 다이얼로그 말풍선 스프라이트")]
    public GameObject dialogueBox;

    [Header("텍스트 (프리팹 안의 TMP 참조)")]
    public TextMeshProUGUI usedText;

    [Header("대화할 데이터")]
    public TalkData NPCTalkDatable;

    [Header("플레이어 태그")]
    public string usedTag = "Player";

    [Header("다이얼로그 캔버스")]
    public GameObject usedCanvas;

    [Header("시네머신 (있으면 사용, 없으면 런타임 생성)")]
    public CinemachineCamera cinemachineCamera;

    [Header("플레이어(자동 바인딩됨)")]
    public GameObject playerSet;

    [Header("원샷(한 번만 실행) 설정")]
    public bool oneShot = true;

    [Tooltip("원샷 차단용으로 끌 콜라이더. 비우면 자동으로 자기 자신/자식에서 찾아요.")]
    public Collider2D triggerCollider;

    [Tooltip("콜라이더를 끄는 대신 이 스크립트를 파괴해서 완전히 막고 싶으면 체크 (대화 종료 후 파괴)")]
    public bool destroyComponentInstead = false;

    // 내부 UI 상태
    GameObject dialgoueSet;
    GameObject dialTextSet;
    TextMeshProUGUI usedMesh;
    GameObject[] _allUI;

    bool isTalk = false;
    bool _consumed = false;
    int currentLine = 0;

    [Header("타이핑 속도 (글자당 딜레이)")]
    public float typingSpeed = 0.05f;

    // === 말풍선 자동 리사이즈 ===
    [Header("토크박스 리사이즈 & 기본 크기")]
    public bool autoResize = true;                         // 글자에 맞춰 박스 커짐
    public bool overrideBaseSize = true;                   // 줄 시작 시 기본 크기로 초기화
    public Vector2 baseSize = new Vector2(380f, 180f);     // 기본 크기
    public Vector2 dialogMinSize = new Vector2(300f, 140f);
    public Vector2 dialogMaxSize = new Vector2(900f, 480f);
    public Vector2 dialogPadding = new Vector2(40f, 30f);  // 글자 주변 여백
    [Min(0.25f)] public float boxScale = 1f;

    RectTransform _bubbleRT;
    Vector2 _dialogBaseSize;

    // ── 홀드 스킵(꾹 눌러 빠르게) ─────────────────────────────────────────────
    [Header("홀드 스킵(꾹 눌러 빠르게 넘기기)")]
    public bool allowHoldSkip = true;
    public float holdThreshold = 0.25f;
    public float fastLineDelay = 0.03f;

    float _holdStartUnscaled = -1f;
    bool _pressingNow = false;
    bool _pressingPrev = false;
    bool _fastForward = false;

    // ── 스킵 힌트 TMP (우상단 고정) ──────────────────────────────────────────
    [Header("스킵 힌트(TMP)")]
    public bool showSkipHint = true;
    public string holdHintText = "꾹 누르면 빠르게 넘김";
    public string fastHintText = "빠르게 넘기는 중...";
    public TMP_FontAsset uiFont;
    [Min(8)] public int hintFontSize = 26;
    public Color hintColor = new Color(1f, 1f, 1f, 0.9f);
    public float hintFadeIn = 0.25f;
    public float hintFadeOut = 0.15f;

    [Header("스킵 힌트(화면 우상단 고정)")]
    public Vector2 skipHintScreenOffset = new Vector2(32f, 32f); // (오른쪽/위에서 안쪽으로)
    public float skipHintMaxWidth = 700f;

    TextMeshProUGUI _hintTMP;

    // 카메라 복구용
    float _origOrthoSize = -1f;
    int _origPriority = 0;
    bool _origEnabled = true;
    bool _createdRuntimeVCam = false;
    Coroutine _hardTrackRoutine;

    // 플레이어/입력 관련
    [Header("플레이어 제어 차단 옵션")]
    public bool blockPlayerInput = true;

    [Tooltip("대화 중 비활성화할 추가 스크립트(오토런/대시/패스 등).")]
    public Behaviour[] movementScriptsToDisable;

#if ENABLE_INPUT_SYSTEM
    PlayerInput _playerInput;
    bool _playerInputWasEnabled = false;
#endif

    PlayerController _pc;          // 대상 플레이어 컨트롤러
    Rigidbody2D _rb;               // 선택: 물리 사용 시만
    bool _rbHad;
    RigidbodyConstraints2D _rbPrevConstraints;
    Vector2 _rbPrevVelocity;

#if UNITY_AI_NAVIGATION || UNITY_2019_1_OR_NEWER
    NavMeshAgent _agent;
    bool _hadAgent = false;
    bool _agentPrevStopped = false;
    float _agentPrevSpeed, _agentPrevAccel, _agentPrevAngSpeed;
#endif

    bool[] _movementPrevEnabled;

    bool _destroyAfterDialogue = false;

    public bool isHeal = false;

    // ===== 스프라이트 교체(초상화) + 원상복구 ==================================
    [Header("스프라이트 교체(이 오브젝트의 SpriteRenderer만)")]
    [Min(0.50f)] public float popDownScaleY = 0.85f;
    [Min(0.01f)] public float popDurDown = 0.06f;
    [Min(0.01f)] public float popDurUp = 0.12f;

    SpriteRenderer _selfSR;
    Sprite _lastAppliedSprite;
    Sprite _origSprite;
    // ==========================================================================

    void Awake()
    {
        if (triggerCollider == null)
        {
            triggerCollider = GetComponent<Collider2D>();
            if (triggerCollider == null)
                triggerCollider = GetComponentInChildren<Collider2D>();
        }

        _selfSR = GetComponent<SpriteRenderer>();
        if (_selfSR == null) _selfSR = GetComponentInChildren<SpriteRenderer>(true);
        if (_selfSR != null) _origSprite = _selfSR.sprite;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_consumed) return;
        if (!collision.CompareTag(usedTag)) return;
        if (isTalk) return;

        _pc = collision.GetComponentInParent<PlayerController>();
        if (_pc != null) playerSet = _pc.gameObject;
        else playerSet = collision.transform.root.gameObject;

#if ENABLE_INPUT_SYSTEM
        _playerInput = playerSet ? playerSet.GetComponentInParent<PlayerInput>() : null;
#endif
        _rb = playerSet ? playerSet.GetComponentInParent<Rigidbody2D>() : null;
        _rbHad = _rb != null;

#if UNITY_AI_NAVIGATION || UNITY_2019_1_OR_NEWER
        _agent = playerSet ? playerSet.GetComponentInParent<NavMeshAgent>() : null;
        _hadAgent = _agent != null;
#endif

        if (movementScriptsToDisable != null && movementScriptsToDisable.Length > 0)
            _movementPrevEnabled = new bool[movementScriptsToDisable.Length];

        if (oneShot)
        {
            if (destroyComponentInstead)
            {
                _destroyAfterDialogue = true; // 종료 후 파괴
                DisableTriggerCollider();
                _consumed = true;
            }
            else
            {
                DisableTriggerCollider();
                _consumed = true;
            }
        }

        FreezePlayer();

        isTalk = true;
        DialogueStart();
    }

    void DisableTriggerCollider()
    {
        if (triggerCollider != null && triggerCollider.enabled)
            triggerCollider.enabled = false;
    }

    void EnsureCameraRig()
    {
        var mainCam = Camera.main;
        if (mainCam == null)
        {
            var go = new GameObject("Main Camera");
            mainCam = go.AddComponent<Camera>();
            go.tag = "MainCamera";
        }
        if (mainCam.GetComponent<CinemachineBrain>() == null)
            mainCam.gameObject.AddComponent<CinemachineBrain>();

        if (cinemachineCamera == null)
        {
            var vgo = new GameObject("DialogueVCam");
            cinemachineCamera = vgo.AddComponent<CinemachineCamera>();
            _createdRuntimeVCam = true;
        }

        if (!cinemachineCamera.TryGetComponent<CinemachinePositionComposer>(out _))
            cinemachineCamera.gameObject.AddComponent<CinemachinePositionComposer>();
    }

    void DialogueStart()
    {
        // UI 전역 비활성 (⚠ usedCanvas는 살려둠)
        _allUI = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        int uiLayer = LayerMask.NameToLayer("UI");
        foreach (GameObject t in _allUI)
        {
            if (t == null) continue;
            if (t.layer == uiLayer && (usedCanvas == null || t != usedCanvas))
                t.SetActive(false);
        }
        if (usedCanvas != null) usedCanvas.SetActive(true);

        // 카메라 설정
        EnsureCameraRig();

        if (_origOrthoSize < 0f) _origOrthoSize = cinemachineCamera.Lens.OrthographicSize;
        _origPriority = cinemachineCamera.Priority;
        _origEnabled = cinemachineCamera.enabled;

        cinemachineCamera.enabled = true;
        cinemachineCamera.Priority = 10000;

        var ct = cinemachineCamera.Target;
        ct.TrackingTarget = transform;      // NPC
        cinemachineCamera.Target = ct;

        cinemachineCamera.PreviousStateIsValid = false;
        cinemachineCamera.Lens.OrthographicSize = 3f;

        _hardTrackRoutine = StartCoroutine(HardTrackRoutine(transform));

        // 말풍선/UI 생성 (✅ 기존 방식 그대로)
        dialgoueSet = Instantiate(dialogueBox, usedCanvas.transform, false);
        dialgoueSet.transform.position = transform.position;

        dialTextSet = Instantiate(usedText.gameObject, usedCanvas.transform, false);
        dialTextSet.transform.position = transform.position;

        usedMesh = dialTextSet.GetComponent<TextMeshProUGUI>();
        usedMesh.text = "";

        // 말풍선 RT 캐시 + 기본 크기
        _bubbleRT = dialgoueSet.GetComponent<RectTransform>();
        _dialogBaseSize = (_bubbleRT != null)
            ? (overrideBaseSize ? baseSize : _bubbleRT.sizeDelta)
            : baseSize;

        usedMesh.DOFade(1f, 0.5f);
        dialTextSet.transform.DOMoveY(transform.position.y + 1f, 0.5f);
        var img = dialgoueSet.GetComponent<Image>();
        if (img) img.DOFade(1f, 0.5f);
        dialgoueSet.transform.DOMoveY(transform.position.y + 1f, 0.5f);

        // 🔵 스킵 힌트: 화면 우상단 고정
        SetupSkipHint();

        StartCoroutine(DialogueRoutine());
    }

    IEnumerator HardTrackRoutine(Transform target)
    {
        var mc = Camera.main;
        while (isTalk && cinemachineCamera != null && target != null)
        {
            if (mc != null)
            {
                Vector3 p = target.position;
                cinemachineCamera.transform.position = new Vector3(p.x, p.y, mc.transform.position.z);
                cinemachineCamera.transform.rotation = mc.transform.rotation;
            }
            UpdateHoldState();
            UpdateSkipHintText();

            // 움직이는 NPC라면 위치 갱신이 필요하면 아래 2줄 활성
            // dialgoueSet.transform.position = target.position + Vector3.up * 1f;
            // dialTextSet.transform.position = target.position + Vector3.up * 1f;

            yield return null;
        }
    }

    // 🔵 스킵 힌트: 화면 우상단 고정만 담당 (본문 위치 변경 X)
    void SetupSkipHint()
    {
        if (!showSkipHint) return;

        if (_hintTMP != null && _hintTMP.gameObject != null)
            Destroy(_hintTMP.gameObject);

        var screenCanvas = FindScreenSpaceCanvas();
        if (screenCanvas == null) screenCanvas = CreateOverlayCanvas("_Runtime_UI_Overlay");

        var go = new GameObject("SkipHintTMP", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(screenCanvas.GetComponent<RectTransform>(), false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-Mathf.Abs(skipHintScreenOffset.x),
                                          -Mathf.Abs(skipHintScreenOffset.y));
        rt.sizeDelta = new Vector2(skipHintMaxWidth, 80f);

        _hintTMP = go.GetComponent<TextMeshProUGUI>();
        if (uiFont != null) _hintTMP.font = uiFont;
        _hintTMP.fontSize = hintFontSize;
        _hintTMP.color = hintColor;
        _hintTMP.textWrappingMode = TextWrappingModes.Normal; // 자동 줄바꿈 허용
        _hintTMP.alignment = TextAlignmentOptions.TopRight;
        _hintTMP.text = holdHintText;
        _hintTMP.alpha = 0f;
        _hintTMP.DOFade(1f, hintFadeIn);
    }

    void UpdateSkipHintText()
    {
        if (!showSkipHint || _hintTMP == null) return;

        string want = _fastForward ? fastHintText : holdHintText;
        if (_hintTMP.text != want)
        {
            _hintTMP.DOKill();
            Sequence s = DOTween.Sequence();
            s.Append(_hintTMP.DOFade(0f, 0.08f));
            s.AppendCallback(() => _hintTMP.text = want);
            s.Append(_hintTMP.DOFade(1f, 0.12f));
        }
    }

    Canvas FindScreenSpaceCanvas()
    {
        var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        Canvas fallback = null;
        foreach (var c in canvases)
        {
            if (!c.isActiveAndEnabled) continue;
            if (c.renderMode == RenderMode.ScreenSpaceOverlay) return c;
            if (c.renderMode == RenderMode.ScreenSpaceCamera && fallback == null) fallback = c;
        }
        return fallback;
    }

    Canvas CreateOverlayCanvas(string name)
    {
        var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var c = go.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        return c;
    }

    IEnumerator DialogueRoutine()
    {
        Coroutine holdWatcher = StartCoroutine(HoldWatcher());

        while (currentLine < NPCTalkDatable.talks.Count)
        {
            // 표정 스왑
            TrySwapPortrait(currentLine);

            // 줄 시작: 말풍선 기본 크기로 초기화
            if (_bubbleRT != null)
            {
                var init = _dialogBaseSize * Mathf.Max(0.25f, boxScale);
                _bubbleRT.sizeDelta = init;
            }

            // 한 줄 타이핑 (홀드 시 즉시 완타)
            yield return StartCoroutine(TypeLineWithHold(NPCTalkDatable.talks[currentLine].talkString));

            if (allowHoldSkip && _fastForward)
            {
                yield return new WaitForSeconds(fastLineDelay);
                currentLine++;
                continue;
            }

            // 입력 대기(스페이스/클릭/터치)
            yield return new WaitUntil(AdvanceTapped);

            currentLine++;
        }

        if (holdWatcher != null) StopCoroutine(holdWatcher);
        EndDialogue();
    }

    IEnumerator TypeLineWithHold(string line)
    {
        usedMesh.text = "";

        // 홀드 중이면 즉시 완타
        if (allowHoldSkip && _fastForward)
        {
            usedMesh.text = line;
            ResizeBubbleToTextInstant();
            yield break;
        }

        for (int i = 0; i < line.Length; i++)
        {
            usedMesh.text += line[i];

            // 글자 몇 개마다 한 번 리사이즈(오버헤드 최소화)
            if (autoResize && (i % 2 == 0 || i == line.Length - 1))
                ResizeBubbleToTextInstant();

            UpdateHoldState();
            if (allowHoldSkip && _fastForward)
            {
                if (i < line.Length - 1)
                {
                    usedMesh.text += line.Substring(i + 1);
                    ResizeBubbleToTextInstant(); // 최종 보정
                }
                break;
            }

            yield return new WaitForSeconds(typingSpeed);
        }
    }

    void ResizeBubbleToTextInstant()
    {
        if (!autoResize || _bubbleRT == null || usedMesh == null) return;

        float maxTextWidth = Mathf.Max(1f, dialogMaxSize.x - dialogPadding.x);
        Vector2 pref = usedMesh.GetPreferredValues(usedMesh.text, maxTextWidth, 0f);

        float w = Mathf.Clamp(pref.x + dialogPadding.x, dialogMinSize.x, dialogMaxSize.x);
        float h = Mathf.Clamp(pref.y + dialogPadding.y, dialogMinSize.y, dialogMaxSize.y);

        _bubbleRT.sizeDelta = new Vector2(w, h) * Mathf.Max(0.25f, boxScale);
    }

    void EndDialogue()
    {
        if (_hintTMP != null) _hintTMP.DOFade(0f, hintFadeOut);

        if (dialgoueSet) Destroy(dialgoueSet);
        if (dialTextSet) Destroy(dialTextSet);

        // 표정/스프라이트 원상복구
        if (_selfSR != null) _selfSR.sprite = _origSprite;

        isTalk = false;
        currentLine = 0;

        // 카메라 복귀
        if (_hardTrackRoutine != null)
        {
            StopCoroutine(_hardTrackRoutine);
            _hardTrackRoutine = null;
        }

        if (cinemachineCamera != null)
        {
            var ct = cinemachineCamera.Target;
            ct.TrackingTarget = playerSet != null ? playerSet.transform : null;
            cinemachineCamera.Target = ct;

            cinemachineCamera.PreviousStateIsValid = false;

            cinemachineCamera.Lens.OrthographicSize = (_origOrthoSize > 0f) ? _origOrthoSize : 5.6f;
            cinemachineCamera.Priority = _origPriority;
            cinemachineCamera.enabled = _origEnabled;

            if (_createdRuntimeVCam)
            {
                Destroy(cinemachineCamera.gameObject);
                cinemachineCamera = null;
                _createdRuntimeVCam = false;
            }
            if (isHeal)
            {

            }
        }

         void PlayerHeal()
        {

        }

        // 플레이어/조이스틱 복구
        UnfreezePlayer();

        // 2초 뒤에 움직임 풀기
        StartCoroutine(DelayedUnlockPlayer(2.0f));

        // UI 복구 (usedCanvas 포함)
        if (_allUI != null)
        {
            foreach (GameObject t in _allUI)
                if (t != null && t.layer == LayerMask.NameToLayer("UI"))
                    t.SetActive(true);
        }

        if (_destroyAfterDialogue)
            Destroy(this);

        var waveManager = FindFirstObjectByType<WaveManager>();
        if (waveManager != null) waveManager.RestoreCameraAndRoom();
    }

    IEnumerator DelayedUnlockPlayer(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (GameManager.Instance != null && GameManager.Instance.playerController != null)
            GameManager.Instance.playerController.UnLockMovement();
    }

    void OnDisable()
    {
        if (isTalk)
        {
            try { UnfreezePlayer(); } catch { }
            isTalk = false;
            if (_selfSR != null) _selfSR.sprite = _origSprite;
        }
    }

    // 입력 체크(모호성 완전 차단)
    bool AdvanceTapped()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) return true;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
        if (Touchscreen.current != null)
        {
            var t = Touchscreen.current.primaryTouch;
            if (t.press.wasPressedThisFrame) return true;
        }
        return false;
#else
        if (Input.GetKeyDown(KeyCode.Space)) return true;
        if (Input.GetMouseButtonDown(0)) return true;

        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                if (t.phase == TouchPhase.Began) return true;
            }
        }
        return false;
#endif
    }

    // 홀드 스킵 상태 갱신
    void UpdateHoldState()
    {
        _pressingPrev = _pressingNow;
        _pressingNow = IsPressingNow();

        if (_pressingNow && !_pressingPrev)
            _holdStartUnscaled = Time.unscaledTime;

        if (allowHoldSkip && _pressingNow && _holdStartUnscaled > 0f)
            _fastForward = (Time.unscaledTime - _holdStartUnscaled) >= holdThreshold;

        if (!_pressingNow && _pressingPrev)
        {
            _fastForward = false;
            _holdStartUnscaled = -1f;
        }
    }

    IEnumerator HoldWatcher()
    {
        while (isTalk)
        {
            UpdateHoldState();
            yield return null;
        }
    }

    bool IsPressingNow()
    {
#if ENABLE_INPUT_SYSTEM
        bool k = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
        bool m = Mouse.current != null && Mouse.current.leftButton.isPressed;
        bool t = false;
        if (Touchscreen.current != null)
        {
            var ts = Touchscreen.current;
            for (int i = 0; i < ts.touches.Count; i++)
                if (ts.touches[i].press.isPressed) { t = true; break; }
        }
        return k || m || t;
#else
        bool k = Input.GetKey(KeyCode.Space);
        bool m = Input.GetMouseButton(0);
        bool t = false;
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Stationary || touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Began)
                { t = true; break; }
            }
        }
        return k || m || t;
#endif
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 하드 스톱 / 복구 (조이스틱은 끄지 않고 중립화만)
    void FreezePlayer()
    {
        if (playerSet == null) return;

        if (_pc == null)
            _pc = playerSet.GetComponentInParent<PlayerController>();

        // 1) PlayerController 잠금
        if (_pc != null)
        {
            _pc.LockMovement(true);
            _pc.inputVec = Vector2.zero;
            if (_pc.joystick != null && _pc.joystick.gameObject != null)
                ResetJoystickObject(_pc.joystick.gameObject);
        }

        // 2) PlayerInput 비활성(옵션)
#if ENABLE_INPUT_SYSTEM
        if (blockPlayerInput && _playerInput != null)
        {
            _playerInputWasEnabled = _playerInput.enabled;
            if (_playerInput.actions != null) _playerInput.actions.Disable();
            _playerInput.enabled = false;
        }
#endif

        // 3) Rigidbody2D 정지
        if (_rbHad && _rb != null)
        {
            _rbPrevVelocity = _rb.linearVelocity;
            _rbPrevConstraints = _rb.constraints;

            _rb.linearVelocity = Vector2.zero;
            _rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }

        // 4) NavMeshAgent 정지
#if UNITY_AI_NAVIGATION || UNITY_2019_1_OR_NEWER
        if (_hadAgent && _agent != null)
        {
            _agentPrevStopped = _agent.isStopped;
            _agentPrevSpeed = _agent.speed;
            _agentPrevAccel = _agent.acceleration;
            _agentPrevAngSpeed = _agent.angularSpeed;

            _agent.isStopped = true;
            _agent.velocity = Vector3.zero;
            _agent.speed = 0f;
            _agent.acceleration = 0f;
            _agent.angularSpeed = 0f;
        }
#endif

        // 5) 기타 이동 스크립트 off
        if (movementScriptsToDisable != null)
        {
            for (int i = 0; i < movementScriptsToDisable.Length; i++)
            {
                if (movementScriptsToDisable[i] == null) continue;
                _movementPrevEnabled[i] = movementScriptsToDisable[i].enabled;
                movementScriptsToDisable[i].enabled = false;
            }
        }

        // 6) 혹시 남아있을 트윈 이동 제거
        if (_pc != null) DOTween.Kill(_pc.transform, complete: false);
        if (_rb != null) DOTween.Kill(_rb, complete: false);
    }

    void UnfreezePlayer()
    {
        // A) 물리/AI부터 복구
        if (_rbHad && _rb != null)
        {
            _rb.constraints = _rbPrevConstraints;
            _rb.linearVelocity = _rbPrevVelocity;
        }

#if UNITY_AI_NAVIGATION || UNITY_2019_1_OR_NEWER
        if (_hadAgent && _agent != null)
        {
            _agent.isStopped = _agentPrevStopped;
            _agent.speed = _agentPrevSpeed;
            _agent.acceleration = _agentPrevAccel;
            _agent.angularSpeed = _agentPrevAngSpeed;
        }
#endif

        // B) PlayerController 중립 상태에서 해제
        if (_pc != null)
        {
            _pc.inputVec = Vector2.zero;
            if (_pc.joystick != null && _pc.joystick.gameObject != null)
                ResetJoystickObject(_pc.joystick.gameObject);
        }

        // C) PlayerInput 켜기(옵션)
#if ENABLE_INPUT_SYSTEM
        if (blockPlayerInput && _playerInput != null)
        {
            _playerInput.enabled = _playerInputWasEnabled;
            if (_playerInput.actions != null) _playerInput.actions.Enable();
        }
#endif

        // D) 기타 이동 스크립트 복구
        if (movementScriptsToDisable != null)
        {
            for (int i = 0; i < movementScriptsToDisable.Length; i++)
            {
                if (movementScriptsToDisable[i] == null) continue;
                movementScriptsToDisable[i].enabled = _movementPrevEnabled[i];
            }
        }
    }

    // 조이스틱 중립 강제 리셋(패키지 불문)
    void ResetJoystickObject(GameObject go)
    {
        if (go == null) return;

        // 1) PointerUp 강제
        try
        {
            var ped = new PointerEventData(EventSystem.current);
            var ups = go.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var m in ups)
            {
                if (m is IPointerUpHandler up)
                    up.OnPointerUp(ped);
            }
        }
        catch { }

        // 2) 패키지별 메서드 호출 시도
        try
        {
            var monos = go.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var mb in monos)
            {
                var t = mb.GetType();
                string[] cand = { "Reset", "Release", "OnRelease", "OnPointerUp" };
                foreach (var name in cand)
                {
                    var mi = t.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, System.Type.EmptyTypes, null);
                    if (mi != null)
                    {
                        mi.Invoke(mb, null);
                        break;
                    }
                }
            }
        }
        catch { }

        // 3) 핸들 UI 중앙 스냅
        try
        {
            var rts = go.GetComponentsInChildren<RectTransform>(true);
            foreach (var rt in rts)
            {
                var n = rt.name.ToLowerInvariant();
                if (n.Contains("handle") || n.Contains("knob"))
                    rt.anchoredPosition = Vector2.zero;
            }
        }
        catch { }
    }

    // ===== 초상화 스왑 =========================================================
    void TrySwapPortrait(int lineIndex)
    {
        if (_selfSR == null) return;
        if (NPCTalkDatable == null || NPCTalkDatable.talks == null) return;
        if (lineIndex < 0 || lineIndex >= NPCTalkDatable.talks.Count) return;

        var next = NPCTalkDatable.talks[lineIndex].talkSprite;
        if (next == null) return;
        if (_selfSR.sprite == next || _lastAppliedSprite == next) return;

        StartCoroutine(SwapSpriteWithPop(next));
    }

    IEnumerator SwapSpriteWithPop(Sprite next)
    {
        if (_selfSR == null) yield break;

        var t = transform;
        var baseScale = t.localScale;

        DOTween.Kill(t, false);

        // 1) 살짝 앉기
        yield return t.DOScaleY(baseScale.y * popDownScaleY, popDurDown)
                      .SetEase(Ease.InQuad)
                      .WaitForCompletion();

        // 2) 스프라이트 교체
        _selfSR.sprite = next;
        _lastAppliedSprite = next;

        // 3) 튕기며 원복
        yield return t.DOScaleY(baseScale.y, popDurUp)
                      .SetEase(Ease.OutBack)
                      .WaitForCompletion();

        t.localScale = baseScale;
    }
}
