using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 부모 RawImage를 가로 3-슬라이스(Left/Center/Right)로 쪼개서
/// 부모 폭이 변해도 가운데만 늘어나게 만드는 래퍼.
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

        // 이미 하위 오브젝트가 존재할 경우 자동 연결
        if (transform.childCount >= 3)
        {
            _left = transform.Find("Left")?.GetComponent<RawImage>();
            _center = transform.Find("Center")?.GetComponent<RawImage>();
            _right = transform.Find("Right")?.GetComponent<RawImage>();
        }

        // 하나라도 null이면 새로 생성
        if (_left == null || _center == null || _right == null)
            CreateChildren();

        SyncAll();
    }

    void OnEnable() => SyncAll();

    protected void OnRectTransformDimensionsChange()
    {
        if (!isActiveAndEnabled) return;
        SyncLayout();
    }

    void CreateChildren()
    {
        // 기존 자식 제거
        for (int i = transform.childCount - 1; i >= 0; --i)
            DestroyImmediate(transform.GetChild(i).gameObject);

        _left = CreatePiece("Left");
        _center = CreatePiece("Center");
        _right = CreatePiece("Right");

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
        // 안전한 null 검사
        if (_src == null)
        {
            Debug.LogError("[RawImageThreeSliceHorizontal] _src is null!");
            return;
        }

        if (_left == null || _center == null || _right == null)
        {
            Debug.LogWarning("[RawImageThreeSliceHorizontal] Child RawImages missing, recreating...");
            CreateChildren();
        }

        if (_src.texture == null) return;

        // 공통 속성 동기화
        _left.texture = _center.texture = _right.texture = _src.texture;
        _left.color = _center.color = _right.color = _src.color;
        _left.material = _center.material = _right.material = _src.material;

        // UV 계산
        var texW = (float)_src.texture.width;
        var uv = useRawImageUvRect ? _src.uvRect : new Rect(0, 0, 1, 1);

        float capU = capWidthPx / texW;
        capU = Mathf.Clamp(capU, 0f, uv.width * 0.49f);

        _left.uvRect = new Rect(uv.xMin, uv.yMin, capU, uv.height);

        float centerU = Mathf.Max(uv.width - capU - capU, 0f);
        _center.uvRect = new Rect(uv.xMin + capU, uv.yMin, centerU, uv.height);

        _right.uvRect = new Rect(uv.xMax - capU, uv.yMin, capU, uv.height);

        if (centerTiled)
            _center.uvRect = new Rect(_center.uvRect.x, _center.uvRect.y, 1f, _center.uvRect.height);

        SyncLayout();

        // 원본은 표시 안 함
        _src.enabled = false;
    }

    void SyncLayout()
    {
        if (_src == null || _src.texture == null) return;

        var tex = _src.texture;
        float texW = tex.width;
        float texH = tex.height;

        var height = _rt.rect.height;
        var width = _rt.rect.width;

        float pxToUnits = (height <= 0f || texH <= 0f) ? 0f : (height / texH);
        float capUnits = Mathf.Round(capWidthPx * pxToUnits);
        float centerWidth = Mathf.Max(0f, width - (capUnits * 2f));

        var lrt = _left.rectTransform;
        lrt.anchorMin = new Vector2(0, 0);
        lrt.anchorMax = new Vector2(0, 1);
        lrt.pivot = new Vector2(0, 0.5f);
        lrt.sizeDelta = new Vector2(capUnits, 0);
        lrt.anchoredPosition = new Vector2(0, 0);

        var crt = _center.rectTransform;
        crt.anchorMin = new Vector2(0, 0);
        crt.anchorMax = new Vector2(0, 1);
        crt.pivot = new Vector2(0, 0.5f);
        crt.sizeDelta = new Vector2(centerWidth, 0);
        crt.anchoredPosition = new Vector2(capUnits, 0);

        var rrt = _right.rectTransform;
        rrt.anchorMin = new Vector2(1, 0);
        rrt.anchorMax = new Vector2(1, 1);
        rrt.pivot = new Vector2(1, 0.5f);
        rrt.sizeDelta = new Vector2(capUnits, 0);
        rrt.anchoredPosition = new Vector2(0, 0);
    }

#if UNITY_EDITOR
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