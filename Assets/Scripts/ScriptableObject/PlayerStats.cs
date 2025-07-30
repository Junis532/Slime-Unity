using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(fileName = "PlayerStats", menuName = "Custom/Player Stats")]
public class PlayerStats : ScriptableObject
{
    public int id;
    public int level;
    public int coin;
    public int maxHP;
    public int currentHP;
    public float speed;
    public float attack;
    public float magic;
    public float attackSpeed;
    public int criticalChance;
    public int criticalValue;
    public float defense;
    public float drainLife;
    public float Luck;
    //public Vector3 size = new Vector3(1f, 1f, 1f);

    // PlayerStats 초기화 함수
    public void ResetStats()
    {
        //size = new Vector3(1f, 1f, 1f);
        speed = 3.5f;
        coin = 0;
        maxHP = 10;
        currentHP = maxHP;
        attack = 20;
    }
}