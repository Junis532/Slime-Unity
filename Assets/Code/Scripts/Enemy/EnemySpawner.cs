using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class EnemySpawner : MonoBehaviour
{
    [Header("스폰 가능한 적 종류")]
    public List<GameObject> enemyPrefabs = new List<GameObject>();

    [Header("경고 이펙트")]
    public GameObject warningEffectPrefab;

    [Header("스폰 범위 (원형 반경)")]
    public float spawnRadius = 3f;

    [Header("한 그룹당 스폰 개수")]
    public int minSpawnCount = 3;
    public int maxSpawnCount = 6;

    [Header("스폰 딜레이")]
    private float warningDuration = 1f; // 경고 이미지가 깜빡이는 시간

    private Coroutine spawnCoroutine;

    IEnumerator SpawnEnemyGroupWithWarning()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("Player 오브젝트를 찾을 수 없습니다!");
            yield break;
        }

        Vector2 centerPos = player.transform.position;

        int spawnCount = Random.Range(minSpawnCount, maxSpawnCount + 1);
        List<Vector2> spawnPositions = new List<Vector2>();

        for (int i = 0; i < spawnCount; i++)
        {
            Vector2 randomPos = centerPos + Random.insideUnitCircle * spawnRadius;
            spawnPositions.Add(randomPos);

            GameObject warning = Instantiate(warningEffectPrefab, randomPos, Quaternion.identity);
            SpriteRenderer sr = warning.GetComponent<SpriteRenderer>();
            sr.color = new Color(1, 0, 0, 0); // 투명하게 시작

            sr.DOFade(1f, 0.3f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutQuad);

            Destroy(warning, warningDuration);
        }

        yield return new WaitForSeconds(warningDuration);

        foreach (Vector2 pos in spawnPositions)
        {
            GameObject enemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
            Instantiate(enemyPrefab, pos, Quaternion.identity);
        }

        spawnCoroutine = null;

        Destroy(gameObject);
    }


    public void StartSpawning()
    {
        if (spawnCoroutine == null)
        {
            spawnCoroutine = StartCoroutine(SpawnEnemyGroupWithWarning());
        }
    }

    public void StopSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }
}
