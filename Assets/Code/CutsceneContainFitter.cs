using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 컷씬 이미지를 기기 화면비에 맞춰 '비율 유지'로 맞춰준다.
/// - FitMode.Contain : 전체를 보이게(레터/필러박스)
/// - FitMode.Cover   : 화면을 꽉 채우게(일부 크롭)
/// - Safe Area 고려 가능
/// </summary>
[RequireComponent(typeof(Image))]
public class CutsceneContainFitter : MonoBehaviour
{
    public enum FitMode { Contain, Cover }

    [Header("기본 설정")]
    public FitMode fitMode = FitMode.Contain;
    public RectTransform safeAreaParent;  // null이면 부모 RectTransform 사용
    public bool fitOnEnable = true;

    [Header("레터박스 배경")]
    public bool useLetterBoxBackground = true;
    public Color letterBoxColor = Color.black;

    [Header("안전영역 처리")]
    public bool useSafeArea = false; // true면 SafeArea를 기준으로 맞춤

    private Image _image;
    private RectTransform _rt;
    private GameObject _bgBox;

    void Awake()
    {
        _image = GetComponent<Image>();
        _rt = GetComponent<RectTransform>();
        _image.preserveAspect = true;

        if (useLetterBoxBackground)
        {
            _bgBox = new GameObject("LetterBoxBG", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var bgRt = _bgBox.GetComponent<RectTransform>();
            _bgBox.transform.SetParent((safeAreaParent ? safeAreaParent : _rt.parent), false);
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.anchoredPosition = Vector2.zero; bgRt.sizeDelta = Vector2.zero;
            _bgBox.GetComponent<Image>().color = letterBoxColor;
            _bgBox.transform.SetAsFirstSibling(); // 배경 뒤로
        }
    }

    void OnEnable()
    {
        if (fitOnEnable) Fit();
    }

    void OnRectTransformDimensionsChange()
    {
        // 화면 회전/리사이즈 시 자동 보정
        Fit();
    }

    public void Fit()
    {
        if (_image == null || _image.sprite == null) return;

        // 기준 사각형 선택
        RectTransform parent = safeAreaParent ? safeAreaParent : (RectTransform)_rt.parent;
        Vector2 baseSize = parent.rect.size;

        if (useSafeArea)
        {
            // SafeArea를 캔버스 좌표로 환산
            var sa = Screen.safeArea;
            var scale = baseSize.x / Screen.width; // UI 픽셀→캔버스 좌표 스케일 대략치
            baseSize = new Vector2(sa.width * scale, sa.height * scale);
            if (baseSize.x <= 0 || baseSize.y <= 0) baseSize = parent.rect.size;
        }

        // 스프라이트 및 박스 비율
        var texSize = _image.sprite.rect.size;
        float spriteAspect = texSize.x / texSize.y;
        float boxAspect = baseSize.x / baseSize.y;

        Vector2 size;

        if (fitMode == FitMode.Contain)
        {
            // 전체가 보이도록
            if (spriteAspect > boxAspect)
                size = new Vector2(baseSize.x, baseSize.x / spriteAspect);
            else
                size = new Vector2(baseSize.y * spriteAspect, baseSize.y);
        }
        else
        {
            // 화면을 꽉 채우도록(일부 크롭)
            if (spriteAspect < boxAspect)
                size = new Vector2(baseSize.x, baseSize.x / spriteAspect);
            else
                size = new Vector2(baseSize.y * spriteAspect, baseSize.y);
        }

        // 적용(센터 기준)
        _rt.anchorMin = new Vector2(0.5f, 0.5f);
        _rt.anchorMax = new Vector2(0.5f, 0.5f);
        _rt.pivot = new Vector2(0.5f, 0.5f);
        _rt.sizeDelta = size;
        _rt.anchoredPosition = Vector2.zero;

        // 배경은 부모 꽉 채우도록 유지
        if (_bgBox)
        {
            var bgRt = _bgBox.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.anchoredPosition = Vector2.zero; bgRt.sizeDelta = Vector2.zero;
        }
    }
}
