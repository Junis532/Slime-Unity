using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class MapEditorWindow : EditorWindow
{
    private List<GameObject> groundPrefabs = new List<GameObject>();
    private GameObject selectedPrefab;

    private int defaultPrefabIndex = 0;
    private string mapName = "NewMap";
    private int gridWidth = 20;
    private int gridHeight = 20;
    private float tileSize = 1f;
    private Vector2 scrollPos;
    private Vector2 fullScroll = Vector2.zero;

    private Dictionary<Vector2Int, Dictionary<string, GameObject>> virtualMap = new();
    private string[] layers = new[] { "Ground", "Structure", "Decoration" };
    private int selectedLayerIndex = 0;
    private Dictionary<string, Color> layerColors = new Dictionary<string, Color>
    {
        { "Ground", Color.white },
        { "Structure", Color.cyan },
        { "Decoration", Color.magenta }
    };

    private bool isMouseDragging = false;

    [MenuItem("Tools/Layered Map Editor")]
    public static void ShowWindow()
    {
        GetWindow<MapEditorWindow>("Map Editor");
    }

    private void OnEnable()
    {
        LoadPrefabs();
        if (groundPrefabs.Count > 0)
            defaultPrefabIndex = 0;
        InitializeMap();
    }

    private void LoadPrefabs()
    {
        groundPrefabs.Clear();
        var loaded = Resources.LoadAll<GameObject>("GROUND");
        groundPrefabs.AddRange(loaded);
    }

    private void InitializeMap()
    {
        virtualMap.Clear();

        if (groundPrefabs.Count == 0) return;
        GameObject defaultPrefab = groundPrefabs[defaultPrefabIndex];

        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                virtualMap[pos] = new Dictionary<string, GameObject>();
                virtualMap[pos]["Ground"] = defaultPrefab;
            }
        }
    }

    private void OnGUI()
    {
        fullScroll = EditorGUILayout.BeginScrollView(fullScroll);

        GUILayout.Label("맵 설정", EditorStyles.boldLabel);

        mapName = EditorGUILayout.TextField("맵 이름", mapName);

        int prevWidth = gridWidth;
        int prevHeight = gridHeight;

        gridWidth = EditorGUILayout.IntSlider("가로", gridWidth, 1, 50);
        gridHeight = EditorGUILayout.IntSlider("세로", gridHeight, 1, 50);

        if (prevWidth != gridWidth || prevHeight != gridHeight)
        {
            InitializeMap();
        }

        GUILayout.Space(10);
        GUILayout.Label("기본 프리팹 선택", EditorStyles.boldLabel);

        if (groundPrefabs.Count == 0)
        {
            EditorGUILayout.HelpBox("Resources/GROUND 폴더에 프리팹이 없습니다.", MessageType.Warning);
        }
        else
        {
            string[] prefabNames = new string[groundPrefabs.Count];
            for (int i = 0; i < groundPrefabs.Count; i++)
                prefabNames[i] = groundPrefabs[i].name;

            int newDefaultIndex = EditorGUILayout.Popup(defaultPrefabIndex, prefabNames);
            if (newDefaultIndex != defaultPrefabIndex)
            {
                defaultPrefabIndex = newDefaultIndex;
                InitializeMap();
            }
        }

        GUILayout.Space(10);
        GUILayout.Label("레이어 선택", EditorStyles.boldLabel);
        selectedLayerIndex = EditorGUILayout.Popup(selectedLayerIndex, layers);

        GUILayout.Space(10);
        GUILayout.Label("프리팹 선택", EditorStyles.boldLabel);
        scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(100));
        foreach (var prefab in groundPrefabs)
        {
            if (GUILayout.Button(prefab.name))
            {
                selectedPrefab = prefab;
            }
        }
        GUILayout.EndScrollView();

        GUILayout.Space(10);
        DrawGridUI();

        GUILayout.Space(10);
        if (GUILayout.Button("맵 씬에 적용 및 저장"))
        {
            ApplyToSceneAndSave();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawGridUI()
    {
        float cellSize = 48f;
        float totalGridHeight = gridHeight * cellSize;
        float totalGridWidth = gridWidth * cellSize;

        Rect gridRect = GUILayoutUtility.GetRect(totalGridWidth, totalGridHeight);
        Vector2 start = gridRect.position;
        Vector2 mousePos = Event.current.mousePosition;
        Event e = Event.current;

        if (e.type == EventType.MouseUp) isMouseDragging = false;

        for (int y = gridHeight - 1; y >= 0; y--)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                Rect cellRect = new Rect(start.x + x * cellSize, start.y + (gridHeight - 1 - y) * cellSize, cellSize, cellSize);

                if (virtualMap.TryGetValue(pos, out var layerDict))
                {
                    foreach (var layerKvp in layerDict)
                    {
                        string layer = layerKvp.Key;
                        GameObject prefab = layerKvp.Value;

                        SpriteRenderer sr = prefab.GetComponent<SpriteRenderer>();
                        if (sr == null || sr.sprite == null) continue;

                        Texture2D tex = sr.sprite.texture;
                        Rect spriteRect = sr.sprite.rect;
                        Rect uv = new Rect(
                            spriteRect.x / tex.width,
                            spriteRect.y / tex.height,
                            spriteRect.width / tex.width,
                            spriteRect.height / tex.height
                        );

                        float alpha = (layer == layers[selectedLayerIndex]) ? 1f : 0.3f;
                        Color prevColor = GUI.color;
                        GUI.color = new Color(1, 1, 1, alpha);

                        GUI.DrawTextureWithTexCoords(cellRect, tex, uv);
                        GUI.color = prevColor;

                        if (layer != layers[selectedLayerIndex])
                        {
                            Handles.BeginGUI();
                            Handles.color = layerColors.ContainsKey(layer) ? layerColors[layer] : Color.gray;
                            Handles.DrawAAPolyLine(2f, new Vector3[]
                            {
                                new Vector3(cellRect.xMin, cellRect.yMin),
                                new Vector3(cellRect.xMax, cellRect.yMin),
                                new Vector3(cellRect.xMax, cellRect.yMax),
                                new Vector3(cellRect.xMin, cellRect.yMax),
                                new Vector3(cellRect.xMin, cellRect.yMin),
                            });
                            Handles.EndGUI();
                        }
                    }
                }

                GUI.Box(cellRect, GUIContent.none);

                if (e.type == EventType.MouseDown && cellRect.Contains(mousePos))
                {
                    isMouseDragging = true;
                    PlaceTile(pos);
                    e.Use();
                }
                else if (e.type == EventType.MouseDrag && isMouseDragging && cellRect.Contains(mousePos))
                {
                    PlaceTile(pos);
                    e.Use();
                }
                else if (e.type == EventType.MouseDown && e.button == 1 && cellRect.Contains(mousePos))
                {
                    if (virtualMap.ContainsKey(pos) && virtualMap[pos].ContainsKey(layers[selectedLayerIndex]))
                    {
                        virtualMap[pos].Remove(layers[selectedLayerIndex]);
                        if (virtualMap[pos].Count == 0)
                            virtualMap.Remove(pos);
                        Repaint();
                        e.Use();
                    }
                }
            }
        }
    }

    private void PlaceTile(Vector2Int pos)
    {
        if (selectedPrefab == null) return;

        if (!virtualMap.ContainsKey(pos))
            virtualMap[pos] = new Dictionary<string, GameObject>();

        virtualMap[pos][layers[selectedLayerIndex]] = selectedPrefab;
        Repaint();
    }

    private void ApplyToSceneAndSave()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject mapRoot = new GameObject("Map");
        Dictionary<string, GameObject> layerParents = new Dictionary<string, GameObject>();
        foreach (string layer in layers)
        {
            GameObject layerObj = new GameObject(layer);
            layerObj.transform.parent = mapRoot.transform;
            layerParents[layer] = layerObj;
        }

        foreach (var kvp in virtualMap)
        {
            Vector2Int pos = kvp.Key;
            foreach (var layerKvp in kvp.Value)
            {
                string layer = layerKvp.Key;
                GameObject prefab = layerKvp.Value;

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.transform.position = new Vector3(pos.x * tileSize, pos.y * tileSize, GetZFromLayer(layer));
                instance.name = $"{layer}_{prefab.name}_{pos.x}_{pos.y}";

                // 콜라이더에 isTrigger 적용
                foreach (var col in instance.GetComponents<Collider2D>())
                {
                    col.isTrigger = true;
                }

                if (layerParents.TryGetValue(layer, out GameObject parentObj))
                    instance.transform.parent = parentObj.transform;
                else
                    instance.transform.parent = mapRoot.transform;
            }
        }

        string path = "Assets/Scenes/MapList/";
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);

        string scenePath = $"{path}{mapName}.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log("씬 저장 완료: " + scenePath);
    }

    private float GetZFromLayer(string layerName)
    {
        return layerName switch
        {
            "Ground" => 0f,
            "Structure" => -0.5f,
            "Decoration" => -1f,
            _ => 0f
        };
    }
}
