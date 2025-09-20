using UnityEngine;

public class MucusSkill : MonoBehaviour
{
    public GameObject mucusPrefab;
    public Transform firePoint;
    public float fireCooldown = 3f;
    private float timer = 0f;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= fireCooldown)
        {
            GameObject target = FindClosestEnemy();
            if (target != null)
            {
                ShootMucus(target.transform.position);
                timer = 0f;
            }
        }
    }

    void ShootMucus(Vector3 targetPos)
    {
        GameObject mucus = Instantiate(mucusPrefab, firePoint.position, Quaternion.identity);
        mucus.GetComponent<MucusProjectile>().Init(targetPos);
    }

    GameObject FindClosestEnemy()
    {
        // 적 태그 배열 설정
        string[] enemyTags = { "Enemy", "DashEnemy", "LongRangeEnemy", "PotionEnemy" };

        GameObject closest = null;
        float minDist = Mathf.Infinity;

        foreach (string tag in enemyTags)
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject enemy in enemies)
            {
                float dist = Vector3.Distance(transform.position, enemy.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = enemy;
                }
            }
        }
        return closest;
    }
}
