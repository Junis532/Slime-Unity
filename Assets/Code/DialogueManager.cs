using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine.EventSystems; // ⬅ UI 터치 차단용(필요시)

public class DialogueManager : MonoBehaviour
{
    [Header("출력해줄 다이얼로그 말풍선 스프라이트")]
    public GameObject dialogueBox;
    [Header("텍스트")]
    public TextMeshProUGUI usedText;
    [Header("대화할 데이터")]
    public TalkData NPCTalkDatable;
    bool isTalk = false; //대화 다 했는가?
    [Header("플레이어 태그")]
    public string usedTag = "";
    [Header("다이얼로그 캔버스")]
    public GameObject usedCanvas;
    GameObject dialgoueSet;
    GameObject dialTextSet;
    TextMeshProUGUI usedMesh;

    [Header("시네머신 (있으면 사용, 없으면 런타임 생성)")]
    public CinemachineCamera cinemachineCamera;   // 인스펙터에 지정 안 해도 됨
    [Header("플레이어")]
    public GameObject playerSet;
    public GameObject[] temp;

    int currentLine = 0; // 현재 대화 인덱스
    [Header("타이핑 속도 (글자당 딜레이)")]
    public float typingSpeed = 0.05f;

    // ==== 카메라 복구용 ====
    float _origOrthoSize = -1f;
    int _origPriority = 0;
    bool _origEnabled = true;

    // 대화 전용 VCam을 만들었는지 여부
    bool _createdRuntimeVCam = false;

    // 대화 중 보정(보수) 추적 코루틴
    Coroutine _hardTrackRoutine;

    // ─────────────────────────────────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag(usedTag) && !isTalk)
        {
            isTalk = true;
            DialogueStart();
        }
    }

    // 필요한 최소 구성 보장:
    // - Main Camera에 CinemachineBrain
    // - 대화용 VCam 존재(없으면 새로 생성)
    // - VCam에 Position Composer(Body)
    void EnsureCameraRig()
    {
        // 1) 메인 카메라 + 브레인 보장
        var mainCam = Camera.main;
        if (mainCam == null)
        {
            var go = new GameObject("Main Camera");
            mainCam = go.AddComponent<Camera>();
            go.tag = "MainCamera";
        }
        if (mainCam.GetComponent<CinemachineBrain>() == null)
            mainCam.gameObject.AddComponent<CinemachineBrain>();

        // 2) VCam 없으면 생성(루트에)
        if (cinemachineCamera == null)
        {
            var vgo = new GameObject("DialogueVCam");
            cinemachineCamera = vgo.AddComponent<CinemachineCamera>();
            _createdRuntimeVCam = true;
        }

        // 3) VCam 레이어가 UI면 꺼지므로 Default로
        cinemachineCamera.gameObject.layer = LayerMask.NameToLayer("Default");

        // 4) Body(위치 구동) 보장
        if (!cinemachineCamera.TryGetComponent<CinemachinePositionComposer>(out _))
            cinemachineCamera.gameObject.AddComponent<CinemachinePositionComposer>();
    }

    // ─────────────────────────────────────────────────────────────────────────────

    void DialogueStart()
    {
        // (기존 UI 끄기 로직)
        temp = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject t in temp)
            if (t.layer == LayerMask.NameToLayer("UI"))
                t.gameObject.SetActive(false);

        // === 카메라 파트 시작 : “확실히” 동작하도록 구성 ===
        EnsureCameraRig();

        // 원본 값 백업
        if (_origOrthoSize < 0f) _origOrthoSize = cinemachineCamera.Lens.OrthographicSize;
        _origPriority = cinemachineCamera.Priority;
        _origEnabled = cinemachineCamera.enabled;

        // 이 VCam을 라이브로 보장
        cinemachineCamera.enabled = true;
        cinemachineCamera.Priority = 10000;

        // CM3 방식: TrackingTarget만 바꾸면 Body가 따라감
        var ct = cinemachineCamera.Target;  // 구조체 복사
        ct.TrackingTarget = transform;      // NPC
        cinemachineCamera.Target = ct;      // 다시 대입

        // 즉시 재계산
        cinemachineCamera.PreviousStateIsValid = false;

        // 줌 인
        cinemachineCamera.Lens.OrthographicSize = 3f;

        // 보수 추적(혹시라도 Body/채널 세팅 이슈가 남아있을 때 대비)
        _hardTrackRoutine = StartCoroutine(HardTrackRoutine(transform));
        // === 카메라 파트 끝 ===

        // (이하 기존 UI 생성/애니메이션)
        dialgoueSet = Instantiate(dialogueBox);
        dialgoueSet.transform.position = transform.position;
        dialTextSet = Instantiate(usedText.gameObject);
        dialTextSet.transform.position = transform.position;

        dialTextSet.GetComponent<TextMeshProUGUI>().text = "";

        dialgoueSet.transform.parent = usedCanvas.transform;
        dialTextSet.transform.parent = usedCanvas.transform;

        dialTextSet.GetComponent<TextMeshProUGUI>().DOFade(1f, 0.5f);
        dialTextSet.transform.DOMoveY(4.5f, 0.5f);
        dialgoueSet.GetComponent<Image>().DOFade(1f, 0.5f);
        dialgoueSet.transform.DOMoveY(4.5f, 0.5f);
        usedMesh = dialTextSet.GetComponent<TextMeshProUGUI>();

        StartCoroutine(DialogueRoutine());
    }

    // “혹시라도” CM 파이프라인이 안 움직일 때를 위해,
    // 대화 중엔 VCam Transform을 타깃 XY로 강제 스냅(메인카메라 Z 유지)
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
            yield return null; // 매 프레임
        }
    }

    IEnumerator DialogueRoutine()
    {
        while (currentLine < NPCTalkDatable.talks.Count)
        {
            yield return StartCoroutine(DialTyping(NPCTalkDatable.talks[currentLine].talkString));
            currentLine++;

            // ▼▼▼ 스페이스바 → 터치/클릭(모바일/PC 겸용)로 변경 ▼▼▼
            yield return new WaitUntil(AdvanceTapped);
            // ▲▲▲

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
            rt.sizeDelta = new Vector2(rt.sizeDelta.x + 40f, rt.sizeDelta.y);
            yield return new WaitForSeconds(typingSpeed);
        }
    }

    void EndDialogue()
    {
        Debug.Log("다이얼로그 종료");
        // 말풍선 및 텍스트 제거
        Destroy(dialgoueSet);
        Destroy(dialTextSet);
        isTalk = false;
        currentLine = 0;

        // === 카메라 복귀 ===
        if (_hardTrackRoutine != null)
        {
            StopCoroutine(_hardTrackRoutine);
            _hardTrackRoutine = null;
        }

        if (cinemachineCamera != null)
        {
            // 플레이어로 타깃 되돌리기
            var ct = cinemachineCamera.Target;
            ct.TrackingTarget = playerSet != null ? playerSet.transform : null;
            cinemachineCamera.Target = ct;

            cinemachineCamera.PreviousStateIsValid = false;

            // 원래 세팅 복구
            cinemachineCamera.Lens.OrthographicSize = (_origOrthoSize > 0f) ? _origOrthoSize : 5.6f;
            cinemachineCamera.Priority = _origPriority;
            cinemachineCamera.enabled = _origEnabled;

            // 런타임에 만든 VCam은 정리(원한다면 남겨도 무방)
            if (_createdRuntimeVCam)
            {
                Destroy(cinemachineCamera.gameObject);
                cinemachineCamera = null;
                _createdRuntimeVCam = false;
            }
        }

        foreach (GameObject t in temp)
        {
            if (t != null)
            {
                if (t.layer == LayerMask.NameToLayer("UI"))
                    t.gameObject.SetActive(true);
            }
        }
        // ===============
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 입력 유틸: 모바일 터치/PC 클릭/스페이스바 중 하나라도 '이번 프레임'에 발생하면 true
    bool AdvanceTapped()
    {
        // 1) 키보드(PC 테스트 편의)
        if (Input.GetKeyDown(KeyCode.Space)) return true;

        // 2) 마우스 클릭(에디터/PC)
        if (Input.GetMouseButtonDown(0))
        {
            // ※ UI 위 클릭도 허용하려면 아래 if 블록을 주석 처리하세요.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return true; // UI 클릭도 통과(허용)
            return true;     // 화면 아무 곳이나 클릭
        }

        // 3) 모바일 터치
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                if (t.phase == TouchPhase.Began)
                {
                    // ※ UI 위 터치도 허용하려면 fingerId 검사 부분을 제거/변경하세요.
                    if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.fingerId))
                        return true; // UI 터치도 통과(허용)
                    return true;     // 화면 아무 곳이나 터치
                }
            }
        }

        return false;
    }
}
