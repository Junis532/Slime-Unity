using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "MonsterDB", menuName = "Database/MonsterDB")]
public class MonsterDB : ScriptableObject
{
    public List<GameObject> monsters;
}
