using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(fileName = "Boss1Stats", menuName = "Custom/Boss1 Stats")]
public class Boss1Stats : ScriptableObject
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
        maxHP = 1000;
        currentHP = maxHP;
        attack = 100;
    }

}