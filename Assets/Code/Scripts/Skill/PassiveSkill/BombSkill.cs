using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombSkill : MonoBehaviour
{
    [Header("ÆøÅº ÇÁ¸®ÆÕ")]
    public GameObject bombPrefab;

    [Header("ÀÚµ¿ ¹ß»ç ¼³Á¤")]
    public float bombShootInterval = 3f;
    public float bombDetectRange = 10f;

    private float bombTimer = 0f;

    void Update()
    {
        HandleAutoBomb();
    }

    void HandleAutoBomb()
    {
        if (bombPrefab == null) return;

        bombTimer += Time.deltaTime;
        if (bombTimer >= bombShootInterval)
        {
            GameObject target = FindClosestEnemy();
            if (target != null)
            {
                Vector2 dir = (target.transform.position - transform.position).normalized;
                ShootBomb(dir);
                bombTimer = 0f;
            }
        }
    }

    void ShootBomb(Vector2 direction)
    {
        GameObject bomb = Instantiate(bombPrefab, transform.position, Quaternion.identity);
        bomb.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

        BombProjectile bombScript = bomb.GetComponent<BombProjectile>();
        if (bombScript != null)
        {
            bombScript.Init(direction);
        }
    }

    GameObject FindClosestEnemy()
    {
        string[] enemyTags = { "Enemy", "DashEnemy", "LongRangeEnemy", "PotionEnemy" };
        GameObject closest = null;
        float minDist = Mathf.Infinity;

        foreach (string tag in enemyTags)
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject enemy in enemies)
            {
                float dist = Vector3.Distance(transform.position, enemy.transform.position);
                if (dist < minDist && dist <= bombDetectRange)
                {
                    minDist = dist;
                    closest = enemy;
                }
            }
        }

        return closest;
    }
}
