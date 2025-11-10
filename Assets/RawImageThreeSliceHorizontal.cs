using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 부모 RawImage를 가로 3-슬라이스(Left/Center/Right)로 쪼개서
/// 부모 폭이 변해도 가운데만 늘어나게 만드는 래퍼.
/// - 원본 RawImage는 텍스처/색상만 들고 있게 하고, 이 컴포넌트가
///   내부에 3개의 RawImage 자식(L/C/R)을 생성해서 표시합니다.
/// - 기존 너비 조절 스크립트는 '부모 RectTransform'만 바꾸면 됩니다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RawImage))]
public class RawImageThreeSliceHorizontal : MonoBehaviour
{
    [Tooltip("왼쪽/오른쪽 캡의 폭(px). 예: 8이면 좌우 8px은 고정, 중앙만 늘어남")]
    public int capWidthPx = 8;

    [Tooltip("픽셀아트 깨짐 방지용. true면 중앙 조각을 '늘림' 대신 '타일'로 반복")]
    public bool centerTiled = false;

    [Tooltip("UV를 원본 전체에서 따오지 않고, uvRect를 그대로 따름")]
    public bool useRawImageUvRect = true;

    RawImage _src;          // 부모에 붙어있는 원본 RawImage
    RawImage _left, _center, _right;
    RectTransform _rt;

    void Awake()
    {
        _src = GetComponent<RawImage>();
        _rt = GetComponent<RectTransform>();

        // 자식 생성(없으면)
        if (transform.childCount < 3 || !_left || !_center || !_right)
            CreateChildren();

        SyncAll();
    }

    void OnEnable() => SyncAll();

    // 부모 크기가 바뀌면 자동 갱신
    protected void OnRectTransformDimensionsChange()
    {
        if (!isActiveAndEnabled) return;
        SyncLayout();
    }

    void CreateChildren()
    {
        // 기존 자식 싹 비우고 재생성해도 됨 (필요 시 보존 로직으로 바꿔도 OK)
        for (int i = transform.childCount - 1; i >= 0; --i)
            DestroyImmediate(transform.GetChild(i).gameObject);

        _left = CreatePiece("Left");
        _center = CreatePiece("Center");
        _right = CreatePiece("Right");

        // MaskableGraphic의 마우스 이벤트가 부모까지 통하도록
        _left.raycastTarget = _center.raycastTarget = _right.raycastTarget = _src.raycastTarget;
    }

    RawImage CreatePiece(string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
        go.transform.SetParent(transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 0.5f);
        var ri = go.GetComponent<RawImage>();
        ri.texture = _src.texture;
        ri.color = _src.color;
        ri.material = _src.material;
        return ri;
    }

    void SyncAll()
    {
        if (_src.texture == null) return;


        // 자식들 공통 속성 동기화
        _left.texture = _center.texture = _right.texture = _src.texture;
        _left.color = _center.color = _right.color = _src.color;
        _left.material = _center.material = _right.material = _src.material;

        // UV 영역 계산
        var texW = (float)_src.texture.width;
        var uv = useRawImageUvRect ? _src.uvRect : new Rect(0, 0, 1, 1);

        float capU = capWidthPx / texW;     // 텍스처 기준 좌/우 캡의 U폭(0~1)
        capU = Mathf.Clamp(capU, 0f, uv.width * 0.49f); // 과도 방지

        // 각 조각의 uvRect 설정
        // Left:  [uv.x, uv.x + capU]
        _left.uvRect = new Rect(uv.xMin, uv.yMin, capU, uv.height);

        // Center: (가운데 영역)
        // [uv.x + capU, uv.x + uv.width - capU]
        float centerU = Mathf.Max(uv.width - capU - capU, 0f);
        _center.uvRect = new Rect(uv.xMin + capU, uv.yMin, centerU, uv.height);

        // Right: [uv.x + uv.width - capU, uv.x + uv.width]
        _right.uvRect = new Rect(uv.xMax - capU, uv.yMin, capU, uv.height);

        // 중앙 타일링 옵션
        // RawImage는 타일링을 직접 제공하지 않아서, uvRect 폭을 늘려 반복되게 함
        // (Unity UI의 RawImage는 uvRect 폭>1이면 텍스처가 반복됨. 텍스처 Import에서 Wrap Mode=Repeat 필요)
        if (centerTiled)
        {
            _center.uvRect = new Rect(_center.uvRect.x, _center.uvRect.y, 1f, _center.uvRect.height);
        }

        // 레이아웃 정렬
        SyncLayout();

        // 부모 원본은 보이지 않게(데이터 홀더 역할만)
        _src.enabled = false;
    }

    void SyncLayout()
    {
        if (_src.texture == null) return;

        var tex = _src.texture;
        float texW = tex.width;
        float texH = tex.height;

        var height = _rt.rect.height;
        var width = _rt.rect.width;

        // 캡 폭(로컬 단위): 부모 높이에 맞춘 비율 스케일링
        // RawImage는 스프라이트처럼 PPU 개념이 없으니, 단순히 비율로 산출.
        // 원본 텍스처 비율을 유지하려면 세로 기준 스케일링.
        float pxToUnits = (height <= 0f || texH <= 0f) ? 0f : (height / texH);
        float capUnits = Mathf.Round(capWidthPx * pxToUnits); // 픽셀 아트면 반올림 추천

        // 최소 폭 보정(좌/우 캡만으로도 넘어가면 가운데=0)
        float centerWidth = Mathf.Max(0f, width - (capUnits * 2f));

        // Left
        var lrt = _left.rectTransform;
        lrt.anchorMin = new Vector2(0, 0);
        lrt.anchorMax = new Vector2(0, 1);
        lrt.pivot = new Vector2(0, 0.5f);
        lrt.sizeDelta = new Vector2(capUnits, 0);
        lrt.anchoredPosition = new Vector2(0, 0);

        // Center
        var crt = _center.rectTransform;
        crt.anchorMin = new Vector2(0, 0);
        crt.anchorMax = new Vector2(0, 1);
        crt.pivot = new Vector2(0, 0.5f);
        crt.sizeDelta = new Vector2(centerWidth, 0);
        crt.anchoredPosition = new Vector2(capUnits, 0);

        // Right
        var rrt = _right.rectTransform;
        rrt.anchorMin = new Vector2(1, 0);
        rrt.anchorMax = new Vector2(1, 1);
        rrt.pivot = new Vector2(1, 0.5f);
        rrt.sizeDelta = new Vector2(capUnits, 0);
        rrt.anchoredPosition = new Vector2(0, 0);
    }

#if UNITY_EDITOR
    // 인스펙터에서 값 바꾸면 바로 반영
    void OnValidate()
    {
        capWidthPx = Mathf.Max(0, capWidthPx);
        if (Application.isPlaying)
            SyncAll();
        else
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this) SyncAll();
            };
    }
#endif
}
