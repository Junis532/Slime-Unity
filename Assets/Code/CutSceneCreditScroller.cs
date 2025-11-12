using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class CutSceneCreditScroller : MonoBehaviour
{
    public enum ScrollDirection { Up, Down }

    [Header("스크롤 대상 (세로 나열 루트)")]
    public RectTransform content;

    [Header("방향/속도")]
    public ScrollDirection direction = ScrollDirection.Up;
    public float baseSpeed = 60f;       // px/sec
    public float holdMultiplier = 3f;   // 꾹 누르면 배속

    [Header("입력(길게 눌렀을 때만 가속)")]
    public float holdThreshold = 0.25f; // 초

    [Header("시작/종료 위치 (Anchored Y)")]
    public float startY = -800f;
    public float endY = 1200f;

    [Header("옵션")]
    public float startDelay = 0.8f;
    public bool useUnscaledTime = false;

    [Header("끝난 뒤 씬 이동")]
    public bool loadSceneOnFinish = true;
    [Tooltip("이름 또는 빌드 인덱스 중 하나만 사용")]
    public string nextSceneName = "";       // 예: "ResultScene"
    public int nextSceneBuildIndex = -1;    // 예: 5 (Build Settings 순서)
    public LoadSceneMode loadMode = LoadSceneMode.Single;
    public float finishDelay = 0.5f;        // 스크롤 끝난 뒤 잠깐 쉬고 전환

    [Header("완료 이벤트(선택)")]
    public UnityEvent onFinished;

    float _holdTimer;
    bool _running;
    Vector2 _pos;

    void OnEnable()
    {
        if (!content)
        {
            Debug.LogWarning("[CutsceneCreditsScroller] content가 비어있습니다.");
            enabled = false;
            return;
        }

        _pos = content.anchoredPosition;
        _pos.y = startY;
        content.anchoredPosition = _pos;

        _holdTimer = 0f;
        _running = true;
    }

    void Update()
    {
        if (!_running) return;

        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        // 시작 지연
        if (startDelay > 0f)
        {
            startDelay -= dt;
            return;
        }

        // 길게 누름 판정
        bool pressed = IsPressedNow();
        if (pressed) _holdTimer += dt;
        else _holdTimer = 0f;

        float speed = baseSpeed * ((pressed && _holdTimer >= holdThreshold) ? holdMultiplier : 1f);

        _pos = content.anchoredPosition;

        if (direction == ScrollDirection.Up)
        {
            _pos.y += speed * dt;
            if (_pos.y >= endY || Mathf.Approximately(_pos.y, endY))
            {
                _pos.y = endY;
                content.anchoredPosition = _pos;
                Finish();
                return;
            }
        }
        else // Down
        {
            _pos.y -= speed * dt;
            if (_pos.y <= endY || Mathf.Approximately(_pos.y, endY))
            {
                _pos.y = endY;
                content.anchoredPosition = _pos;
                Finish();
                return;
            }
        }

        content.anchoredPosition = _pos;
    }

    void Finish()
    {
        if (!_running) return;
        _running = false;
        onFinished?.Invoke();
        if (loadSceneOnFinish)
            StartCoroutine(CoLoadNextScene());
    }

    System.Collections.IEnumerator CoLoadNextScene()
    {
        if (finishDelay > 0f)
        {
            if (useUnscaledTime) yield return new WaitForSecondsRealtime(finishDelay);
            else yield return new WaitForSeconds(finishDelay);
        }

        // 이름이 우선, 비어있으면 빌드 인덱스 사용
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName, loadMode);
        }
        else if (nextSceneBuildIndex >= 0)
        {
            SceneManager.LoadScene(nextSceneBuildIndex, loadMode);
        }
        else
        {
            Debug.LogWarning("[CutsceneCreditsScroller] 다음 씬 지정이 없어 전환을 건너뜁니다.");
        }
    }

    bool IsPressedNow()
    {
        // UI 위 터치/클릭 무시하려면 아래 주석 해제
        // if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return false;

#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButton(0)) return true;
#endif
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; ++i)
            {
                var t = Input.GetTouch(i);
                if (t.phase == TouchPhase.Stationary || t.phase == TouchPhase.Moved)
                    return true;
            }
        }
        return false;
    }

#if UNITY_EDITOR
    [ContextMenu("Suggest Up Start/End (parent-based)")]
    void SuggestUp()
    {
        if (!content || !content.parent) return;
        var parentRT = content.parent as RectTransform;
        if (!parentRT) return;

        float viewH = Mathf.Abs(parentRT.rect.height);
        float contH = Mathf.Abs(content.rect.height);

        startY = -viewH - 50f;   // 화면 아래에서 시작
        endY = contH + 50f;    // 콘텐츠 끝 위로 조금 더
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
