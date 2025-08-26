using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(fileName = "EnemyStats", menuName = "Custom/Enemy Stats")]
public class EnemyStats : ScriptableObject
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
        speed = 2.5f;
        maxHP = 600;
        currentHP = maxHP;
        attack = 100;
    }

}