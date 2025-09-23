using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(fileName = "MiddleBoss1Stats", menuName = "Custom/Middle Boss1 Stats")]
public class MiddleBoss1Stats : ScriptableObject
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
        speed = 0f;
        maxHP = 30000;
        currentHP = maxHP;
        attack = 100;
    }

}