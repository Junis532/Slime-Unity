using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class WaveManager : MonoBehaviour
{
    [Header("WaveData 리스트 (1 웨이브 = 1 WaveData)")]
    public List<WaveData> waveDataList;

    [Header("웨이브 스폰 설정")]
    public Transform playerTransform;
    public float spawnInterval = 5f;
    public float spawnRadius = 5f; // Player-centric spawn radius
    public GameObject warningEffectPrefab;
    public float warningDuration = 1f;
    //public TextMeshProUGUI waveText;
    public int currentWave = 1;

    [Header("★ 반드시 인스펙터/코드로 연결")]
    public JoystickDirectionIndicator3 playerSkillController;   // << 추가 >>

    private int lastValidSkillIndex = -1;

    // New: Define min/max spawn coordinates for the overall bounding box
    [Header("스폰 가능 전체 영역 (맵 경계)")]
    public float minMapX = -10f;
    public float maxMapX = 10f;
    public float minMapY = -6f;
    public float maxMapY = 6f;

    private Coroutine spawnCoroutine;

    [Header("맵 관련")]
    private GameObject currentMapInstance;

    [Header("포탈")]
    public GameObject portalPrefab;
    public GameObject shopPortalPrefab;
    public Vector2 portalOffset = new Vector2(0f, 2f);
    private bool portalSpawned = false;

    void Start()
    {
        if (playerSkillController == null)
            playerSkillController = FindFirstObjectByType<JoystickDirectionIndicator3>();
 
        ResetWave();
        StartSpawnLoop();
    }

    void Update()
    {
        if (!portalSpawned && GameManager.Instance.CurrentState == "Clear")
        {
            SpawnPortal();
            portalSpawned = true;
        }
    }

    void SpawnPortal()
    {
        if (playerTransform == null)
        {
            Debug.LogWarning("플레이어 트랜스폼이 연결되지 않았습니다.");
            return;
        }

        Vector2 portalPosition = (Vector2)playerTransform.position + portalOffset;

        WaveData currentWaveData = null;
        if (currentWave - 1 >= 0 && currentWave - 1 < waveDataList.Count)
        {
            currentWaveData = waveDataList[currentWave - 1];
        }

        if (currentWaveData != null && currentWaveData.isShopMap)
        {
            if (shopPortalPrefab == null)
            {
                Debug.LogWarning("상점 포탈 프리팹이 연결되지 않았습니다.");
                return;
            }
            Instantiate(shopPortalPrefab, portalPosition, Quaternion.identity);
            Debug.Log("[WaveManager] 클리어 상태 - 상점 포탈 생성됨");
        }
        else
        {
            if (portalPrefab == null)
            {
                Debug.LogWarning("포탈 프리팹이 연결되지 않았습니다.");
                return;
            }
            Instantiate(portalPrefab, portalPosition, Quaternion.identity);
            Debug.Log("[WaveManager] 클리어 상태 - 포탈 생성됨");
        }
    }


    public void ResetWave()
    {
        currentWave = 1;
        //UpdateWaveText();
    }

    //public void UpdateWaveText()
    //{
    //    if (waveText != null)
    //        waveText.text = $"WAVE {currentWave}";
    //}

    public void StartNextWave()
    {
        if (currentWave >= waveDataList.Count)
        {
            Debug.LogWarning("더 이상 웨이브가 없습니다.");
            return;
        }

        // 이전 맵 제거
        if (currentMapInstance != null)
        {
            Destroy(currentMapInstance);
            currentMapInstance = null;
        }

        // 새 맵 프리팹 인스턴스화
        WaveData waveData = waveDataList[currentWave];
        if (waveData.mapPrefab != null)
        {
            currentMapInstance = Instantiate(waveData.mapPrefab, Vector3.zero, Quaternion.identity);
        }

        currentWave++;
        //UpdateWaveText();
        UpdateEnemyHP();
        //StartCoroutine(SpawnWithWarning());
        if (ShopManager.Instance != null)
            ShopManager.Instance.ResetRerollPrice();

        GameManager.Instance.ChangeStateToGame();
        //RestartSpawnLoop();
    }

    bool IsEnemyTag(string tag)
    {
        return tag == "Enemy" || tag == "DashEnemy" || tag == "LongRangeEnemy" || tag == "PotionEnemy";
    }

    IEnumerator SpawnWithWarning()
    {
        // ★ 상점상태 체크: 상점이면 즉시 중단, 절대 스폰하지 않음
        if (GameManager.Instance != null && GameManager.Instance.IsShop())
            yield break;

        WaveData currentWaveData = waveDataList[currentWave - 1];
        if (currentWaveData == null || currentWaveData.skillMonsterLists.Count == 0)
        {
            Debug.LogWarning($"[WaveManager] 유효한 WaveData가 없습니다. currentWave = {currentWave}");
            yield break;
        }

        int currentSkillNumber = playerSkillController != null ? playerSkillController.CurrentUsingSkillIndex : 0;

        // 스킬이 유효하면 저장하고, 아니면 마지막 유효 스킬을 사용
        if (currentSkillNumber > 0)
        {
            lastValidSkillIndex = currentSkillNumber;
        }
        else
        {
            if (lastValidSkillIndex <= 0)
            {
                Debug.LogWarning("[WaveManager] 현재 스킬이 없고 이전 사용된 스킬도 없음: 몬스터 스폰 스킵");
                yield break;
            }
            currentSkillNumber = lastValidSkillIndex;
        }

        int skillListIndex = currentSkillNumber - 1;
        if (skillListIndex < 0 || skillListIndex >= currentWaveData.skillMonsterLists.Count)
        {
            Debug.LogWarning($"[WaveManager] 스킬({currentSkillNumber})에 매핑된 몬스터 리스트 없음");
            yield break;
        }

        var monsterList = currentWaveData.skillMonsterLists[skillListIndex].monsters;
        if (monsterList == null || monsterList.Count == 0)
        {
            Debug.LogWarning($"[WaveManager] 스킬({currentSkillNumber})의 랜덤 몬스터 리스트가 비어있음");
            yield break;
        }

        int spawnCount = Random.Range(currentWaveData.minSpawnCount, currentWaveData.maxSpawnCount + 1);
        List<GameObject> spawnMonsters = new List<GameObject>();
        List<Vector2> spawnPositions = new List<Vector2>();

        for (int i = 0; i < spawnCount; i++)
        {
            // 각 몬스터는 랜덤
            GameObject selected = monsterList[Random.Range(0, monsterList.Count)];
            spawnMonsters.Add(selected);

            // Calculate a position around the player
            Vector2 spawnOffset = Random.insideUnitCircle.normalized * spawnRadius;
            Vector2 potentialSpawnPosition = (Vector2)playerTransform.position + spawnOffset;

            // Clamp the potential spawn position to the defined map boundaries
            float finalX = Mathf.Clamp(potentialSpawnPosition.x, minMapX, maxMapX);
            float finalY = Mathf.Clamp(potentialSpawnPosition.y, minMapY, maxMapY);
            Vector2 finalSpawnPosition = new Vector2(finalX, finalY);

            spawnPositions.Add(finalSpawnPosition);
        }

        // 경고 이펙트 (상점 진입시 중간에 코루틴 종료되어도 스폰 안됨)
        for (int i = 0; i < spawnPositions.Count; i++)
        {
            // 상점상태 즉시 중지
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

        // 상점 진입시 혹시나 몬스터 스폰 중단됨(안전장치)
        if (GameManager.Instance != null && GameManager.Instance.IsShop())
            yield break;

        // 몬스터 스폰 풀매니저로
        // 몬스터 스폰 (Instantiate 방식)
        for (int i = 0; i < spawnPositions.Count; i++)
        {
            GameObject prefab = spawnMonsters[i];
            Vector2 spawnPos = spawnPositions[i];
            Instantiate(prefab, spawnPos, Quaternion.identity);
        }


        Debug.Log($"[WaveManager] {currentWave} 웨이브 몬스터 스폰 완료: {spawnCount}마리, 스킬({currentSkillNumber})에 매핑된 몬스터 수: {monsterList.Count}개");
    }

    // 경고 이펙트 풀로 반환 (DOTween 정리 포함)
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
        int prevEnemyHP = GameManager.Instance.enemyStats.maxHP;
        int nextEnemyHP = Mathf.FloorToInt(prevEnemyHP + prevEnemyHP * waveFactorEnemy);
        GameManager.Instance.enemyStats.maxHP = nextEnemyHP;
        GameManager.Instance.enemyStats.currentHP = nextEnemyHP;
        int prevDashHP = GameManager.Instance.dashEnemyStats.maxHP;
        int nextDashHP = Mathf.FloorToInt(prevDashHP + prevDashHP * waveFactorEnemy);
        GameManager.Instance.dashEnemyStats.maxHP = nextDashHP;
        GameManager.Instance.dashEnemyStats.currentHP = nextDashHP;
        int prevLongRangeHP = GameManager.Instance.longRangeEnemyStats.maxHP;
        int nextLongRangeHP = Mathf.FloorToInt(prevLongRangeHP + prevLongRangeHP * waveFactorLongRange);
        GameManager.Instance.longRangeEnemyStats.maxHP = nextLongRangeHP;
        GameManager.Instance.longRangeEnemyStats.currentHP = nextLongRangeHP;
        int prevPotionHP = GameManager.Instance.potionEnemyStats.maxHP;
        int nextPotionHP = Mathf.FloorToInt(prevPotionHP + prevPotionHP * waveFactorLongRange);
        GameManager.Instance.potionEnemyStats.maxHP = nextPotionHP;
        GameManager.Instance.potionEnemyStats.currentHP = nextPotionHP;
    }

    IEnumerator SpawnerLoopRoutine()
    {
        float initialDelay = 1f;
        yield return new WaitForSeconds(initialDelay);
        while (true)
        {
            // ★ Shop 상태 진입시 즉시 중지 (안전장치)
            if (GameManager.Instance != null && GameManager.Instance.IsShop())
                yield break;
            yield return StartCoroutine(SpawnWithWarning());
            yield return new WaitForSeconds(spawnInterval);
        }
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
    void RestartSpawnLoop()
    {
        StopSpawnLoop();
        StartSpawnLoop();
    }
}