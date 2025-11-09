using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class TopDownShadow2D : MonoBehaviour
{
    [Flags]
    public enum ShadowDirections
    {
        None = 0,
        North = 1 << 0,
        East = 1 << 1,
        South = 1 << 2,
        West = 1 << 3,
        All = North | East | South | West
    }

    [Header("기본")]
    public bool enableShadow = true;

    [Tooltip("여러 방향을 동시에 체크할 수 있습니다.")]
    public ShadowDirections directions = ShadowDirections.South;

    [Tooltip("그림자 시작 오프셋(월드 유닛)")]
    public Vector2 baseOffset = new Vector2(0f, -0.02f);

    [Header("길이/모양")]
    [Tooltip("그림자 길이(방향벡터로 멀어지는 길이)")]
    public float length = 0.22f;

    [Tooltip("세로 납작 비율(0.3~1 권장)")]
    [Range(0.2f, 1.2f)] public float squashY = 0.55f;

    [Tooltip("전반적인 크기 스케일")]
    public float overallScale = 1f;

    [Header("부드러움/알파")]
    [Tooltip("소프트 샘플 개수. 1이면 날카로움 / 3~6 권장")]
    [Range(1, 8)] public int softnessSamples = 4;

    [Tooltip("샘플 간 퍼짐(수직축 퍼지기)")]
    [Range(0f, 0.06f)] public float softnessSpread = 0.02f;

    [Tooltip("그림자 진하기(총 알파)")]
    [Range(0f, 1f)] public float alpha = 0.55f;

    public Color shadowTint = Color.black;

    [Header("렌더링")]
    [Tooltip("원본과 같은 Sorting Layer 사용")]
    public bool useSameSortingLayer = true;
    public string sortingLayerName = "Default";

    [Tooltip("원본보다 몇 단계 아래(음수면 뒤쪽)")]
    public int sortingOrderOffset = -5;

    [Header("기타")]
    [Tooltip("원본 SpriteRenderer의 flipX/Y를 따라갈지")]
    public bool followSpriteFlip = true;

    // 내부 캐시
    private SpriteRenderer _src;
    private Transform _root; // __ShadowRoot
    private Sprite _lastSprite;
    private bool _lastFlipX, _lastFlipY;

    // 방향별 샘플들
    private readonly Dictionary<ShadowDirections, List<SpriteRenderer>> _dirTaps =
        new Dictionary<ShadowDirections, List<SpriteRenderer>>()
        {
            { ShadowDirections.North, new List<SpriteRenderer>() },
            { ShadowDirections.East,  new List<SpriteRenderer>() },
            { ShadowDirections.South, new List<SpriteRenderer>() },
            { ShadowDirections.West,  new List<SpriteRenderer>() },
        };

    // ======================================================

    void OnEnable()
    {
        _src = GetComponent<SpriteRenderer>();
        SyncAll(true);
    }

    void OnValidate()
    {
        if (!isActiveAndEnabled) return;
        _src = GetComponent<SpriteRenderer>();
        SyncAll(false);
    }

    void LateUpdate()
    {
        if (!enableShadow)
        {
            if (_root) _root.gameObject.SetActive(false);
            return;
        }
        if (_root && !_root.gameObject.activeSelf) _root.gameObject.SetActive(true);

        if (_src && (_src.sprite != _lastSprite || _src.flipX != _lastFlipX || _src.flipY != _lastFlipY))
        {
            SyncSpriteAndFlip();
        }

        SyncTransformAll();
        SyncRenderSettings();
    }

    // ================== 동기화 헬퍼 =======================

    void SyncAll(bool forceRebuild = false)
    {
        BuildOrRefreshRoot();

        // 방향별 자식/샘플 구성
        BuildOrRefreshPerDirection(ShadowDirections.North, forceRebuild);
        BuildOrRefreshPerDirection(ShadowDirections.East, forceRebuild);
        BuildOrRefreshPerDirection(ShadowDirections.South, forceRebuild);
        BuildOrRefreshPerDirection(ShadowDirections.West, forceRebuild);

        // 선택되지 않은 방향은 비활성/정리
        ToggleDirectionGroups();

        SyncSpriteAndFlip();
        SyncRenderSettings();
        SyncTransformAll();
    }

    // 루트 생성/캐시
    void BuildOrRefreshRoot()
    {
        if (_root != null) return;
        var t = transform.Find("__ShadowRoot");
        _root = t ? t : new GameObject("__ShadowRoot").transform;
        _root.SetParent(transform, false);
        _root.localPosition = Vector3.zero;
        _root.localRotation = Quaternion.identity;
        _root.localScale = Vector3.one;
    }

    // 방향별 그룹과 샘플 수 맞추기
    void BuildOrRefreshPerDirection(ShadowDirections dir, bool forceRebuild)
    {
        string groupName = "__" + dir.ToString();
        Transform group = _root.Find(groupName);
        if (group == null)
        {
            group = new GameObject(groupName).transform;
            group.SetParent(_root, false);
        }

        var taps = _dirTaps[dir];
        int targetCount = Mathf.Max(1, softnessSamples);

        // 필요 수만큼 만들기
        while (taps.Count < targetCount)
        {
            var go = new GameObject($"{dir}_Tap_{taps.Count}");
            go.hideFlags = HideFlags.DontSaveInEditor;
            go.transform.SetParent(group, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sharedMaterial = _src ? _src.sharedMaterial : null;
            taps.Add(sr);
        }

        // 초과분 제거
        while (taps.Count > targetCount)
        {
            var last = taps[taps.Count - 1];
            if (last) DestroyImmediate(last.gameObject);
            taps.RemoveAt(taps.Count - 1);
        }

        // 그룹 활성화는 선택 여부에 따라 아래에서 처리
    }

    // 선택되지 않은 방향 그룹은 끄기
    void ToggleDirectionGroups()
    {
        ToggleOne(ShadowDirections.North);
        ToggleOne(ShadowDirections.East);
        ToggleOne(ShadowDirections.South);
        ToggleOne(ShadowDirections.West);

        void ToggleOne(ShadowDirections d)
        {
            var g = _root.Find("__" + d.ToString());
            if (g) g.gameObject.SetActive(IsSelected(d));
        }
    }

    bool IsSelected(ShadowDirections d) => (directions & d) == d;

    void SyncSpriteAndFlip()
    {
        if (_src == null) return;
        _lastSprite = _src.sprite;
        _lastFlipX = _src.flipX;
        _lastFlipY = _src.flipY;

        foreach (var kv in _dirTaps)
        {
            foreach (var sr in kv.Value)
            {
                if (!sr) continue;
                sr.sprite = _lastSprite;
                if (followSpriteFlip)
                {
                    sr.flipX = _lastFlipX;
                    sr.flipY = _lastFlipY;
                }
                else
                {
                    sr.flipX = false;
                    sr.flipY = false;
                }
            }
        }
    }

    void SyncRenderSettings()
    {
        if (_src == null) return;

        int sortingLayerID = useSameSortingLayer ? _src.sortingLayerID : SortingLayer.NameToID(sortingLayerName);
        int order = _src.sortingOrder + sortingOrderOffset;

        // 전체 알파를 모든 샘플에 공평 분배(방향 합쳐서)
        int totalSamples = 0;
        foreach (var kv in _dirTaps)
        {
            if (!IsSelected(kv.Key)) continue;
            totalSamples += kv.Value.Count;
        }
        totalSamples = Mathf.Max(1, totalSamples);

        float perTapAlpha = alpha / totalSamples;
        var col = new Color(shadowTint.r, shadowTint.g, shadowTint.b, perTapAlpha);

        foreach (var kv in _dirTaps)
        {
            if (!IsSelected(kv.Key)) continue;
            foreach (var sr in kv.Value)
            {
                if (!sr) continue;
                sr.sharedMaterial = _src.sharedMaterial;
                sr.sortingLayerID = sortingLayerID;
                sr.sortingOrder = order;
                sr.color = col;
                sr.enabled = enableShadow;
            }
        }
    }

    void SyncTransformAll()
    {
        if (_root == null) return;

        ApplyForDir(ShadowDirections.North, Vector2.up);
        ApplyForDir(ShadowDirections.East, Vector2.right);
        ApplyForDir(ShadowDirections.South, Vector2.down);
        ApplyForDir(ShadowDirections.West, Vector2.left);
    }

    void ApplyForDir(ShadowDirections dir, Vector2 dirVec)
    {
        if (!IsSelected(dir)) return;

        var taps = _dirTaps[dir];
        int n = Mathf.Max(1, taps.Count);

        Vector2 start = baseOffset;
        Vector2 end = start + dirVec.normalized * length;

        for (int i = 0; i < n; i++)
        {
            var sr = taps[i];
            if (!sr) continue;

            float t = (n == 1) ? 1f : (i / (n - 1f));     // 0..1
            Vector2 along = Vector2.Lerp(start, end, t);

            // dir에 수직인 퍼짐
            Vector2 perp = new Vector2(-dirVec.y, dirVec.x);
            float spread = softnessSpread * (0.5f + i * 0.5f / Mathf.Max(1, n - 1));
            Vector2 jitter = perp * spread;

            sr.transform.localPosition = new Vector3(along.x, along.y, 0f) + (Vector3)jitter;

            // 납작/스케일
            sr.transform.localScale = new Vector3(overallScale, overallScale * squashY, 1f);
            sr.transform.localRotation = Quaternion.identity;
        }
    }
}
