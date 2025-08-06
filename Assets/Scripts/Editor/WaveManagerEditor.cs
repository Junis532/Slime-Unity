using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(WaveManager))]
public class WaveManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // 기본 인스펙터 표시

        WaveManager waveManager = (WaveManager)target;

        if (GUILayout.Button("🔁 맵 + 적 자동 할당 (Map_01, EnemyPrefab_01~)"))
        {
            AssignMapsAndEnemiesAutomatically(waveManager);
        }
    }

    private void AssignMapsAndEnemiesAutomatically(WaveManager waveManager)
    {
        for (int i = 0; i < waveManager.waveDataList.Count; i++)
        {
            // 📍 Map 자동 할당
            string mapName = $"Map_{(i + 1).ToString("D2")}";
            GameObject mapPrefab = Resources.Load<GameObject>($"Maps/{mapName}");

            if (mapPrefab != null)
            {
                waveManager.waveDataList[i].mapPrefab = mapPrefab;
                Debug.Log($"[WaveManagerEditor] '{mapName}' 로드 및 할당 완료");
            }
            else
            {
                Debug.LogWarning($"[WaveManagerEditor] '{mapName}' 프리팹을 찾을 수 없습니다.");
            }

            // 📍 EnemyPrefab_XX 자동 1개만 할당
            string enemyName = $"EnemyPrefab_{(i + 1).ToString("D2")}";
            GameObject enemyPrefab = Resources.Load<GameObject>($"Enemies/{enemyName}");

            if (enemyPrefab != null)
            {
                waveManager.waveDataList[i].MonsterLists = new List<GameObject> { enemyPrefab };
                Debug.Log($"[WaveManagerEditor] Wave {i + 1}: '{enemyName}' 로드 및 할당 완료");
            }
            else
            {
                Debug.LogWarning($"[WaveManagerEditor] '{enemyName}' 프리팹을 찾을 수 없습니다.");
            }
        }

        EditorUtility.SetDirty(waveManager); // 변경 사항 저장 표시
        Debug.Log("[WaveManagerEditor] ✅ 맵 및 적 자동 할당 완료");
    }
}
