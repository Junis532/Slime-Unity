using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class WaveDataGeneratorWindow : EditorWindow
{
    private string basePath = "Assets/Prefabs/Enemy/EnemyWavePrefabs";
    private string outputPath = "Assets/Resources/WaveDatas";
    private int waveCount = 1;  // 생성할 WaveData 개수 조절용 변수

    [MenuItem("Tools/Generate WaveData")]
    public static void ShowWindow()
    {
        GetWindow<WaveDataGeneratorWindow>("WaveData Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("WaveData 자동 생성기", EditorStyles.boldLabel);

        basePath = EditorGUILayout.TextField("스킬 프리팹 경로", basePath);
        outputPath = EditorGUILayout.TextField("WaveData 저장 경로", outputPath);

        waveCount = EditorGUILayout.IntField("생성할 WaveData 수", waveCount);
        if (waveCount < 1) waveCount = 1;

        if (GUILayout.Button("WaveData 생성"))
        {
            GenerateWaveDatas();
        }
    }

    private void GenerateWaveDatas()
    {
        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);

        string[] skillNames = { "Blink", "Elect", "FireBall", "Shiled" };

        // 기존 에셋 경로 및 이름 목록 수집
        string[] existingAssets = Directory.GetFiles(outputPath, "*_WaveData.asset", SearchOption.TopDirectoryOnly);
        HashSet<string> existingWaveNames = new HashSet<string>();
        foreach (string assetPath in existingAssets)
        {
            string fileName = Path.GetFileNameWithoutExtension(assetPath);
            // 파일명이 예: Wave1_WaveData 인 경우 Wave1 부분 추출
            int underscoreIndex = fileName.IndexOf('_');
            if (underscoreIndex > 0)
                existingWaveNames.Add(fileName.Substring(0, underscoreIndex));
        }

        // 사용자가 지정한 waveCount 까지 생성 보장
        for (int i = 1; i <= waveCount; i++)
        {
            string waveName = $"Wave{i}";

            // 이미 존재하면 생성 건너뜀
            if (existingWaveNames.Contains(waveName))
                continue;

            WaveData waveData = ScriptableObject.CreateInstance<WaveData>();
            waveData.MonsterLists = new List<GameObject>();

            // 각 스킬별 폴더에서 Wave{i} 폴더 내 프리팹 불러오기
            foreach (string skill in skillNames)
            {
                string prefabFolder = Path.Combine(basePath, skill, waveName);
                if (Directory.Exists(prefabFolder))
                {
                    string[] prefabPaths = Directory.GetFiles(prefabFolder, "*.prefab", SearchOption.TopDirectoryOnly);
                    foreach (string prefabPath in prefabPaths)
                    {
                        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                        if (prefab != null)
                            waveData.MonsterLists.Add(prefab);
                    }
                }
                else
                {
                    Debug.LogWarning($"[주의] {skill}/{waveName} 경로가 없습니다.");
                }
            }

            string assetName = $"{waveName}_WaveData.asset";
            string assetPath = Path.Combine(outputPath, assetName);
            AssetDatabase.CreateAsset(waveData, assetPath);
            Debug.Log($"[생성됨] {assetPath}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"✅ WaveData 생성 완료. (총 {waveCount}개 생성 보장)");
    }

}
