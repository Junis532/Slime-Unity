using System.Collections;
using UnityEngine;

public class PoisonSkill : MonoBehaviour
{
    public GameObject poisonPrefab;           // 생성할 독 프리팹 (PoolManager에 등록 필요)
    public float spawnInterval = 15f;         // 생성 간격
    public float poisonLifetime = 10f;        // 독 지속 시간
    public Vector3 spawnOffset = Vector3.zero; // 발 밑 위치 조정

    private Coroutine spawnCoroutine;

    void Start()
    {
        if (poisonPrefab != null)
        {
            spawnCoroutine = StartCoroutine(SpawnPoisonRoutine());
        }
        else
        {
            Debug.LogWarning("poisonPrefab이 설정되지 않았습니다.");
        }
    }

    private IEnumerator SpawnPoisonRoutine()
    {
        while (true)
        {
            Vector3 spawnPos = transform.position + spawnOffset;

            // Instantiate → PoolManager 사용
            GameObject poison = PoolManager.Instance.SpawnFromPool(poisonPrefab.name, spawnPos, Quaternion.identity);

            // 초기화
            PoisonDamage poisonDamage = poison.GetComponent<PoisonDamage>();
            if (poisonDamage != null)
            {
                poisonDamage.Init();
            }

            // Destroy → 일정 시간 뒤 ReturnToPool
            StartCoroutine(ReturnPoisonToPool(poison, poisonLifetime));

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private IEnumerator ReturnPoisonToPool(GameObject poison, float time)
    {
        yield return new WaitForSeconds(time);
        PoolManager.Instance.ReturnToPool(poison);
    }

    void OnDisable()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }
}
