using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "EnemySpawnData", menuName = "Spawn/EnemySpawnData")]
public class EnemySpawnData : ScriptableObject
{
    public List<List<int>> SpawnEnemyIndexes;
    public int SpawnerCount;
    public int MinSpawn;
    public int MaxSpawn;
}