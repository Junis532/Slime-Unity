// Assets/Editor/SpriteComposerWindow.cs
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class SpriteComposerWindow : EditorWindow
{
    [SerializeField] private string spritesFolder = "Assets/Sprites/Sliced";
    [SerializeField] private string saveFolder = "Assets/Resources/Composites";

    private Vector2 paletteScroll;
    private List<Sprite> palette = new List<Sprite>();
    private Sprite selectedSprite;
    private Texture2D eraserIcon;

    // 캐시된 스프라이트 텍스처
    private Dictionary<Sprite, Texture2D> spritePreviews = new Dictionary<Sprite, Texture2D>();

    private int columns = 3;
    private int rows = 3;
    private float cellWorldSize = 1f;
    private bool fitToCell = true;

    private Sprite[,] grid;
    private Rect gridRect;
    private Vector2 gridScroll;

    [MenuItem("Tools/Sprite Composer")]
    public static void ShowWindow()
    {
        GetWindow<SpriteComposerWindow>("Sprite Composer");
    }

    private void OnEnable()
    {
        InitGrid(columns, rows);
        LoadPalette();
        MakeEraserIcon();
    }

    private void InitGrid(int cols, int rows)
    {
        grid = new Sprite[rows, cols];
    }

    private void MakeEraserIcon()
    {
        if (eraserIcon != null) return;
        eraserIcon = new Texture2D(32, 32);
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
                eraserIcon.SetPixel(x, y, ((x ^ y) & 4) == 0 ? new Color(0.85f, 0.85f, 0.85f, 1f) : Color.white);
        eraserIcon.Apply();
    }

    private void LoadPalette()
    {
        palette.Clear();
        spritePreviews.Clear();

        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { spritesFolder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sp != null)
            {
                palette.Add(sp);
                spritePreviews[sp] = GenerateSpritePreview(sp);
            }
        }
    }

    private Texture2D GenerateSpritePreview(Sprite sp)
    {
        if (sp == null) return null;
        if (spritePreviews.TryGetValue(sp, out var tex)) return tex;

        Texture2D preview;
        try
        {
            preview = new Texture2D((int)sp.rect.width, (int)sp.rect.height);
            Color[] pixels = sp.texture.GetPixels(
                (int)sp.textureRect.x,
                (int)sp.textureRect.y,
                (int)sp.textureRect.width,
                (int)sp.textureRect.height
            );
            preview.SetPixels(pixels);
            preview.Apply();
        }
        catch
        {
            preview = EditorGUIUtility.IconContent("Sprite Icon").image as Texture2D;
        }

        spritePreviews[sp] = preview;
        return preview;
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(6);
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(320)))
            {
                DrawSettings();
                EditorGUILayout.Space(8);
                DrawPalette();
            }

            using (new EditorGUILayout.VerticalScope())
            {
                DrawGridCanvas();
                EditorGUILayout.Space(6);
                DrawBottomActions();
            }
        }
    }

    private void DrawSettings()
    {
        EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        int newColumns = Mathf.Max(1, EditorGUILayout.IntField("Columns", columns));
        int newRows = Mathf.Max(1, EditorGUILayout.IntField("Rows", rows));
        cellWorldSize = Mathf.Max(0.01f, EditorGUILayout.FloatField("Cell World Size", cellWorldSize));
        fitToCell = EditorGUILayout.ToggleLeft("Fit sprite to cell", fitToCell);

        if (newColumns != columns || newRows != rows)
        {
            ResizeGrid(newColumns, newRows);
            columns = newColumns;
            rows = newRows;
        }

        spritesFolder = EditorGUILayout.TextField("Sprites Folder", spritesFolder);
        saveFolder = EditorGUILayout.TextField("Save Prefab Folder", saveFolder);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Reload Palette")) LoadPalette();
            if (GUILayout.Button("Apply Grid Size")) ResizeGrid(columns, rows);
        }

        if (EditorGUI.EndChangeCheck()) Repaint();
    }

    private void ResizeGrid(int newCols, int newRows)
    {
        Sprite[,] newGrid = new Sprite[newRows, newCols];
        for (int y = 0; y < Mathf.Min(rows, newRows); y++)
        {
            for (int x = 0; x < Mathf.Min(columns, newCols); x++)
            {
                newGrid[y, x] = grid[y, x];
            }
        }
        grid = newGrid;
        Repaint();
    }

    private void DrawPalette()
    {
        EditorGUILayout.LabelField("Palette (from folder)", EditorStyles.boldLabel);

        float itemSize = 64f;
        int perRow = 4;
        paletteScroll = EditorGUILayout.BeginScrollView(paletteScroll, GUILayout.Height(260));
        int idx = 0;

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button(new GUIContent(eraserIcon, "Eraser (clear cell)"), GUILayout.Width(itemSize), GUILayout.Height(itemSize)))
                selectedSprite = null;
            GUILayout.FlexibleSpace();
        }

        EditorGUILayout.Space(4);

        while (idx < palette.Count)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                for (int i = 0; i < perRow && idx < palette.Count; i++, idx++)
                {
                    var sp = palette[idx];
                    Texture2D texPreview = GenerateSpritePreview(sp);

                    GUIStyle style = new GUIStyle(GUI.skin.button);
                    if (selectedSprite == sp) style.normal.textColor = Color.cyan;

                    if (GUILayout.Button(new GUIContent(texPreview, sp.name), style, GUILayout.Width(itemSize), GUILayout.Height(itemSize)))
                        selectedSprite = sp;
                }
            }
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.HelpBox(selectedSprite == null ? "Eraser selected: click on a cell to clear it." :
            $"Selected: {selectedSprite.name}", MessageType.None);
    }

    private void DrawGridCanvas()
    {
        EditorGUILayout.LabelField("Compose Grid", EditorStyles.boldLabel);

        float cellPx = 72f;
        float gridWidth = columns * cellPx;
        float gridHeight = rows * cellPx;

        gridScroll = EditorGUILayout.BeginScrollView(gridScroll, GUILayout.ExpandHeight(true));
        Rect r = GUILayoutUtility.GetRect(gridWidth, gridHeight, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
        gridRect = r;

        EditorGUI.DrawRect(r, new Color(0.15f, 0.15f, 0.15f, 1f));

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                Rect cell = new Rect(r.x + x * cellPx, r.y + y * cellPx, cellPx - 1, cellPx - 1);
                EditorGUI.DrawRect(cell, new Color(0.22f, 0.22f, 0.22f, 1f));
                Handles.color = new Color(0, 0, 0, 0.6f);
                Handles.DrawAAPolyLine(2f, new Vector3(cell.x, cell.yMax), new Vector3(cell.xMax, cell.yMax));
                Handles.DrawAAPolyLine(2f, new Vector3(cell.xMax, cell.y), new Vector3(cell.xMax, cell.yMax));

                var sp = grid[y, x];
                if (sp != null)
                {
                    Texture2D tex = sp.texture;
                    var tr = sp.textureRect;
                    Rect uv = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);
                    GUI.DrawTextureWithTexCoords(cell, tex, uv, true);
                }
            }
        }

        Event e = Event.current;
        if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && r.Contains(e.mousePosition))
        {
            Vector2 local = e.mousePosition - new Vector2(r.x, r.y);
            int gx = Mathf.Clamp(Mathf.FloorToInt(local.x / cellPx), 0, columns - 1);
            int gy = Mathf.Clamp(Mathf.FloorToInt(local.y / cellPx), 0, rows - 1);
            grid[gy, gx] = selectedSprite;
            Repaint();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawBottomActions()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Clear Grid", GUILayout.Height(28)))
            {
                grid = new Sprite[rows, columns];
                Repaint();
            }

            if (GUILayout.Button("Create Prefab", GUILayout.Height(28)))
                CreateCompositePrefab();
        }
    }

    private static Vector2 GetSpriteWorldSize(Sprite sp) => sp.rect.size / sp.pixelsPerUnit;

    private void CreateCompositePrefab()
    {
        if (!AssetDatabase.IsValidFolder(saveFolder))
        {
            Directory.CreateDirectory(saveFolder.Replace("Assets/", Application.dataPath + "/"));
            AssetDatabase.Refresh();
        }

        string prefabName = $"Composite_{columns}x{rows}_{System.DateTime.Now:HHmmss}";
        GameObject root = new GameObject(prefabName);

        var rb = root.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        var composite = root.AddComponent<CompositeCollider2D>();
        composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
        composite.generationType = CompositeCollider2D.GenerationType.Synchronous;

        float width = columns * cellWorldSize;
        float height = rows * cellWorldSize;
        Vector2 origin = new Vector2(-width * 0.5f + cellWorldSize * 0.5f, -height * 0.5f + cellWorldSize * 0.5f);

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                Sprite sp = grid[y, x];
                if (sp == null) continue;

                GameObject tile = new GameObject($"tile_{y}_{x}");
                tile.transform.SetParent(root.transform, false);

                var sr = tile.AddComponent<SpriteRenderer>();
                sr.sprite = sp;

                Vector3 pos = new Vector3(origin.x + x * cellWorldSize, origin.y + y * cellWorldSize, 0f);
                tile.transform.localPosition = pos;

                if (fitToCell)
                {
                    Vector2 ws = GetSpriteWorldSize(sp);
                    float sx = ws.x <= 0 ? 1f : (cellWorldSize / ws.x);
                    float sy = ws.y <= 0 ? 1f : (cellWorldSize / ws.y);
                    tile.transform.localScale = new Vector3(sx, sy, 1f);
                }

                var bc = tile.AddComponent<BoxCollider2D>();
                bc.size = new Vector2(cellWorldSize, cellWorldSize);
                bc.usedByComposite = true;
            }
        }

        string savePath = $"{saveFolder}/{prefabName}.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, savePath);
        Debug.Log($"[SpriteComposer] Prefab saved: {savePath}");

        DestroyImmediate(root);
        AssetDatabase.Refresh();
        Selection.activeObject = prefab;
    }
}
