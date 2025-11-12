using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SideSpriteSlideshow : MonoBehaviour
{
    [Header("보여줄 스프라이트들")]
    public List<Sprite> slides = new List<Sprite>();

    [Header("타이밍")]
    [Tooltip("한 장이 완전히 보이는 유지 시간(초)")]
    public float durationPerSlide = 2.5f;
    [Tooltip("크로스 페이드 시간(초)")]
    public float fadeDuration = 0.6f;
    public bool loop = true;
    public bool randomOrder = false;
    public bool playOnEnable = true;
    public bool useUnscaledTime = false;

    [Header("이미지 옵션")]
    public bool preserveAspect = true;
    [Tooltip("부모 GO에 Image가 있다면 자동으로 숨깁니다(겹침 방지).")]
    public bool autoHideParentImage = true;

    // 내부
    RectTransform _root;
    Image _frontImg, _backImg;
    CanvasGroup _frontCG, _backCG;
    int _index = 0;
    bool _running = false;

    void Awake()
    {
        _root = transform as RectTransform ?? gameObject.AddComponent<RectTransform>();
        HideParentImageIfNeeded();
        EnsureLayers();
    }

    void OnEnable()
    {
        if (playOnEnable) Play();
    }

    void OnDisable()
    {
        Stop();
    }

    // ===== 외부 제어 =====
    public void Play()
    {
        _running = true;
        StopAllCoroutines();

        if (slides == null || slides.Count == 0)
        {
            // 아무것도 없으면 전부 숨김
            if (_frontCG) _frontCG.alpha = 0f;
            if (_backCG) _backCG.alpha = 0f;
            return;
        }

        // 초기 상태: 첫 장을 front에 바로 보여줌
        _frontImg.sprite = slides[Mathf.Clamp(_index, 0, slides.Count - 1)];
        _frontImg.preserveAspect = preserveAspect;
        _frontCG.alpha = 1f;

        _backCG.alpha = 0f;
        _backImg.sprite = null;

        StartCoroutine(CoPlay());
    }

    public void Stop()
    {
        _running = false;
        StopAllCoroutines();
    }

    public void SetSlides(List<Sprite> newSlides, bool restart = true)
    {
        slides = newSlides ?? new List<Sprite>();
        _index = 0;
        if (restart) Play();
    }

    public void Next() { _index = NextIndex(_index); }
    public void Prev()
    {
        if (slides == null || slides.Count == 0) return;
        _index = (_index - 1 + slides.Count) % slides.Count;
    }

    // ===== 메인 루프 =====
    IEnumerator CoPlay()
    {
        // 슬라이드가 1장뿐이면 그냥 유지
        if (slides.Count <= 1)
        {
            while (_running) yield return null;
            yield break;
        }

        while (_running)
        {
            // 유지
            yield return Wait(durationPerSlide);
            if (!_running) yield break;

            // 다음 장 준비
            int next = NextIndex(_index);
            _backImg.sprite = slides[next];
            _backImg.preserveAspect = preserveAspect;

            // 크로스 페이드
            yield return CrossFade(_frontCG, _backCG, fadeDuration);
            if (!_running) yield break;

            // 레이어 스왑
            Swap(ref _frontImg, ref _backImg);
            Swap(ref _frontCG, ref _backCG);

            _index = next;

            if (!loop && _index == slides.Count - 1)
            {
                _running = false;
                yield break;
            }
        }
    }

    // ===== 유틸 =====
    int NextIndex(int cur)
    {
        if (slides == null || slides.Count == 0) return 0;
        if (slides.Count == 1) return 0;

        if (randomOrder)
        {
            int r;
            do { r = Random.Range(0, slides.Count); } while (r == cur);
            return r;
        }
        return (cur + 1) % slides.Count;
    }

    IEnumerator CrossFade(CanvasGroup from, CanvasGroup to, float sec)
    {
        if (sec <= 0f)
        {
            from.alpha = 0f; to.alpha = 1f; yield break;
        }
        float t = 0f;
        while (t < sec)
        {
            t += Delta();
            float a = Mathf.Clamp01(t / sec);
            from.alpha = 1f - a;
            to.alpha = a;
            yield return null;
        }
        from.alpha = 0f;
        to.alpha = 1f;
    }

    IEnumerator Wait(float sec)
    {
        if (sec <= 0f) yield break;
        if (useUnscaledTime) yield return new WaitForSecondsRealtime(sec);
        else yield return new WaitForSeconds(sec);
    }

    float Delta() => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

    void EnsureLayers()
    {
        if (_frontImg != null && _backImg != null) return;

        CreateLayer("_Back", out _backImg, out _backCG);
        CreateLayer("_Front", out _frontImg, out _frontCG);

        // 정렬: Back 아래, Front 위
        _backImg.transform.SetAsFirstSibling();
        _frontImg.transform.SetAsLastSibling();

        // 초기 알파
        _backCG.alpha = 0f;
        _frontCG.alpha = 0f;
    }

    void CreateLayer(string name, out Image img, out CanvasGroup cg)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(_root, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        img = go.GetComponent<Image>();
        img.color = Color.white;
        img.preserveAspect = preserveAspect;
        img.raycastTarget = false;

        cg = go.GetComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
    }

    void HideParentImageIfNeeded()
    {
        if (!autoHideParentImage) return;
        var parentImg = GetComponent<Image>();
        if (parentImg)
        {
            var c = parentImg.color; c.a = 0f; parentImg.color = c;
            parentImg.raycastTarget = false;
        }
    }

    static void Swap<T>(ref T a, ref T b)
    {
        T t = a; a = b; b = t;
    }
}
