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
            portalSpawned = true;
            StartCoroutine(ShakeAndSpawnPortal());
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

    IEnumerator ShakeAndSpawnPortal()
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.portalSpawnSound);

        if (GameManager.Instance.cameraShake != null)
        {
            for (int i = 0; i < 7; i++)
            {
                GameManager.Instance.cameraShake.GenerateImpulse();
                yield return new WaitForSeconds(0.1f);
            }
            SpawnPortal();
        }
    }

    bool AreAllEnemiesDead()
    {
        GameObject[] allEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject[] dashEnemies = GameObject.FindGameObjectsWithTag("DashEnemy");
        GameObject[] longRangeEnemies = GameObject.FindGameObjectsWithTag("LongRangeEnemy");
        GameObject[] potionEnemies = GameObject.FindGameObjectsWithTag("PotionEnemy");

        int totalEnemies = allEnemies.Length + dashEnemies.Length + longRangeEnemies.Length + potionEnemies.Length;
        return totalEnemies == 0;
    }

    void SpawnPortal()
    {
        Vector2 portalPos = portalPosition;
        WaveData currentWaveData = (currentWave - 1 >= 0 && currentWave - 1 < waveDataList.Count)
            ? waveDataList[currentWave - 1] : null;

        if (currentWaveData != null && currentWaveData.isShopMap)
        {
            if (shopPortalPrefab != null)
                Instantiate(shopPortalPrefab, portalPos, Quaternion.identity);
        }
        else
        {
            if (portalPrefab != null)
                Instantiate(portalPrefab, portalPos, Quaternion.identity);
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
        yield return null;
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

    /// <summary>
    /// 개별 경고 이펙트 표시
    /// </summary>
    void ShowWarningEffect(Vector2 pos)
    {
        if (warningEffectPrefab == null) return;

        GameObject warning = GameManager.Instance.poolManager.SpawnFromPool(
            warningEffectPrefab.name, pos, Quaternion.identity);

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

    IEnumerator SpawnWithWarning()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsShop())
            yield break;

        WaveData currentWaveData = waveDataList[currentWave - 1];
        if (currentWaveData == null || currentWaveData.MonsterLists.Count == 0)
            yield break;

        int spawnCount = currentWaveData.MonsterLists.Count;

        for (int i = 0; i < spawnCount; i++)
        {
            if (GameManager.Instance != null && GameManager.Instance.IsShop())
                yield break;

            float delay = 0f;
            if (i < currentWaveData.spawnDelays.Count)
                delay = currentWaveData.spawnDelays[i];
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            GameObject prefab = currentWaveData.MonsterLists[i];
            Vector2 spawnPos = Vector2.zero; // 필요 시 랜덤 위치 가능

            // 임시 오브젝트 생성 (비활성 상태)
            GameObject tempObj = Instantiate(prefab, spawnPos, Quaternion.identity);
            tempObj.SetActive(false);

            // 자식 중 적 태그를 가진 오브젝트 위치마다 경고 표시
            var allTransforms = tempObj.GetComponentsInChildren<Transform>();
            foreach (var t in allTransforms)
            {
                if (t == tempObj.transform) continue;
                if (IsEnemyTag(t.gameObject.tag))
                {
                    ShowWarningEffect(t.position);
                }
            }

            // 경고 시간 대기
            yield return new WaitForSeconds(warningDuration);

            // 실제 스폰
            GameManager.Instance.poolManager.SpawnFromPool(prefab.name, spawnPos, Quaternion.identity);
            Destroy(tempObj);

            Debug.Log($"[WaveManager] {currentWave} 웨이브 몬스터 스폰 완료: {i + 1}/{spawnCount}");
        }

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
