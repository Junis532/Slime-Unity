using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(fileName = "TankerEnemyStats", menuName = "Custom/Tanker Enemy Stats")]
public class TankerEnemyStats : ScriptableObject
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
        maxHP = 3000;
        currentHP = maxHP;
        attack = 100;
    }

}