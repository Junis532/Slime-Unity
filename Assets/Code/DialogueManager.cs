using System.Collections;
using System.Collections.Generic;
using System.Reflection;            // 조이스틱 리셋용(리플렉션)
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine.EventSystems;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;      // 새 입력 시스템
#endif

#if UNITY_AI_NAVIGATION || UNITY_2019_1_OR_NEWER
using UnityEngine.AI;
#endif

public class DialogueManager : MonoBehaviour
{
    [Header("출력해줄 다이얼로그 말풍선 스프라이트")]
    public GameObject dialogueBox;

    [Header("텍스트")]
    public TextMeshProUGUI usedText;

    [Header("대화할 데이터")]
    public TalkData NPCTalkDatable;

    [Header("플레이어 태그")]
    public string usedTag = "";

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
    GameObject[] temp;

    bool isTalk = false;
    bool _consumed = false;
    int currentLine = 0;

    [Header("타이핑 속도 (글자당 딜레이)")]
    public float typingSpeed = 0.05f;

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

    // 파괴를 대화 종료 후로 미루기
    bool _destroyAfterDialogue = false;

    // ===== [추가: 스프라이트 교체 전용 옵션/캐시] ==========================
    [Header("스프라이트 교체(이 오브젝트의 SpriteRenderer만)")]
    [Min(0.50f)] public float popDownScaleY = 0.85f;  // 살짝 앉기
    [Min(0.01f)] public float popDurDown = 0.06f;
    [Min(0.01f)] public float popDurUp = 0.12f;

    SpriteRenderer _selfSR;        // 이 스크립트가 붙은 GO의 SR
    Sprite _lastAppliedSprite;     // 마지막 적용 스프라이트
    // =====================================================================

    // ─────────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (triggerCollider == null)
        {
            triggerCollider = GetComponent<Collider2D>();
            if (triggerCollider == null)
                triggerCollider = GetComponentInChildren<Collider2D>();
        }

        // ===== [추가] 이 오브젝트의 SpriteRenderer 캐시 ====================
        _selfSR = GetComponent<SpriteRenderer>();
        if (_selfSR == null) _selfSR = GetComponentInChildren<SpriteRenderer>(true);
        // ===================================================================
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_consumed) return;
        if (!collision.CompareTag(usedTag)) return;
        if (isTalk) return;

        // 충돌한 객체 기준으로 PlayerController 찾기
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

        // 원샷 처리 (즉시 파괴 금지)
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

        // 하드 스톱(조이스틱은 끄지 않고 중립화만)
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
        // UI 전역 비활성
        temp = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject t in temp)
            if (t.layer == LayerMask.NameToLayer("UI"))
                t.gameObject.SetActive(false);

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

        // 말풍선/UI 생성
        dialgoueSet = Instantiate(dialogueBox);
        dialgoueSet.transform.position = transform.position;
        dialTextSet = Instantiate(usedText.gameObject);
        dialTextSet.transform.position = transform.position;

        dialgoueSet.transform.SetParent(usedCanvas.transform, false);
        dialTextSet.transform.SetParent(usedCanvas.transform, false);

        usedMesh = dialTextSet.GetComponent<TextMeshProUGUI>();
        usedMesh.text = "";

        usedMesh.DOFade(1f, 0.5f);
        dialTextSet.transform.DOMoveY(transform.position.y + 1f, 0.5f);
        dialgoueSet.GetComponent<Image>().DOFade(1f, 0.5f);
        dialgoueSet.transform.DOMoveY(transform.position.y + 1f, 0.5f);

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
            yield return null;
        }
    }

    IEnumerator DialogueRoutine()
    {
        while (currentLine < NPCTalkDatable.talks.Count)
        {
            // ===== [추가] 이 줄 시작 전에 초상화/스프라이트 교체 시도 =====
            TrySwapPortrait(currentLine);
            // ============================================================

            yield return StartCoroutine(DialTyping(NPCTalkDatable.talks[currentLine].talkString));
            currentLine++;

            // 입력 대기(스페이스/클릭/터치)
            yield return new WaitUntil(AdvanceTapped);

            var rt = dialgoueSet.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(150, 150);
        }
        EndDialogue();
    }

    IEnumerator DialTyping(string line)
    {
        usedMesh.text = "";
        var rt = dialgoueSet.GetComponent<RectTransform>();
        foreach (char c in line)
        {
            usedMesh.text += c;
            rt.sizeDelta = new Vector2(rt.sizeDelta.x + 30f, rt.sizeDelta.y);
            yield return new WaitForSeconds(typingSpeed);
        }
    }

    void EndDialogue()
    {
        if (dialgoueSet) Destroy(dialgoueSet);
        if (dialTextSet) Destroy(dialTextSet);

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
        }

        // 플레이어/조이스틱 복구 (중립 리셋 포함)
        UnfreezePlayer();

        // UI 복구
        if (temp != null)
        {
            foreach (GameObject t in temp)
                if (t != null && t.layer == LayerMask.NameToLayer("UI"))
                    t.gameObject.SetActive(true);
        }

        // 대화 끝난 뒤에만 파괴
        if (_destroyAfterDialogue)
            Destroy(this);
    }

    void OnDisable()
    {
        // 예외 상황에서도 반드시 풀기
        if (isTalk)
        {
            try { UnfreezePlayer(); } catch { }
            isTalk = false;
        }
    }

    // 입력 체크(모호성 완전 차단)
    bool AdvanceTapped()
    {
#if ENABLE_INPUT_SYSTEM
        if (global::UnityEngine.InputSystem.Keyboard.current != null &&
            global::UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
            return true;

        if (global::UnityEngine.InputSystem.Mouse.current != null &&
            global::UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
            return true;

        if (global::UnityEngine.InputSystem.Touchscreen.current != null)
        {
            var t = global::UnityEngine.InputSystem.Touchscreen.current.primaryTouch;
            if (t.press.wasPressedThisFrame) return true;
        }
        return false;
#else
        if (global::UnityEngine.Input.GetKeyDown(global::UnityEngine.KeyCode.Space)) return true;

        if (global::UnityEngine.Input.GetMouseButtonDown(0))
        {
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                return true;
            return true;
        }

        if (global::UnityEngine.Input.touchCount > 0)
        {
            for (int i = 0; i < global::UnityEngine.Input.touchCount; i++)
            {
                global::UnityEngine.Touch t = global::UnityEngine.Input.GetTouch(i);
                if (t.phase == global::UnityEngine.TouchPhase.Began)
                {
                    if (UnityEngine.EventSystems.EventSystem.current != null &&
                        UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(i))
                        return true;
                    return true;
                }
            }
        }
        return false;
#endif
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 하드 스톱 / 복구 (조이스틱은 끄지 않고 중립화만)
    void FreezePlayer()
    {
        if (playerSet == null) return;

        if (_pc == null)
            _pc = playerSet.GetComponentInParent<PlayerController>();

        // 1) PlayerController 잠금 (컴포넌트 Enabled는 건드리지 않음)
        if (_pc != null)
        {
            _pc.LockMovement(true);            // canMove=false + 입력/방향 0
            _pc.inputVec = Vector2.zero;       // 한 번 더 0
            // 조이스틱 중립 강제 (있는 경우)
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

        // 3) Rigidbody2D(있으면) 안전 정지 — simulated는 건드리지 않음
        if (_rbHad && _rb != null)
        {
            _rbPrevVelocity = _rb.linearVelocity;
            _rbPrevConstraints = _rb.constraints;

            _rb.linearVelocity = Vector2.zero;
            _rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }

        // 4) NavMeshAgent(있으면) 정지
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
            _pc.inputVec = Vector2.zero;               // 입력 0
            if (_pc.joystick != null && _pc.joystick.gameObject != null)
                ResetJoystickObject(_pc.joystick.gameObject);   // 조이스틱 0,0

            _pc.UnlockMovement();                      // canMove=true
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

    // ─────────────────────────────────────────────────────────────────────────────
    // 조이스틱 중립 강제 리셋(패키지 불문)
    void ResetJoystickObject(GameObject go)
    {
        if (go == null) return;

        // 1) PointerUp 강제 → "손 뗀" 상태
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

        // 2) 패키지별 메서드 호출 시도(Reset/Release/OnRelease/OnPointerUp(매개변수X))
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

    // ===== [추가 메서드] 현재 줄 인덱스의 talkSprite로 교체 시도 ==============
    void TrySwapPortrait(int lineIndex)
    {
        if (_selfSR == null) return;
        if (NPCTalkDatable == null || NPCTalkDatable.talks == null) return;
        if (lineIndex < 0 || lineIndex >= NPCTalkDatable.talks.Count) return;

        var next = NPCTalkDatable.talks[lineIndex].talkSprite;
        if (next == null) return;                // 비어있으면 패스
        if (_selfSR.sprite == next || _lastAppliedSprite == next) return; // 같으면 스킵

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
    // =====================================================================
}
