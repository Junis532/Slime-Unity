using DG.Tweening;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CutScene : MonoBehaviour
{
    [Header("컷씬 이미지들")]
    public List<Sprite> cutScene;
    public Image usedCutImage;
    public Image fadeBlack;

    [Header("메시지 타이핑")]
    public TextMeshProUGUI usedMessage;      // 이미 씬에 있는 TMP
    public string finalLine = "저기요 괜찮아요?";
    public float typeSpeed = 0.04f;           // 타이핑 속도(글자당)
    public float keepAfterTyping = 2f;        // 타이핑 후 유지 시간
    public float backspaceSpeed = 0.03f;      // 삭제 속도(글자당)

    [Header("눈 떠지는 연출")]
    public Sprite awakeningSprite;
    public Image topLid;                      // 비어 있으면 자동 생성
    public Image bottomLid;                   // 비어 있으면 자동 생성
    public bool autoSetupEyelidsOnly = true;
    public float afterCutDelay = 0.6f;        // 컷씬 종료 후 메시지 시작 전
    public float eyeOpenDelay = 0.4f;         // 메시지 삭제 후 눈뜨기까지
    public float eyeOpenDuration = 0.6f;
    public float eyeOpenOvershoot = 0.0f;
    public float lidExtraMargin = 30f;

    [Header("포커스 스냅(흐림→선명)")]
    public bool useFocusSnap = true;
    public int blurGhostCount = 8;
    public float blurRadius = 6f;
    public float focusDuration = 0.5f;
    public float startScale = 1.02f;

    [Header("고급 설정")]
    public bool useUnscaledTime = false;      // true면 게임 일시정지에도 연출 진행
    public bool allowSkip = true;             // 컷 사이 스킵 허용(마지막 구간 제외)
    public float cutFadeOut = 1.0f;           // 컷 등장(검정→투명)
    public float cutHold = 3.0f;              // 컷 유지
    public float cutFadeIn = 1.0f;            // 다음 컷 준비(투명→검정)
    public float cutBetween = 0.6f;           // 컷 사이 숨 고르기

    private readonly List<Image> _blurGhosts = new List<Image>();
    private CutsceneContainFitter _fitter;

    void Awake()
    {
        DOTween.Init(false, true, LogBehaviour.ErrorsOnly);
        DOTween.useSafeMode = true;
    }

    void Start()
    {
        if (usedMessage != null)
        {
            usedMessage.gameObject.SetActive(true);
            var c = usedMessage.color; c.a = 0f; usedMessage.color = c;
            usedMessage.text = string.Empty;
            usedMessage.maxVisibleCharacters = 0;
        }

        EnsureFadeBlackExists(setAlpha: 1f);

        if (autoSetupEyelidsOnly) EnsureEyelidsExist();

        if (usedCutImage)
        {
            var imgc = usedCutImage.color; imgc.a = 1f; usedCutImage.color = imgc;
            _fitter = usedCutImage.GetComponent<CutsceneContainFitter>();
        }

        StartCoroutine(cutSceneStart());
    }

    private void EnsureFadeBlackExists(float setAlpha = 1f)
    {
        if (fadeBlack == null)
        {
            var canvas = GetTargetCanvas();
            if (canvas != null)
                fadeBlack = CreateFullScreenImage("FadeBlack", canvas.transform, Color.black);
        }
        if (fadeBlack != null)
        {
            var fc = fadeBlack.color; fc.a = setAlpha; fadeBlack.color = fc;
            fadeBlack.gameObject.SetActive(true);
            fadeBlack.transform.SetAsLastSibling(); // 시작 시 최상단 가림막
        }
    }

    public IEnumerator cutSceneStart()
    {
        // 첫 컷 준비(검정 1로 가려진 상태에서 세팅)
        if (cutScene.Count > 0 && usedCutImage)
        {
            yield return SetImageSpriteAndSync(usedCutImage, cutScene[0]); // ✅ 강제 교체
            _fitter?.Fit();
        }
        // 보이기(검정→투명)
        yield return DOFade(fadeBlack, 0f, cutFadeOut);

        // 첫 컷 유지
        yield return DelaySkippable(cutHold, allowSkip);

        // 남은 컷 전환
        for (int i = 1; i < cutScene.Count; i++)
        {
            yield return TransitionToSprite(cutScene[i], cutFadeIn, cutFadeOut, allowSkip);
            yield return DelaySkippable(cutHold, allowSkip);
            if (cutBetween > 0f) yield return DelaySkippable(cutBetween, allowSkip);
        }

        // 텍스트 단계: 반드시 검정으로 닫고 진행
        yield return DOFade(fadeBlack, 1f, cutFadeIn); // 스킵 불가
        EnsureMessageOnTop();
        yield return WaitFor(afterCutDelay);

        if (usedMessage != null)
        {
            yield return FadeTMP(usedMessage, 1f, 0.25f);
            yield return StartCoroutine(TypeText(finalLine, typeSpeed));
            yield return WaitFor(keepAfterTyping);
            yield return StartCoroutine(BackspaceDelete(backspaceSpeed));
            yield return FadeTMP(usedMessage, 0f, 0.2f);
        }

        // 눈떠지기
        yield return WaitFor(eyeOpenDelay);
        yield return StartCoroutine(EyeOpenReveal(awakeningSprite));

        // 마무리
        yield return WaitFor(1.0f);
        yield return StartCoroutine(FadeOutAndLoadScene("InGame", 1f));
    }

    /// 투명→검정(스킵 가능) → 스프라이트 교체(스냅/동기화) → 검정→투명(스킵 가능)
    private IEnumerator TransitionToSprite(Sprite next, float fadeInToBlack, float fadeOutToClear, bool canSkip)
    {
        yield return DOFadeSkippable(fadeBlack, 1f, fadeInToBlack, canSkip); // 닫기

        if (usedCutImage)
        {
            yield return SetImageSpriteAndSync(usedCutImage, next);          // ✅ 강제 교체
            _fitter?.Fit();
            Canvas.ForceUpdateCanvases();
            yield return null; // 1프레임 동기화
        }

        yield return DOFadeSkippable(fadeBlack, 0f, fadeOutToClear, canSkip); // 열기
    }

    // ====== 🔴 스프라이트 강제 교체 유틸(누락 금지!) ======
    private void SetImageSprite(Image img, Sprite s)
    {
        if (img == null || s == null) return;

        img.DOKill();                 // 진행 중 트윈 제거
        bool wasEnabled = img.enabled;
        img.enabled = false;          // 잠깐 끄고

        // 캐시 무효화 → 새 스프라이트 지정
        img.overrideSprite = null;
        img.sprite = null;
        img.overrideSprite = s;
        img.sprite = s;
        img.preserveAspect = true;

        // 즉시 리빌드
        img.SetVerticesDirty();
        img.SetMaterialDirty();
        img.enabled = wasEnabled;

        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(img.rectTransform);
        Canvas.ForceUpdateCanvases();
    }

    private IEnumerator SetImageSpriteAndSync(Image img, Sprite s)
    {
        SetImageSprite(img, s);
        yield return null; // 1프레임 대기해 캔버스/메시 갱신 보장
    }
    // =====================================================

    // ---------- 페이드/대기 유틸 ----------
    private IEnumerator DOFade(Image img, float to, float duration)
    {
        if (img == null) yield break;
        img.DOKill();
        if (duration <= 0f || !img.gameObject.activeInHierarchy)
        {
            var c = img.color; c.a = to; img.color = c;
            yield break;
        }
        yield return img.DOFade(to, duration).SetUpdate(useUnscaledTime).WaitForCompletion();
    }

    private IEnumerator DOFadeSkippable(Image img, float to, float duration, bool canSkip)
    {
        if (img == null) yield break;

        img.DOKill();
        if (duration <= 0f || !img.gameObject.activeInHierarchy)
        {
            var c = img.color; c.a = to; img.color = c;
            yield break;
        }

        Tween tw = img.DOFade(to, duration).SetUpdate(useUnscaledTime);
        while (tw.IsActive() && tw.IsPlaying())
        {
            if (canSkip && IsSkipInput())
            {
                tw.Complete();
                break;
            }
            yield return null;
        }
    }

    private IEnumerator DelaySkippable(float seconds, bool canSkip)
    {
        float t = 0f;
        while (t < seconds)
        {
            if (canSkip && IsSkipInput()) break;
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator WaitFor(float seconds)
    {
        if (seconds <= 0f) yield break;
        if (useUnscaledTime) yield return new WaitForSecondsRealtime(seconds);
        else yield return new WaitForSeconds(seconds);
    }

    private bool IsSkipInput()
    {
        bool mouse = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);
        bool touch = false;
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
                if (Input.GetTouch(i).phase == TouchPhase.Began) { touch = true; break; }
        }
        return mouse || touch;
    }

    // ---------- 텍스트 ----------
    private IEnumerator TypeText(string msg, float perCharDelay)
    {
        usedMessage.text = msg;
        usedMessage.ForceMeshUpdate();
        int total = usedMessage.textInfo.characterCount;
        usedMessage.maxVisibleCharacters = 0;

        for (int i = 1; i <= total; i++)
        {
            usedMessage.maxVisibleCharacters = i;
            yield return WaitFor(perCharDelay);
        }
    }

    private IEnumerator BackspaceDelete(float perCharDelay)
    {
        usedMessage.ForceMeshUpdate();
        int total = usedMessage.textInfo.characterCount;

        for (int i = total; i >= 0; i--)
        {
            usedMessage.maxVisibleCharacters = i;
            yield return WaitFor(perCharDelay);
        }
        usedMessage.text = string.Empty;
        usedMessage.maxVisibleCharacters = 0;
    }

    private IEnumerator FadeTMP(TextMeshProUGUI tmp, float toAlpha, float duration)
    {
        if (tmp == null) yield break;
        tmp.DOKill();
        if (duration <= 0f)
        {
            var c = tmp.color; c.a = toAlpha; tmp.color = c;
            yield break;
        }
        yield return tmp.DOFade(toAlpha, duration).SetUpdate(useUnscaledTime).WaitForCompletion();
    }

    // ---------- 눈꺼풀/포커스 ----------
    private IEnumerator EyeOpenReveal(Sprite newSprite)
    {
        // 1) 새 스프라이트로 교체 (여전히 검정 1로 가려진 상태)
        if (newSprite && usedCutImage)
        {
            yield return SetImageSpriteAndSync(usedCutImage, newSprite); // ✅ 강제 교체
            _fitter?.Fit();
            Canvas.ForceUpdateCanvases();
            yield return null;
        }

        // 2) 눈꺼풀 준비(완전히 닫힘 상태로 최상단)
        var canvas = GetTargetCanvas();
        if (canvas && topLid && bottomLid)
        {
            PrepareEyelidsClosedGeometry();
            topLid.gameObject.SetActive(true);
            bottomLid.gameObject.SetActive(true);
            topLid.transform.SetAsLastSibling();
            bottomLid.transform.SetAsLastSibling();
        }

        // 3) 가림 역할을 눈꺼풀로 넘기고 블랙은 즉시 0
        if (fadeBlack)
        {
            fadeBlack.DOKill();
            var c = fadeBlack.color; c.a = 0f; fadeBlack.color = c; // 스냅
        }

        // 4) 눈꺼풀 열기
        if (canvas && topLid && bottomLid)
        {
            var canvasRect = canvas.GetComponent<RectTransform>().rect;
            float moveY = (canvasRect.height * 0.5f) + lidExtraMargin;

            var topRT = topLid.rectTransform;
            var botRT = bottomLid.rectTransform;

            topRT.DOKill(); botRT.DOKill();
            var ease = eyeOpenOvershoot > 0f ? Ease.OutBack : Ease.OutCubic;

            Tween t1 = topRT.DOAnchorPosY(moveY, eyeOpenDuration).SetEase(ease).SetUpdate(useUnscaledTime);
            if (eyeOpenOvershoot > 0f) t1.SetEase(Ease.OutBack, eyeOpenOvershoot);

            Tween t2 = botRT.DOAnchorPosY(-moveY, eyeOpenDuration).SetEase(ease).SetUpdate(useUnscaledTime);
            if (eyeOpenOvershoot > 0f) t2.SetEase(Ease.OutBack, eyeOpenOvershoot);

            yield return t2.WaitForCompletion();

            topLid.gameObject.SetActive(false);
            bottomLid.gameObject.SetActive(false);
        }
        else
        {
            // 눈꺼풀이 없을 때만 페이드로 열기
            if (fadeBlack) yield return fadeBlack.DOFade(0f, eyeOpenDuration).SetUpdate(useUnscaledTime).WaitForCompletion();
        }

        if (useFocusSnap && usedCutImage && newSprite)
            yield return StartCoroutine(PlayFocusSnap(newSprite));
    }

    private IEnumerator PlayFocusSnap(Sprite baseSprite)
    {
        var mainRT = usedCutImage.rectTransform;
        mainRT.DOKill();
        mainRT.localScale = Vector3.one * startScale;

        CreateBlurGhostsIfNeeded(baseSprite);
        foreach (var g in _blurGhosts)
            g.transform.SetSiblingIndex(usedCutImage.transform.GetSiblingIndex() + 1);

        var seq = DOTween.Sequence().SetUpdate(useUnscaledTime);
        seq.Join(mainRT.DOScale(1f, focusDuration).SetEase(Ease.OutQuad).SetUpdate(useUnscaledTime));
        foreach (var g in _blurGhosts)
        {
            var rt = g.rectTransform;
            g.DOKill();
            seq.Join(rt.DOAnchorPos(Vector2.zero, focusDuration).SetEase(Ease.OutCubic).SetUpdate(useUnscaledTime));
            seq.Join(g.DOFade(0f, focusDuration).SetEase(Ease.OutCubic).SetUpdate(useUnscaledTime));
        }

        yield return seq.WaitForCompletion();
        ClearBlurGhosts();
    }

    private void EnsureMessageOnTop()
    {
        if (usedMessage == null) return;
        var msgTrans = usedMessage.transform;
        msgTrans.SetAsLastSibling(); // 텍스트 최상단
        if (fadeBlack != null)
        {
            var parent = msgTrans.parent;
            if (parent == fadeBlack.transform.parent)
            {
                fadeBlack.transform.SetSiblingIndex(Mathf.Max(0, parent.childCount - 2));
                msgTrans.SetAsLastSibling();
            }
        }
    }

    // ---------- 유틸 ----------
    private void CreateBlurGhostsIfNeeded(Sprite baseSprite)
    {
        if (_blurGhosts.Count > 0) return;

        int n = Mathf.Max(3, blurGhostCount);
        var parent = usedCutImage.transform.parent;
        float alphaEach = 0.12f;

        for (int i = 0; i < n; i++)
        {
            var g = CreateFullScreenImage($"BlurGhost_{i}", parent, Color.white);
            g.sprite = baseSprite; g.preserveAspect = true; g.raycastTarget = false;

            float ang = (360f / n) * i * Mathf.Deg2Rad;
            Vector2 off = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * blurRadius;
            g.rectTransform.anchoredPosition = off;

            var c = g.color; c.a = alphaEach; g.color = c;
            g.transform.SetSiblingIndex(usedCutImage.transform.GetSiblingIndex() + 1);
            _blurGhosts.Add(g);
        }
    }

    private void ClearBlurGhosts()
    {
        foreach (var g in _blurGhosts)
            if (g) Destroy(g.gameObject);
        _blurGhosts.Clear();
    }

    private void EnsureEyelidsExist()
    {
        var canvas = GetTargetCanvas();
        if (canvas == null) return;

        if (topLid == null) topLid = CreateFullScreenImage("TopLid", canvas.transform, Color.black);
        if (bottomLid == null) bottomLid = CreateFullScreenImage("BottomLid", canvas.transform, Color.black);

        topLid.raycastTarget = false;
        bottomLid.raycastTarget = false;

        topLid.gameObject.SetActive(false);
        bottomLid.gameObject.SetActive(false);
    }

    private void PrepareEyelidsClosedGeometry()
    {
        if (!topLid || !bottomLid) return;

        var topRT = topLid.rectTransform;
        var botRT = bottomLid.rectTransform;

        topRT.anchorMin = new Vector2(0f, 0.5f);
        topRT.anchorMax = new Vector2(1f, 1f);
        topRT.pivot = new Vector2(0.5f, 1f);
        topRT.anchoredPosition = Vector2.zero;
        topRT.sizeDelta = Vector2.zero;

        botRT.anchorMin = new Vector2(0f, 0f);
        botRT.anchorMax = new Vector2(1f, 0.5f);
        botRT.pivot = new Vector2(0.5f, 0f);
        botRT.anchoredPosition = Vector2.zero;
        botRT.sizeDelta = Vector2.zero;
    }

    private Canvas GetTargetCanvas()
    {
        if (usedCutImage) return usedCutImage.canvas;
        if (fadeBlack) return fadeBlack.canvas;
        if (usedMessage) return usedMessage.canvas;
        return null;
    }

    private Image CreateFullScreenImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;

        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;

        return img;
    }

    private IEnumerator FadeOutAndLoadScene(string sceneName, float duration = 1f)
    {
        EnsureFadeBlackExists();
        fadeBlack.DOKill();
        yield return fadeBlack.DOFade(1f, duration).SetUpdate(useUnscaledTime).WaitForCompletion();
        LoadingManager.LoadScene(sceneName);
    }
}