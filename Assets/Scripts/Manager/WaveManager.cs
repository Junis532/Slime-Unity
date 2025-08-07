using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;
using NavMeshPlus.Components;

public class WaveManager : MonoBehaviour
{
    [Header("WaveData 리스트 (1 웨이브 = 1 WaveData)")]
    public List<WaveData> waveDataList;

    [Header("웨이브 스폰 설정")]
    public Transform playerTransform;
    public float spawnInterval = 5f;
    public GameObject warningEffectPrefab;
    public float warningDuration = 1f;
    public int currentWave = 1;

    private Coroutine spawnCoroutine;

    [Header("맵 관련")]
    private GameObject currentMapInstance;
    private BoxCollider2D mapBoundary;

    [Header("포탈")]
    public GameObject portalPrefab;
    public GameObject shopPortalPrefab;
    public Vector2 portalPosition = new Vector2(8f, 0f);
    private bool portalSpawned = false;

    private bool hasSpawned = false;

    void Start()
    {
        ResetWave();
        StartNextWave(); // 첫 웨이브 시작

    }

    void Update()
    {
        if (!portalSpawned && GameManager.Instance.CurrentState == "Clear")
        {
            SpawnPortal();
            portalSpawned = true;
        }

        // ✅ 적이 모두 죽었고 아직 Clear 상태가 아니라면
        if (hasSpawned && GameManager.Instance.CurrentState == "Game")
        {
            if (AreAllEnemiesDead())
            {
                GameManager.Instance.ChangeStateToClear();
                Debug.Log("[WaveManager] 모든 적 처치 -> 상태 Clear로 전환");
            }
        }
    }

    bool AreAllEnemiesDead()
    {
        // 현재 씬에 남아있는 적 GameObject가 있는지 확인
        GameObject[] allEnemies = GameObject.FindGameObjectsWithTag("Enemy");

        // 특수한 적들 (DashEnemy 등)도 검사
        GameObject[] dashEnemies = GameObject.FindGameObjectsWithTag("DashEnemy");
        GameObject[] longRangeEnemies = GameObject.FindGameObjectsWithTag("LongRangeEnemy");
        GameObject[] potionEnemies = GameObject.FindGameObjectsWithTag("PotionEnemy");

        int totalEnemies = allEnemies.Length + dashEnemies.Length + longRangeEnemies.Length + potionEnemies.Length;

        return totalEnemies == 0;
    }


    void SpawnPortal()
    {
        Vector2 portalPosition = new Vector2(8f, 0f); // 항상 x=8, y=0에 포탈 생성

        WaveData currentWaveData = (currentWave - 1 >= 0 && currentWave - 1 < waveDataList.Count)
            ? waveDataList[currentWave - 1] : null;

        if (currentWaveData != null && currentWaveData.isShopMap)
        {
            if (shopPortalPrefab != null)
                Instantiate(shopPortalPrefab, portalPosition, Quaternion.identity);
        }
        else
        {
            if (portalPrefab != null)
                Instantiate(portalPrefab, portalPosition, Quaternion.identity);
        }
    }

    public void ResetWave()
    {
        currentWave = 0;
        hasSpawned = false;
    }

    public void StartNextWave()
    {
        StopSpawnLoop();

        if (currentWave >= waveDataList.Count)
        {
            GameManager.Instance.ChangeStateToClear();
            return;
        }

        if (currentMapInstance != null)
        {
            Destroy(currentMapInstance);
            currentMapInstance = null;
        }

        portalSpawned = false;
        hasSpawned = false;

        WaveData waveData = waveDataList[currentWave];
        if (waveData.mapPrefab != null)
        {
            currentMapInstance = Instantiate(waveData.mapPrefab, Vector3.zero, Quaternion.identity);
            mapBoundary = currentMapInstance.GetComponentInChildren<BoxCollider2D>();

            // ✅ NavMesh 베이크 실행
            StartCoroutine(BakeNavMeshDelayed(currentMapInstance));
        }

        currentWave++;
        UpdateEnemyHP();

        if (GameManager.Instance.shopManager != null)
            GameManager.Instance.shopManager.ResetRerollPrice();

        GameManager.Instance.ChangeStateToGame();
        StartSpawnLoop();
    }

    IEnumerator BakeNavMeshDelayed(GameObject mapInstance)
    {
        yield return null; // 한 프레임 대기

        NavMeshSurface surface = mapInstance.GetComponentInChildren<NavMeshSurface>();
        if (surface != null)
        {
            surface.BuildNavMesh();
            Debug.Log($"[WaveManager] {currentWave} 웨이브 NavMesh 베이크 완료");
        }
        else
        {
            Debug.LogWarning("[WaveManager] NavMeshSurface를 찾을 수 없습니다.");
        }
    }

    bool IsEnemyTag(string tag)
    {
        return tag == "Enemy" || tag == "DashEnemy" || tag == "LongRangeEnemy" || tag == "PotionEnemy";
    }

    IEnumerator SpawnWithWarning()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsShop())
            yield break;

        WaveData currentWaveData = waveDataList[currentWave - 1];
        if (currentWaveData == null || currentWaveData.MonsterLists.Count == 0)
            yield break;

        List<Vector2> spawnPositions = new List<Vector2>();
        List<GameObject> spawnMonsters = new List<GameObject>();
        int spawnCount = currentWaveData.MonsterLists.Count;

        for (int i = 0; i < spawnCount; i++)
        {
            GameObject selected = currentWaveData.MonsterLists[Random.Range(0, currentWaveData.MonsterLists.Count)];
            spawnMonsters.Add(selected);
            spawnPositions.Add(Vector2.zero);
        }

        for (int i = 0; i < spawnPositions.Count; i++)
        {
            if (GameManager.Instance != null && GameManager.Instance.IsShop())
                yield break;

            GameObject prefab = spawnMonsters[i];
            Vector2 spawnPos = spawnPositions[i];

            GameObject tempObj = Instantiate(prefab, spawnPos, Quaternion.identity);
            tempObj.SetActive(false);

            bool hasRealEnemy = false;
            var allMonsters = tempObj.GetComponentsInChildren<Transform>();
            foreach (var t in allMonsters)
            {
                if (t == tempObj.transform) continue;
                if (IsEnemyTag(t.gameObject.tag) && warningEffectPrefab != null)
                {
                    GameObject warning = GameManager.Instance.poolManager.SpawnFromPool(
                        warningEffectPrefab.name, t.position, Quaternion.identity);
                    if (warning != null)
                    {
                        SpriteRenderer sr = warning.GetComponent<SpriteRenderer>();
                        if (sr != null)
                        {
                            sr.color = new Color(1, 0, 0, 0);
                            sr.DOFade(1f, 0.3f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutQuad);
                        }
                        StartCoroutine(ReturnWarningToPool(warning, warningDuration));
                    }
                    hasRealEnemy = true;
                }
            }
            if (!hasRealEnemy && IsEnemyTag(tempObj.tag) && warningEffectPrefab != null)
            {
                GameObject warning = GameManager.Instance.poolManager.SpawnFromPool(
                    warningEffectPrefab.name, spawnPos, Quaternion.identity);
                if (warning != null)
                {
                    SpriteRenderer sr = warning.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.color = new Color(1, 0, 0, 0);
                        sr.DOFade(1f, 0.3f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutQuad);
                    }
                    StartCoroutine(ReturnWarningToPool(warning, warningDuration));
                }
            }
            Destroy(tempObj);
        }

        yield return new WaitForSeconds(warningDuration);

        if (GameManager.Instance != null && GameManager.Instance.IsShop())
            yield break;

        for (int i = 0; i < spawnPositions.Count; i++)
        {
            GameObject prefab = spawnMonsters[i];
            Vector2 spawnPos = spawnPositions[i];

            GameManager.Instance.poolManager.SpawnFromPool(prefab.name, spawnPos, Quaternion.identity);
        }

        Debug.Log($"[WaveManager] {currentWave} 웨이브 몬스터 스폰 완료: {spawnCount}마리");

        hasSpawned = true;
    }

    IEnumerator ReturnWarningToPool(GameObject warning, float duration)
    {
        yield return new WaitForSeconds(duration);
        SpriteRenderer sr = warning.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.DOKill();
        GameManager.Instance.poolManager.ReturnToPool(warning);
    }

    void UpdateEnemyHP()
    {
        float waveFactorEnemy = 0.07f + (currentWave / 30000f);
        float waveFactorLongRange = 0.068f + (currentWave / 30000f);

        GameManager.Instance.enemyStats.maxHP = ApplyHPScale(GameManager.Instance.enemyStats.maxHP, waveFactorEnemy);
        GameManager.Instance.dashEnemyStats.maxHP = ApplyHPScale(GameManager.Instance.dashEnemyStats.maxHP, waveFactorEnemy);
        GameManager.Instance.longRangeEnemyStats.maxHP = ApplyHPScale(GameManager.Instance.longRangeEnemyStats.maxHP, waveFactorLongRange);
        GameManager.Instance.potionEnemyStats.maxHP = ApplyHPScale(GameManager.Instance.potionEnemyStats.maxHP, waveFactorLongRange);
    }

    int ApplyHPScale(int baseHP, float factor)
    {
        int newHP = Mathf.FloorToInt(baseHP + baseHP * factor);
        return newHP;
    }

    IEnumerator SpawnerLoopRoutine()
    {
        yield return new WaitForSeconds(1f);

        if (hasSpawned || (GameManager.Instance != null && GameManager.Instance.IsShop()))
            yield break;

        yield return StartCoroutine(SpawnWithWarning());
    }

    public void StartSpawnLoop()
    {
        if (spawnCoroutine == null)
            spawnCoroutine = StartCoroutine(SpawnerLoopRoutine());
    }

    public void StopSpawnLoop()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }
}
