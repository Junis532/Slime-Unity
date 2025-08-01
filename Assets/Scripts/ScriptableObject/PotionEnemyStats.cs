using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(fileName = "PotionEnemyStats", menuName = "Custom/Potion Enemy Stats")]
public class PotionEnemyStats : ScriptableObject
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
        maxHP = 20;
        currentHP = maxHP;
        attack = 1;
    }

}