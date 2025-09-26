using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(fileName = "ExplosionEnemyStats", menuName = "Custom/Explosion Enemy Stats")]
public class ExplosionEnemyStats : ScriptableObject
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
        speed = 8f;
        maxHP = 1000;
        currentHP = maxHP;
        attack = 100;
    }

}