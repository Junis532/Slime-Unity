using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MucusArea : MonoBehaviour
{
    private float slowRatio = 0.5f; // 속도를 50%로 줄임
    private bool isSlowing = false;

    // 범위 내 느려진 적들 저장
    private List<EnemyBase> enemiesInRange = new List<EnemyBase>();

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy") || other.CompareTag("DashEnemy") ||
            other.CompareTag("LongRangeEnemy") || other.CompareTag("PotionEnemy"))
        {
            EnemyBase enemy = other.GetComponent<EnemyBase>(); // 공통 부모 클래스 또는 인터페이스
            if (enemy != null && !enemiesInRange.Contains(enemy))
            {
                enemiesInRange.Add(enemy);
                enemy.speed = enemy.originalSpeed * slowRatio;

                if (!isSlowing)
                {
                    StartCoroutine(SlowOverTime());
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Enemy") || other.CompareTag("DashEnemy") ||
            other.CompareTag("LongRangeEnemy") || other.CompareTag("PotionEnemy"))
        {
            EnemyBase enemy = other.GetComponent<EnemyBase>();
            if (enemy != null && enemiesInRange.Contains(enemy))
            {
                enemy.speed = enemy.originalSpeed; // 원래 속도로 복원
                enemiesInRange.Remove(enemy);

                if (enemiesInRange.Count == 0)
                {
                    isSlowing = false;
                    StopCoroutine(SlowOverTime());
                }
            }
        }
    }

    private IEnumerator SlowOverTime()
    {
        isSlowing = true;

        while (isSlowing)
        {
            enemiesInRange.RemoveAll(e => e == null);

            foreach (var enemy in enemiesInRange)
            {
                enemy.speed = enemy.originalSpeed * slowRatio; // 속도 유지
            }

            yield return new WaitForSeconds(0.5f);
        }
    }
}