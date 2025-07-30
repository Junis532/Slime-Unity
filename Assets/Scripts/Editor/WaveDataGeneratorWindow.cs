//using UnityEngine;
//using UnityEditor;
//using System.Collections.Generic;
//using System.IO;

//public class WaveDataGeneratorWindow : EditorWindow
//{
//    private string basePath = "Assets/Prefabs/Enemy/EnemyWavePrefabs";
//    private string outputPath = "Assets/Resources/WaveDatas";

//    [MenuItem("Tools/Generate WaveData")]
//    public static void ShowWindow()
//    {
//        GetWindow<WaveDataGeneratorWindow>("WaveData Generator");
//    }

//    private void OnGUI()
//    {
//        GUILayout.Label("WaveData 자동 생성기", EditorStyles.boldLabel);
//        basePath = EditorGUILayout.TextField("스킬 프리팹 경로", basePath);
//        outputPath = EditorGUILayout.TextField("WaveData 저장 경로", outputPath);

//        if (GUILayout.Button("WaveData 생성"))
//        {
//            GenerateWaveDatas();
//        }
//    }

//    private void GenerateWaveDatas()
//    {
//        if (!Directory.Exists(outputPath))
//            Directory.CreateDirectory(outputPath);

//        // 폴더 이름 정확하게!
//        string[] skillNames = { "Blink", "Elect", "FireBall", "Shiled" };

//        // 공통 Wave 폴더명 수집 (Wave1, Wave2, ...)
//        HashSet<string> waveNames = new HashSet<string>();
//        foreach (string skill in skillNames)
//        {
//            string skillPath = Path.Combine(basePath, skill);
//            if (!Directory.Exists(skillPath))
//            {
//                Debug.LogWarning($"[경고] 스킬 폴더 없음: {skillPath}");
//                continue;
//            }

//            foreach (string subDir in Directory.GetDirectories(skillPath))
//            {
//                string waveName = new DirectoryInfo(subDir).Name;
//                if (waveName.StartsWith("Wave"))
//                    waveNames.Add(waveName);
//            }
//        }

//        foreach (string waveName in waveNames)
//        {
//            WaveData waveData = ScriptableObject.CreateInstance<WaveData>();
//            waveData.skillMonsterLists = new List<SkillMonsterList>();

//            foreach (string skill in skillNames)
//            {
//                SkillMonsterList skillList = new SkillMonsterList { monsters = new List<GameObject>() };
//                string prefabFolder = Path.Combine(basePath, skill, waveName);

//                if (Directory.Exists(prefabFolder))
//                {
//                    string[] prefabPaths = Directory.GetFiles(prefabFolder, "*.prefab", SearchOption.TopDirectoryOnly);
//                    foreach (string prefabPath in prefabPaths)
//                    {
//                        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
//                        if (prefab != null)
//                            skillList.monsters.Add(prefab);
//                    }
//                }
//                else
//                {
//                    Debug.LogWarning($"[주의] {skill}/{waveName} 경로 없음. 빈 리스트로 대체됨.");
//                }

//                waveData.skillMonsterLists.Add(skillList); // 무조건 추가
//            }

//            string assetName = $"{waveName}_WaveData.asset";
//            string assetPath = Path.Combine(outputPath, assetName);

//            AssetDatabase.CreateAsset(waveData, assetPath);
//            Debug.Log($"[생성됨] {assetPath}");
//        }

//        AssetDatabase.SaveAssets();
//        AssetDatabase.Refresh();
//        Debug.Log("✅ 모든 WaveData 생성 완료!");
//    }
//}
