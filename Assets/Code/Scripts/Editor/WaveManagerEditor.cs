//using UnityEditor;
//using UnityEngine;
//using System.Collections.Generic;
//using System.IO;

//[CustomEditor(typeof(WaveManager))]
//public class WaveManagerEditor : Editor
//{
//    public override void OnInspectorGUI()
//    {
//        DrawDefaultInspector(); // 기본 인스펙터 표시
//        WaveManager waveManager = (WaveManager)target;

//        if (GUILayout.Button("🔁 맵 + 적 자동 할당 (Map_01, EnemyPrefab_01~)"))
//        {
//            AssignMapsAndEnemiesAutomatically(waveManager);
//        }

//        if (GUILayout.Button("➕ 새로운 WaveData 생성 및 자동 할당"))
//        {
//            CreateAndAssignNewWaveData(waveManager);
//        }
//    }

//    /// <summary>
//    /// 기존 waveDataList에 맵과 적 prefab 자동 할당
//    /// </summary>
//    private void AssignMapsAndEnemiesAutomatically(WaveManager waveManager)
//    {
//        for (int i = 0; i < waveManager.waveDataList.Count; i++)
//        {
//            AssignMapAndEnemy(waveManager, i);
//        }

//        EditorUtility.SetDirty(waveManager); // 변경 사항 저장
//        AssetDatabase.SaveAssets();
//        Debug.Log("[WaveManagerEditor] ✅ 기존 WaveData 맵 및 적 자동 할당 완료");
//    }

//    /// <summary>
//    /// 새로운 WaveData ScriptableObject를 생성 후 리스트에 추가
//    /// </summary>
//    private void CreateAndAssignNewWaveData(WaveManager waveManager)
//    {
//        // 새 WaveData index (기존 개수에서 +1)
//        int newIndex = waveManager.waveDataList.Count + 1;
//        string waveName = $"Wave{newIndex}_WaveData";

//        // 저장 경로 설정
//        string folderPath = "Assets/Resources/WaveDatas";
//        if (!Directory.Exists(folderPath))
//        {
//            Directory.CreateDirectory(folderPath);
//        }

//        string assetPath = $"{folderPath}/{waveName}.asset";

//        // ScriptableObject 생성
//        WaveData newWaveData = ScriptableObject.CreateInstance<WaveData>();
//        AssetDatabase.CreateAsset(newWaveData, assetPath);

//        // 리스트에 추가
//        waveManager.waveDataList.Add(newWaveData);

//        // 자동으로 Map & Enemy 할당
//        AssignMapAndEnemy(waveManager, newIndex - 1);

//        EditorUtility.SetDirty(waveManager);
//        AssetDatabase.SaveAssets();
//        AssetDatabase.Refresh();

//        Debug.Log($"[WaveManagerEditor] 🟢 새로운 '{waveName}' 생성 및 자동 할당 완료");
//    }

//    /// <summary>
//    /// 특정 waveData에 Map, Enemy 자동 할당
//    /// </summary>
//    private void AssignMapAndEnemy(WaveManager waveManager, int i)
//    {
//        var targetWaveData = waveManager.waveDataList[i];

//        // 📍 Map 자동 할당
//        string mapName = $"Map_{(i + 1).ToString("D2")}";
//        GameObject mapPrefab = Resources.Load<GameObject>($"Maps/{mapName}");
//        if (mapPrefab != null)
//        {
//            targetWaveData.mapPrefab = mapPrefab;
//            Debug.Log($"[WaveManagerEditor] '{mapName}' 로드 및 할당 완료");
//        }
//        else
//        {
//            Debug.LogWarning($"[WaveManagerEditor] '{mapName}' 프리팹을 찾을 수 없습니다.");
//        }

//        // 📍 EnemyPrefab 자동 할당
//        string enemyName = $"EnemyPrefab_{(i + 1).ToString("D2")}";
//        GameObject enemyPrefab = Resources.Load<GameObject>($"Enemies/{enemyName}");
//        if (enemyPrefab != null)
//        {
//            targetWaveData.MonsterLists = new List<GameObject> { enemyPrefab };
//            Debug.Log($"[WaveManagerEditor] Wave {i + 1}: '{enemyName}' 로드 및 할당 완료");
//        }
//        else
//        {
//            Debug.LogWarning($"[WaveManagerEditor] '{enemyName}' 프리팹을 찾을 수 없습니다.");
//        }
//    }
//}
