using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class CSVSpawnGeneratorWindow : EditorWindow
{
    private string csvFileName = "enemy_spawn_data";
    private string savePath = "Assets/Resources/EnemySpawnData";
    private MonsterDB monsterDB;
    private List<EnemySpawnDataPreview> previewList = new List<EnemySpawnDataPreview>();

    [MenuItem("Window/Tools/CSV 생성기")]
    public static void ShowWindow()
    {
        GetWindow<CSVSpawnGeneratorWindow>("CSV Spawn Generator");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("CSV 기반 스폰 데이터 생성기", EditorStyles.boldLabel);

        csvFileName = EditorGUILayout.TextField("CSV 파일명", csvFileName);
        savePath = EditorGUILayout.TextField("저장 경로", savePath);

        GUILayout.Space(10);

        monsterDB = (MonsterDB)EditorGUILayout.ObjectField("Monster DB", monsterDB, typeof(MonsterDB), false);

        GUILayout.Space(10);

        if (GUILayout.Button("\uD83D\uDCC4 CSV 미리보기"))
        {
            ParseCSV();
        }

        if (previewList.Count > 0)
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("\uD83D\uDD0D 미리보기", EditorStyles.boldLabel);

            using (var scroll = new GUILayout.ScrollViewScope(Vector2.zero, GUILayout.Height(200)))
            {
                foreach (var p in previewList)
                {
                    EditorGUILayout.BeginVertical("box");
                    for (int i = 0; i < p.enemyIndexesPerSpawner.Count; i++)
                    {
                        EditorGUILayout.LabelField($"Spawner {i + 1}: {string.Join(",", p.enemyIndexesPerSpawner[i])}");
                    }
                    EditorGUILayout.LabelField($"Spawner Count: {p.spawnerCount}");
                    EditorGUILayout.LabelField($"Min Spawn: {p.minSpawn}");
                    EditorGUILayout.LabelField($"Max Spawn: {p.maxSpawn}");
                    EditorGUILayout.EndVertical();
                }
            }

            GUILayout.Space(10);

            if (GUILayout.Button("\uD83D\uDEE0\uFE0F ScriptableObject 생성"))
            {
                CreateScriptableObjects();
            }

            if (GUILayout.Button("\uD83E\uDDF1 Spawner GameObjects 생성"))
            {
                CreateSpawnerObjects();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("CSV를 먼저 불러오세요.", MessageType.Info);
        }
    }

    private void CreateScriptableObjects()
    {
        if (previewList.Count == 0) return;

        Directory.CreateDirectory(savePath);

        for (int i = 0; i < previewList.Count; i++)
        {
            var p = previewList[i];
            EnemySpawnData data = ScriptableObject.CreateInstance<EnemySpawnData>();
            data.SpawnerCount = p.spawnerCount;
            data.MinSpawn = p.minSpawn;
            data.MaxSpawn = p.maxSpawn;

            // 각 스포너에 대한 몬스터 인덱스 리스트를 하나의 리스트로 합치기 (필요 시)
            List<int> flatIndexes = new List<int>();
            foreach (var list in p.enemyIndexesPerSpawner)
            {
                flatIndexes.AddRange(list);
            }
            data.SpawnEnemyIndexes = new List<List<int>>(p.enemyIndexesPerSpawner);

            string assetPath = Path.Combine(savePath, $"EnemySpawnData_{i}.asset");
            AssetDatabase.CreateAsset(data, assetPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("ScriptableObject 생성 완료!");
    }

    private void ParseCSV()
    {
        previewList.Clear();

        TextAsset csvData = Resources.Load<TextAsset>(csvFileName);
        if (csvData == null)
        {
            Debug.LogError($"CSV 파일을 찾을 수 없습니다: Resources/{csvFileName}.csv");
            return;
        }

        string[] lines = csvData.text.Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] values = lines[i].Trim().Split('|');
            if (values.Length < 5) continue;

            string[] enemyGroups = values[1].Split(new[] { "),(" }, System.StringSplitOptions.None);
            List<List<int>> enemyIndexesPerSpawner = new List<List<int>>();

            foreach (var group in enemyGroups)
            {
                string cleanedGroup = group.Replace("(", "").Replace(")", "");
                string[] indices = cleanedGroup.Split(',');

                List<int> parsedIndexes = new List<int>();
                foreach (var indexStr in indices)
                {
                    if (int.TryParse(indexStr.Trim(), out int index))
                        parsedIndexes.Add(index);
                    else
                        Debug.LogWarning($"잘못된 몬스터 인덱스: {indexStr}");
                }

                enemyIndexesPerSpawner.Add(parsedIndexes);
            }

            if (int.TryParse(values[2], out int spawnerCount) &&
                int.TryParse(values[3], out int minSpawn) &&
                int.TryParse(values[4], out int maxSpawn))
            {
                if (enemyIndexesPerSpawner.Count != spawnerCount)
                {
                    Debug.LogWarning($"스포너 개수({spawnerCount})와 괄호 그룹 수({enemyIndexesPerSpawner.Count})가 일치하지 않습니다. → {lines[i]}");
                    continue;
                }

                previewList.Add(new EnemySpawnDataPreview
                {
                    enemyIndexesPerSpawner = enemyIndexesPerSpawner,
                    spawnerCount = spawnerCount,
                    minSpawn = minSpawn,
                    maxSpawn = maxSpawn
                });
            }
            else
            {
                Debug.LogWarning($"CSV 데이터 파싱 실패: {lines[i]}");
            }
        }

        Debug.Log($"CSV 파싱 완료 - {previewList.Count}개 항목");
    }

    private void CreateSpawnerObjects()
    {
        if (previewList.Count == 0)
        {
            Debug.LogWarning("먼저 CSV를 불러오세요.");
            return;
        }

        if (monsterDB == null || monsterDB.monsters == null)
        {
            Debug.LogError("MonsterDB ScriptableObject를 지정해주세요.");
            return;
        }

        GameObject masterGroup = new GameObject("AllWaves");
        Undo.RegisterCreatedObjectUndo(masterGroup, "Create AllWaves Group");

        for (int i = 0; i < previewList.Count; i++)
        {
            var p = previewList[i];
            GameObject waveGroup = new GameObject($"Wave_{i + 1}");
            waveGroup.transform.parent = masterGroup.transform;
            Undo.RegisterCreatedObjectUndo(waveGroup, $"Create Wave_{i + 1}");

            for (int j = 0; j < p.spawnerCount; j++)
            {
                GameObject spawner = new GameObject($"Spawner_{j + 1}");
                spawner.transform.parent = waveGroup.transform;
                Undo.RegisterCreatedObjectUndo(spawner, $"Create Spawner_{j + 1}");

                EnemySpawner enemySpawner = spawner.AddComponent<EnemySpawner>();
                enemySpawner.enemyPrefabs = new List<GameObject>();

                if (j < p.enemyIndexesPerSpawner.Count)
                {
                    foreach (int index in p.enemyIndexesPerSpawner[j])
                    {
                        if (index >= 0 && index < monsterDB.monsters.Count)
                        {
                            enemySpawner.enemyPrefabs.Add(monsterDB.monsters[index]);
                        }
                        else
                        {
                            Debug.LogWarning($"MonsterDB에 존재하지 않는 인덱스: {index}");
                        }
                    }
                }

                enemySpawner.minSpawnCount = p.minSpawn;
                enemySpawner.maxSpawnCount = p.maxSpawn;
            }
        }

        Debug.Log("Wave 그룹 및 스포너 생성 완료!");
    }
}

[System.Serializable]
public class EnemySpawnDataPreview
{
    public List<List<int>> enemyIndexesPerSpawner;
    public int spawnerCount;
    public int minSpawn;
    public int maxSpawn;
}