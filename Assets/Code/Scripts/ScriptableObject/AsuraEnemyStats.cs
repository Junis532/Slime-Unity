using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(fileName = "AsuraEnemyStats", menuName = "Custom/Asura Enemy Stats")]
public class AsuraEnemyStats : ScriptableObject
{
    public int id;
    public int level;
    public int maxHP;
    public int currentHP;
    public float speed;
    public int attack;
    public int magic;
    public float attackSpeed;
    public float defense;


    public void ResetStats()
    {
        speed = 3f;
        maxHP = 1000000;
        currentHP = maxHP;
        attack = 100;
    }

}