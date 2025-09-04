// Assets/Editor/SpriteSplitter.cs
using UnityEngine;
using UnityEditor;
using System.IO;

public class SpriteSplitter : EditorWindow
{
    private Texture2D sourceTexture;
    private int sliceWidth = 64;
    private int sliceHeight = 64;
    private string saveFolder = "Assets/Sprites/Sliced";

    [MenuItem("Tools/Sprite Splitter")]
    public static void ShowWindow()
    {
        GetWindow<SpriteSplitter>("Sprite Splitter");
    }

    private void OnGUI()
    {
        GUILayout.Label("Texture2D Sprite Splitter", EditorStyles.boldLabel);

        sourceTexture = (Texture2D)EditorGUILayout.ObjectField("Source Texture", sourceTexture, typeof(Texture2D), false);
        sliceWidth = EditorGUILayout.IntField("Slice Width", sliceWidth);
        sliceHeight = EditorGUILayout.IntField("Slice Height", sliceHeight);
        saveFolder = EditorGUILayout.TextField("Save Folder", saveFolder);

        if (GUILayout.Button("Split Texture into PNG Sprites"))
        {
            if (sourceTexture == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a source texture.", "OK");
                return;
            }

            SplitTextureToPNGs();
        }
    }

    private void SplitTextureToPNGs()
    {
        // 1️⃣ Texture Read/Write 자동 활성화
        string path = AssetDatabase.GetAssetPath(sourceTexture);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && !importer.isReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
        }

        // 2️⃣ 저장 폴더 확인
        if (!Directory.Exists(saveFolder))
        {
            Directory.CreateDirectory(saveFolder.Replace("Assets/", Application.dataPath + "/"));
            AssetDatabase.Refresh();
        }

        int cols = sourceTexture.width / sliceWidth;
        int rows = sourceTexture.height / sliceHeight;

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                Texture2D newTex = new Texture2D(sliceWidth, sliceHeight);
                newTex.filterMode = FilterMode.Point;

                // 픽셀 복사 (Unity 좌표계 맞춤)
                for (int j = 0; j < sliceHeight; j++)
                {
                    for (int i = 0; i < sliceWidth; i++)
                    {
                        Color c = sourceTexture.GetPixel(x * sliceWidth + i, sourceTexture.height - (y + 1) * sliceHeight + j);
                        newTex.SetPixel(i, j, c);
                    }
                }

                newTex.Apply();

                // PNG로 저장
                byte[] bytes = newTex.EncodeToPNG();
                string fileName = $"{sourceTexture.name}_{y}_{x}.png";
                string savePath = Path.Combine(saveFolder, fileName);
                File.WriteAllBytes(savePath, bytes);

                Object.DestroyImmediate(newTex);
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"[SpriteSplitter] Split {sourceTexture.name} into {cols * rows} PNG sprites at {saveFolder}");
    }
}
