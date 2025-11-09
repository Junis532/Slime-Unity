using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossHPBarView : MonoBehaviour, IBossHPView
{
    // ========= 기본 UI 참조 =========
    [Header("바 이미지")]
    [SerializeField] private Image fill;          // 실제 HP
    [SerializeField] private Image delayedFill;   // 흰 잔상(선택)
    [Header("라벨(선택)")]
    [SerializeField] private TMP_Text hpText;

    // ========= 등장 연출 옵션 =========
    public enum IntroStyle { SlideFromRight, SlideFromTop, ScalePop, FadeOnly }
    [Header("등장 연출")]
    [SerializeField] private IntroStyle introStyle = IntroStyle.SlideFromRight;
    [SerializeField] private float introDuration = 0.45f;
    [SerializeField] private float introFade = 0.20f;
    [SerializeField] private Vector2 slideOffset = new Vector2(420f, 0f); // Slide 시작 오프셋(px)
    [SerializeField] private float popOvershoot = 1.12f; // ScalePop에서 첫 스케일
    [SerializeField] private Ease introEase = Ease.OutCubic;
    [SerializeField] private bool playOnShow = true;     // Show() 때 자동 재생

    // 프레임/테두리 플래시(격겜 느낌)
    [Header("프레임 플래시(선택)")]
    [SerializeField] private Graphic[] frameGraphics;      // 테두리/프레임 이미지들
    [SerializeField] private Color frameFlashColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private float frameFlashTime = 0.12f;
    [SerializeField] private int frameFlashCount = 1;

    // ========= 바 트윈 옵션 =========
    [Header("바 트윈")]
    [SerializeField] private float fillTween = 0.12f;   // 메인바 빠르게
    [SerializeField] private float delayHold = 0.15f;   // 잔상 대기
    [SerializeField] private float delayTween = 0.35f;  // 잔상 따라오기

    [Header("페이드 옵션")]
    [SerializeField] private bool useCanvasGroupFade = true;
    [SerializeField] private float hideFade = 0.25f;

    // (선택) 사운드 훅
    [Header("사운드(선택)")]
    [SerializeField] private AudioClip introSfx;
    [SerializeField] private float sfxVolume = 0.8f;

    // 내부 상태
    private float _max = 1f;
    private float _cur = 1f;

    private CanvasGroup _cg;
    private RectTransform _rt;
    private Vector2 _originalAnchoredPos;
    private Vector3 _originalScale;
    private Sequence _introSeq;

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _originalAnchoredPos = _rt ? _rt.anchoredPosition : Vector2.zero;
        _originalScale = transform.localScale;

        if (useCanvasGroupFade)
        {
            _cg = GetComponent<CanvasGroup>();
            if (!_cg) _cg = gameObject.AddComponent<CanvasGroup>();
        }

        if (fill) fill.fillAmount = 1f;
        if (delayedFill) delayedFill.fillAmount = 1f;
        UpdateLabel();
    }

    // ========== IBossHPView ==========
    public void Init(float maxHP, float currentHP)
    {
        _max = Mathf.Max(1f, maxHP);
        _cur = Mathf.Clamp(currentHP, 0f, _max);

        float ratio = _cur / _max;
        if (fill) { fill.DOKill(); fill.fillAmount = ratio; }
        if (delayedFill) { delayedFill.DOKill(); delayedFill.fillAmount = ratio; }
        UpdateLabel();
    }

    public void SetHP(float currentHP, float maxHP)
    {
        float prev = _cur;
        _max = Mathf.Max(1f, maxHP);
        _cur = Mathf.Clamp(currentHP, 0f, _max);

        float target = _cur / _max;

        if (fill)
        {
            fill.DOKill();
            fill.DOFillAmount(target, fillTween).SetEase(Ease.OutCubic);
        }

        if (delayedFill)
        {
            delayedFill.DOKill();
            bool healing = _cur > prev;
            if (healing) delayedFill.fillAmount = target;
            else DOTween.Sequence()
                        .AppendInterval(delayHold)
                        .Append(delayedFill.DOFillAmount(target, delayTween).SetEase(Ease.OutQuad));
        }

        UpdateLabel();
    }

    public void Show()
    {
        gameObject.SetActive(true);

        if (useCanvasGroupFade && _cg) _cg.alpha = 0f;

        // 시작 상태 세팅
        if (_rt)
        {
            switch (introStyle)
            {
                case IntroStyle.SlideFromRight:
                    _rt.anchoredPosition = _originalAnchoredPos + slideOffset;
                    transform.localScale = _originalScale;
                    break;
                case IntroStyle.SlideFromTop:
                    _rt.anchoredPosition = _originalAnchoredPos + new Vector2(0f, Mathf.Abs(slideOffset.x) > 0.01f ? slideOffset.x : 220f);
                    transform.localScale = _originalScale;
                    break;
                case IntroStyle.ScalePop:
                    _rt.anchoredPosition = _originalAnchoredPos;
                    transform.localScale = _originalScale * popOvershoot;
                    break;
                case IntroStyle.FadeOnly:
                    _rt.anchoredPosition = _originalAnchoredPos;
                    transform.localScale = _originalScale;
                    break;
            }
        }

        if (playOnShow) PlayIntro();
        else if (useCanvasGroupFade && _cg) _cg.DOFade(1f, introFade);
    }

    public void Hide()
    {
        _introSeq?.Kill();
        if (useCanvasGroupFade && _cg)
        {
            _cg.DOKill();
            _cg.DOFade(0f, hideFade).OnComplete(() => gameObject.SetActive(false));
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    // ========== 연출 본체 ==========
    public void PlayIntro()
    {
        _introSeq?.Kill();
        _introSeq = DOTween.Sequence();

        // 사운드(선택)
        if (introSfx)
            AudioSource.PlayClipAtPoint(introSfx, Camera.main ? Camera.main.transform.position : transform.position, sfxVolume);

        // 프레임 플래시
        if (frameGraphics != null && frameGraphics.Length > 0 && frameFlashCount > 0 && frameFlashTime > 0f)
        {
            for (int i = 0; i < frameGraphics.Length; i++)
            {
                var g = frameGraphics[i];
                if (!g) continue;
                g.DOKill();
                // 현재 색 저장
                Color baseCol = g.color;
                // 짧은 플래시 N회
                _introSeq.Join(g.DOColor(frameFlashColor, frameFlashTime * 0.5f).SetLoops(2 * frameFlashCount, LoopType.Yoyo));
                // 끝날 때 원상복구
                _introSeq.OnComplete(() => { if (g) g.color = baseCol; });
            }
        }

        // 메인 애니메이션
        if (useCanvasGroupFade && _cg)
        {
            _cg.DOKill();
            _cg.alpha = 0f;
            _introSeq.Join(_cg.DOFade(1f, introFade));
        }

        if (_rt)
        {
            switch (introStyle)
            {
                case IntroStyle.SlideFromRight:
                    {
                        _introSeq.Join(_rt.DOAnchorPos(_originalAnchoredPos, introDuration).SetEase(introEase));
                        break;
                    }
                case IntroStyle.SlideFromTop:
                    {
                        _introSeq.Join(_rt.DOAnchorPos(_originalAnchoredPos, introDuration).SetEase(introEase));
                        break;
                    }
                case IntroStyle.ScalePop:
                    {
                        // 팝 → 과슈링크 → 정착
                        _introSeq.Join(transform.DOScale(_originalScale, introDuration * 0.7f).SetEase(Ease.OutBack));
                        break;
                    }
                case IntroStyle.FadeOnly:
                default:
                    // 추가 이동 없음
                    break;
            }

            // 공통으로 아주 살짝의 마무리 탄성(선호 시)
            _introSeq.AppendInterval(0.02f);
            if (introStyle == IntroStyle.SlideFromRight || introStyle == IntroStyle.SlideFromTop)
                _introSeq.Append(_rt.DOAnchorPos(_originalAnchoredPos + new Vector2(0f, 0f), 0.05f)); // 자리 고정(의미상)
        }
    }

    private void UpdateLabel()
    {
        if (hpText)
            hpText.text = $"{Mathf.CeilToInt(_cur)}/{Mathf.CeilToInt(_max)}";
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (_rt == null) _rt = GetComponent<RectTransform>();
        if (_rt != null) _originalAnchoredPos = _rt.anchoredPosition;
        _originalScale = transform.localScale;
    }
#endif
}
