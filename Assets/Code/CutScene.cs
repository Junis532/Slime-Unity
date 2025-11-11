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

    // 내부
    private readonly List<Image> _blurGhosts = new List<Image>();

    void Start()
    {
        if (usedMessage != null)
        {
            usedMessage.gameObject.SetActive(true);
            var c = usedMessage.color; c.a = 0f; usedMessage.color = c;
            usedMessage.text = string.Empty;
            usedMessage.maxVisibleCharacters = 0;
        }
        if (fadeBlack != null)
        {
            var fc = fadeBlack.color; fc.a = 1f; fadeBlack.color = fc;
        }
        if (autoSetupEyelidsOnly) EnsureEyelidsExist();

        StartCoroutine(cutSceneStart());
    }

    public IEnumerator cutSceneStart()
    {
        bool isMobile = Application.isMobilePlatform;
        bool allowClickSkip = !isMobile; // PC/에디터에서 클릭으로 스킵

        // 모바일은 자동 스킵이므로 바로 투명
        if (isMobile && fadeBlack != null)
        {
            var fc = fadeBlack.color; fc.a = 0f; fadeBlack.color = fc;
        }

        // 컷씬 루프
        for (int i = 0; i < cutScene.Count; i++)
        {
            if (usedCutImage) usedCutImage.sprite = cutScene[i];

            if (!isMobile)
            {
                // 페이드아웃(보이기) 1초
                yield return DOFadeSkippable(fadeBlack, 0f, 1f, allowClickSkip);
                // 유지 3초
                yield return DelaySkippable(3f, allowClickSkip);
                // 페이드인(가리기) 1초
                yield return DOFadeSkippable(fadeBlack, 1f, 1f, allowClickSkip);
                // 대기 2초
                yield return DelaySkippable(2f, allowClickSkip);
            }
            else
            {
                // 모바일: 즉시 다음 스프라이트로
                yield return null; // 한 프레임 갱신
            }
        }

        // 메시지 타이핑(스킵 없음)
        yield return new WaitForSeconds(afterCutDelay);

        if (usedMessage != null)
        {
            usedMessage.DOFade(1f, 0.25f);
            yield return StartCoroutine(TypeText(finalLine, typeSpeed));
            yield return new WaitForSeconds(keepAfterTyping);
            yield return StartCoroutine(BackspaceDelete(backspaceSpeed));
            usedMessage.DOFade(0f, 0.2f);
        }

        // 마지막은 스킵 금지: 눈 떠지는 연출
        yield return new WaitForSeconds(eyeOpenDelay);
        yield return StartCoroutine(EyeOpenReveal(awakeningSprite));

        yield return new WaitForSeconds(3f);
        // 화면 페이드아웃 후 씬 전환
        yield return StartCoroutine(FadeOutAndLoadScene("InGame", 1f));
    }

    // ----- 스킵 가능한 유틸 -----
    private IEnumerator DOFadeSkippable(Image img, float to, float duration, bool allowSkip)
    {
        if (img == null)
        {
            yield break;
        }
        img.DOKill();
        if (duration <= 0f || !img.gameObject.activeInHierarchy)
        {
            var c = img.color; c.a = to; img.color = c;
            yield break;
        }

        Tween tw = img.DOFade(to, duration);
        while (tw.IsActive() && tw.IsPlaying())
        {
            if (allowSkip && IsSkipInput())
            {
                tw.Complete(); // 즉시 목표값
                break;
            }
            yield return null;
        }
    }

    private IEnumerator DelaySkippable(float seconds, bool allowSkip)
    {
        float t = 0f;
        while (t < seconds)
        {
            if (allowSkip && IsSkipInput()) break;
            t += Time.deltaTime;
            yield return null;
        }
    }

    private bool IsSkipInput()
    {
        // PC/에디터용 스킵 입력
        return Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space);
    }

    // ----- 타이핑 & 백스페이스 -----
    private IEnumerator TypeText(string msg, float perCharDelay)
    {
        usedMessage.text = msg;
        usedMessage.ForceMeshUpdate();
        int total = usedMessage.textInfo.characterCount;
        usedMessage.maxVisibleCharacters = 0;

        for (int i = 1; i <= total; i++)
        {
            usedMessage.maxVisibleCharacters = i;
            yield return new WaitForSeconds(perCharDelay);
        }
    }

    private IEnumerator BackspaceDelete(float perCharDelay)
    {
        usedMessage.ForceMeshUpdate();
        int total = usedMessage.textInfo.characterCount;

        for (int i = total; i >= 0; i--)
        {
            usedMessage.maxVisibleCharacters = i;
            yield return new WaitForSeconds(perCharDelay);
        }
        usedMessage.text = string.Empty;
        usedMessage.maxVisibleCharacters = 0;
    }

    // ----- 눈꺼풀 & 포커스 스냅 -----
    private IEnumerator EyeOpenReveal(Sprite newSprite)
    {
        if (newSprite && usedCutImage) usedCutImage.sprite = newSprite;

        if (fadeBlack) fadeBlack.DOFade(0f, eyeOpenDuration * 0.8f);

        if (topLid && bottomLid)
        {
            var canvas = GetTargetCanvas();
            if (canvas != null)
            {
                PrepareEyelidsClosedGeometry();
                topLid.gameObject.SetActive(true);
                bottomLid.gameObject.SetActive(true);
                topLid.transform.SetAsLastSibling();
                bottomLid.transform.SetAsLastSibling();

                var canvasRect = canvas.GetComponent<RectTransform>().rect;
                float moveY = (canvasRect.height * 0.5f) + lidExtraMargin;

                var topRT = topLid.rectTransform;
                var botRT = bottomLid.rectTransform;

                topRT.DOKill(); botRT.DOKill();
                var ease = eyeOpenOvershoot > 0f ? Ease.OutBack : Ease.OutCubic;

                var t1 = topRT.DOAnchorPosY(moveY, eyeOpenDuration).SetEase(ease);
                if (eyeOpenOvershoot > 0f) t1.SetEase(Ease.OutBack, eyeOpenOvershoot);

                var t2 = botRT.DOAnchorPosY(-moveY, eyeOpenDuration).SetEase(ease);
                if (eyeOpenOvershoot > 0f) t2.SetEase(Ease.OutBack, eyeOpenOvershoot);

                yield return t2.WaitForCompletion();

                topLid.gameObject.SetActive(false);
                bottomLid.gameObject.SetActive(false);
            }
        }
        else
        {
            if (fadeBlack) yield return fadeBlack.DOFade(0f, eyeOpenDuration).WaitForCompletion();
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

        var seq = DOTween.Sequence();
        seq.Join(mainRT.DOScale(1f, focusDuration).SetEase(Ease.OutQuad));
        foreach (var g in _blurGhosts)
        {
            var rt = g.rectTransform;
            g.DOKill();
            seq.Join(rt.DOAnchorPos(Vector2.zero, focusDuration).SetEase(Ease.OutCubic));
            seq.Join(g.DOFade(0f, focusDuration).SetEase(Ease.OutCubic));
        }

        yield return seq.WaitForCompletion();
        ClearBlurGhosts();
    }

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

    // ----- 눈꺼풀 유틸 -----
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
        // fadeBlack이 없으면 생성
        if (fadeBlack == null)
        {
            var canvas = GetTargetCanvas();
            if (canvas != null)
            {
                fadeBlack = CreateFullScreenImage("FadeBlack", canvas.transform, Color.black);
            }
        }

        fadeBlack.gameObject.SetActive(true);
        fadeBlack.DOKill();

        // 알파 0 → 1로 페이드
        yield return fadeBlack.DOFade(1f, duration).WaitForCompletion();

        // 씬 로드
        LoadingManager.LoadScene(sceneName);
    }
}
