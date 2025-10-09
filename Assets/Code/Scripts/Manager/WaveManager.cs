using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.AI;

[System.Serializable]
public class RoomWaveData
{
    [Header("웨이브 정보")]
    public string waveName = "Wave 1";
    public List<GameObject> enemyPrefabs;
    public float waveDelay = 2f; // 웨이브 시작 전 대기 시간
}

[System.Serializable]
public class RoomData
{
    public string roomName;
    public GameObject roomPrefab;

    [Header("Room 판정용 Collider")]
    public Collider2D roomCollider;

    [Header("Camera Confiner Collider")]
    public Collider2D cameraCollider;

    [Header("웨이브 시스템 설정")]
    public List<RoomWaveData> waves = new List<RoomWaveData>(); // 웨이브별 적 설정
    public List<MovingWall> movingWalls;

    [HideInInspector]
    public bool activated = false;

    [Header("카메라 Follow 설정")]
    public bool CameraFollow = true;

    [Header("카메라 연출 설정")]
    public bool enableZoomInSequence = true;        // 줌인 연출 사용 여부
    public bool zoomInCameraFollow = false;         // 줌인 시 플레이어 중심으로 이동 여부
    public float zoomInDelay = 0.8f;               // 줌인 시작 전 대기 시간
    public float zoomInDuration = 1.2f;            // 줌인 지속 시간
    public float zoomInTargetSize = 5.5f;          // 줌인 목표 크기

    [Header("이벤트 씬 설정")]
    public bool eventSceneEnabled = false;
    public Transform eventStartPos;
    public Transform eventEndPos;
    public GameObject eventObjectPrefab;
    public float eventMoveDuration = 3f;

    [Header("방 시작 시 기존 적 제거 여부")]
    public bool clearPreviousEnemies = true; // Room별로 설정 가능
}

public class WaveManager : MonoBehaviour
{
    [Header("모든 방 데이터")]
    public List<RoomData> rooms;

    [Header("플레이어")]
    public Transform playerTransform;

    [Header("카메라")]
    public CinemachineCamera cineCamera;
    public float cameraMoveDuration = 0.5f;

    [Header("경고 이펙트")]
    public GameObject warningEffectPrefab;
    public float warningDuration = 1f;

    [Header("문 프리팹 부모")]
    public GameObject doorParentPrefab;

    [Header("문 애니메이션 프리팹 부모")]
    public GameObject doorAnimationParentPrefab;

    [Header("스폰 관련")]
    public float spawnStop = 0f;

    [Tooltip("방 시작 시 기존 방 적을 모두 제거할지 여부")]
    public bool clearPreviousEnemies = true;

    private List<DoorController> allDoors = new List<DoorController>();
    private List<DoorAnimation> allDoorAnimations = new List<DoorAnimation>();
    private RoomData currentRoom;
    private bool cleared = false;
    private bool isSpawning = false;
    private bool isFirstRoom = true;
    private bool isEventRunning = false;
    
    [Header("웨이브 진행 상태")]
    private int currentWaveIndex = 0;
    private bool isWaveActive = false;

    void Start()
    {
        if (doorParentPrefab != null)
            allDoors.AddRange(doorParentPrefab.GetComponentsInChildren<DoorController>(true));
        if (doorAnimationParentPrefab != null)
            allDoorAnimations.AddRange(doorAnimationParentPrefab.GetComponentsInChildren<DoorAnimation>(true));
    }

    void Update()
    {
        if (!isSpawning && !isEventRunning)
        {
            RoomData room = GetPlayerRoom();
            if (room != null && room != currentRoom)
            {
                // 1. 먼저 새로운 방의 Confiner를 즉시 적용 (연출 전에)
                ApplyCameraConfiner(room);
                
                // 2. Follow 해제
                if (cineCamera != null) cineCamera.Follow = null;

                currentRoom = room;
                StartCoroutine(MoveCameraToRoomAndStart(room));
            }
        }
    }

    public void SetAllEnemiesAI(bool enabled)
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        foreach (var obj in enemies)
        {
            TurretEnemy_PlayerTracking enemyScript = obj.GetComponent<TurretEnemy_PlayerTracking>();
            if (enemyScript != null)
                enemyScript.AIEnabled = enabled;

            Enemy enemyBase = obj.GetComponent<Enemy>();
            if (enemyBase != null)
            {
                if (enabled)
                    enemyBase.EnableAI();
                else
                    enemyBase.DisableAI();
            }
        }
    }

    IEnumerator RunEventScene(RoomData room)
    {
        if (!room.eventSceneEnabled || room.eventObjectPrefab == null ||
            room.eventStartPos == null || room.eventEndPos == null)
            yield break;

        isEventRunning = true;

        // 이벤트 오브젝트 생성
        GameObject eventObj = Instantiate(room.eventObjectPrefab, room.eventStartPos.position, Quaternion.identity);

        // 카메라를 이벤트 중심으로 이동 (플레이어 비활성화)
        cineCamera.Follow = eventObj.transform;
        PlayerController playerCtrl = playerTransform.GetComponent<PlayerController>();
        if (playerCtrl != null) playerCtrl.canMove = false;

        // 이동 연출 (DOTween)
        eventObj.transform.DOMove(room.eventEndPos.position, room.eventMoveDuration)
            .SetEase(Ease.InOutSine);

        // 이동 시간 대기
        yield return new WaitForSeconds(room.eventMoveDuration);

        // 이벤트 종료 처리
        Destroy(eventObj);
        cineCamera.Follow = null; // 다시 제어권 복귀

        isEventRunning = false;
    }


    IEnumerator MoveCameraToRoomAndStart(RoomData room)
    {
        if (room == null || room.cameraCollider == null)
        {
            Debug.LogWarning("Room or cameraCollider is null!");
            yield break;
        }

        // 1. 현재 카메라 위치 저장 (안전장치)
        Vector3 currentCameraPos = cineCamera.transform.position;
        
        // 2. 목표 방 중심 위치 계산
        Vector3 roomCenter = room.cameraCollider.bounds.center;
        roomCenter.z = currentCameraPos.z;

        // 3. Follow 해제 (Confiner는 이미 Update에서 설정됨)
        cineCamera.Follow = null;

        PlayerController playerCtrl = playerTransform.GetComponent<PlayerController>();
        if (playerCtrl != null) playerCtrl.canMove = false;
        cleared = false;
        CloseDoors();

        SetAllEnemiesAI(false);
        SetAllBulletSpawnersActive(false);

        // ✅ [1단계] 이벤트씬 먼저 실행
        if (room.eventSceneEnabled)
        {
            Debug.Log($"이벤트씬 시작: {room.roomName}");
            yield return StartCoroutine(RunEventScene(room));
            Debug.Log("이벤트씬 종료");
        }

        // -------------------
        // 카메라 줌 연출 (줌아웃 + 줌인)
        // -------------------
        if (room.enableZoomInSequence)
        {
            // -----------------
            // 1. 줌아웃 (카메라 Collider 전체 보여주기)
            // ----------------
            Camera cam = Camera.main;
            if (cam != null)
            {
                Bounds bounds = room.cameraCollider.bounds;
                float screenRatio = (float)Screen.width / Screen.height;

                // 세로 기준 OrthographicSize
                float targetOrthoSize = bounds.size.y / 2f;

                // 가로가 부족하면 세로를 늘려서 가로 맞춤
                float camHalfWidth = targetOrthoSize * screenRatio;
                if (camHalfWidth < bounds.size.x / 2f)
                {
                    targetOrthoSize = bounds.size.x / 2f / screenRatio;
                }

                // 최소/최대 제한
                targetOrthoSize = Mathf.Clamp(targetOrthoSize, 3f, 12f);

                // DOTween으로 카메라 이동 및 줌 적용
                Sequence zoomOutSeq = DOTween.Sequence();
                zoomOutSeq.Append(cineCamera.transform.DOMove(
                    new Vector3(bounds.center.x, bounds.center.y, cineCamera.transform.position.z),
                    cameraMoveDuration
                ).SetEase(Ease.InOutSine));
                zoomOutSeq.Join(DOTween.To(() => cam.orthographicSize, x => cam.orthographicSize = x, targetOrthoSize, 0.6f).SetEase(Ease.InOutSine));
                zoomOutSeq.Join(DOTween.To(() => cineCamera.Lens.OrthographicSize, x => cineCamera.Lens.OrthographicSize = x, targetOrthoSize, 0.6f).SetEase(Ease.InOutSine));
                yield return zoomOutSeq.WaitForCompletion();
            }

            // -------------------
            // 2. 줌인 연출 (설정에 따라 실행)
            // -------------------
            yield return new WaitForSeconds(room.zoomInDelay);

            if (room.zoomInCameraFollow && room.cameraCollider != null)
            {
                // 플레이어 중심으로 줌인
                Bounds camBounds = room.cameraCollider.bounds;
                Vector3 targetPos = playerTransform.position;
                targetPos.z = cineCamera.transform.position.z;

                float camHalfHeight = room.zoomInTargetSize;
                float camHalfWidth = camHalfHeight * Camera.main.aspect;

                float minX = camBounds.min.x + camHalfWidth;
                float maxX = camBounds.max.x - camHalfWidth;
                float minY = camBounds.min.y + camHalfHeight;
                float maxY = camBounds.max.y - camHalfHeight;

                targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
                targetPos.y = Mathf.Clamp(targetPos.y, minY, maxY);

                Sequence zoomInSeq = DOTween.Sequence();
                zoomInSeq.Append(cineCamera.transform.DOMove(targetPos, room.zoomInDuration).SetEase(Ease.InOutSine));
                zoomInSeq.Join(DOTween.To(() => cineCamera.Lens.OrthographicSize, x => cineCamera.Lens.OrthographicSize = x, room.zoomInTargetSize, room.zoomInDuration).SetEase(Ease.InOutSine));
                yield return zoomInSeq.WaitForCompletion();
            }
            else
            {
                // 방 중앙에서 줌인
                Sequence zoomInSeq = DOTween.Sequence();
                zoomInSeq.Append(DOTween.To(() => cineCamera.Lens.OrthographicSize, x => cineCamera.Lens.OrthographicSize = x, room.zoomInTargetSize, room.zoomInDuration).SetEase(Ease.InOutSine));
                yield return zoomInSeq.WaitForCompletion();
            }
        }
        else
        {
            // 🔸 줌 기능이 비활성화된 경우: 바로 웨이브로 진입
            Debug.Log($"Room '{room.roomName}'은(는) 줌 연출이 비활성화됨 → 줌 건너뜀");
        }
        // enableZoomInSequence = false인 경우 줌인 연출 전체를 건너뜀

        // -------------------
        // 3. Follow 적용 + Confiner (튐 방지용)
        // -------------------
        // DOTween 종료 위치를 그대로 유지한 채 Follow만 적용
        Vector3 finalCamPos = cineCamera.transform.position;
        finalCamPos.z = cineCamera.transform.position.z; // Z 유지
        cineCamera.transform.position = finalCamPos;

        cineCamera.Follow = playerTransform;
        // Confiner는 이미 Update에서 설정되었으므로 재설정 불필요

        // -------------------
        // 4. 카메라 연출 완료 후 웨이브 시스템 시작
        // -------------------
        currentWaveIndex = 0;
        isWaveActive = false;

        SetAllEnemiesAI(true);
        DOVirtual.DelayedCall(1.5f, () => SetAllBulletSpawnersActive(true));
        if (playerCtrl != null) playerCtrl.canMove = true;

        if (!room.activated && room.movingWalls != null)
        {
            room.activated = true;
            foreach (var wall in room.movingWalls)
                wall.isActive = true;
        }

        // 웨이브 시스템 시작
        StartCoroutine(StartWaveSystem(room));
    }

    // 웨이브 시스템 시작
    IEnumerator StartWaveSystem(RoomData room)
    {
        if (room.waves == null || room.waves.Count == 0)
        {
            Debug.LogWarning($"Room {room.roomName}에 웨이브가 설정되지 않았습니다!");
            cleared = true;
            OpenDoors();
            yield break;
        }

        for (currentWaveIndex = 0; currentWaveIndex < room.waves.Count; currentWaveIndex++)
        {
            RoomWaveData currentWave = room.waves[currentWaveIndex];
            
            // 웨이브 시작 전 대기
            yield return new WaitForSeconds(currentWave.waveDelay);
            
            // 웨이브 시작 알림 (필요시 UI 표시)
            Debug.Log($"Starting {currentWave.waveName}");
            
            // 현재 웨이브 적들 소환
            yield return StartCoroutine(SpawnWaveEnemies(currentWave));
            
            // 현재 웨이브 적들이 모두 처치될 때까지 대기
            yield return StartCoroutine(WaitForWaveCleared());
            
            Debug.Log($"{currentWave.waveName} 클리어!");
        }

        // 모든 웨이브 완료
        cleared = true;
        
        // 방 클리어 효과
        if (GameManager.Instance.cameraShake != null)
        {
            for (int i = 0; i < 7; i++)
            {
                GameManager.Instance.cameraShake.GenerateImpulse();
                yield return new WaitForSeconds(0.1f);
            }
        }

        OpenDoors();

        if (room.movingWalls != null)
        {
            foreach (var wall in room.movingWalls)
                wall?.ResetWall();
        }
    }

    // 웨이브의 적들 소환
    IEnumerator SpawnWaveEnemies(RoomWaveData wave)
    {
        isWaveActive = true;
        
        // 경고 이펙트 먼저 표시
        foreach (var prefab in wave.enemyPrefabs)
        {
            foreach (Transform child in prefab.transform)
                ShowWarningEffect(child.position);
        }

        // 경고 이펙트가 완전히 끝날 때까지 대기
        yield return new WaitForSeconds(warningDuration);

        // 적 실제 소환
        foreach (var prefab in wave.enemyPrefabs)
        {
            GameObject tempObj = Instantiate(prefab, prefab.transform.position, prefab.transform.rotation);
            EnemyBase enemyBase = tempObj.GetComponent<EnemyBase>();
            if (enemyBase != null) enemyBase.CanMove = true; // 웨이브 시작 시 이동 가능
        }
    }

    // 현재 웨이브의 모든 적이 처치될 때까지 대기
    IEnumerator WaitForWaveCleared()
    {
        while (true)
        {
            int enemiesLeft =
                GameObject.FindGameObjectsWithTag("Enemy").Length +
                GameObject.FindGameObjectsWithTag("DashEnemy").Length +
                GameObject.FindGameObjectsWithTag("LongRangeEnemy").Length +
                GameObject.FindGameObjectsWithTag("PotionEnemy").Length;

            if (enemiesLeft == 0)
            {
                isWaveActive = false;
                yield break;
            }

            yield return new WaitForSeconds(0.5f);
        }
    }


    void SetAllBulletSpawnersActive(bool enabled)
    {
        BulletSpawner[] spawners = Object.FindObjectsByType<BulletSpawner>(FindObjectsSortMode.None);
        foreach (var spawner in spawners)
        {
            spawner.enabled = enabled;
        }
    }

    public RoomData GetPlayerRoom()
    {
        if (playerTransform == null) return null;
        Collider2D[] hits = Physics2D.OverlapCircleAll(playerTransform.position, 0.1f);
        foreach (var hit in hits)
        {
            foreach (var room in rooms)
            {
                if (hit == room.roomCollider) return room;
            }
        }
        return null;
    }

    void ShowWarningEffect(Vector3 pos)
    {
        if (warningEffectPrefab == null) return;
        GameObject warning = Instantiate(warningEffectPrefab, pos, Quaternion.identity);
        SpriteRenderer sr = warning.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = new Color(1, 0, 0, 0);
            sr.DOFade(1f, 0.3f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutQuad);
        }
        Destroy(warning, warningDuration);
    }

    void CloseDoors()
    {
        foreach (var door in allDoors)
        {
            door.CloseDoor();
            if (door.TryGetComponent<Collider2D>(out var col)) col.isTrigger = false;
        }
        foreach (var anim in allDoorAnimations)
            anim.PlayAnimation(DoorAnimation.DoorState.Closed);
    }

    void OpenDoors()
    {
        foreach (var door in allDoors)
        {
            door.OpenDoor();
            if (door.TryGetComponent<Collider2D>(out var col)) col.isTrigger = true;
        }
        foreach (var anim in allDoorAnimations)
            anim.PlayAnimation(DoorAnimation.DoorState.Open);
    }

    public void ApplyCameraConfiner(RoomData room)
    {
        if (cineCamera == null) return;
        
        var confiner = cineCamera.GetComponent<CinemachineConfiner2D>();
        if (confiner == null) return;
        
        Collider2D col = (room != null && room.cameraCollider != null) ? room.cameraCollider : null;
        
        // Confiner 변경 시 현재 카메라 위치 보존
        Vector3 preservedPos = cineCamera.transform.position;
        
        // (0,0) 위치 감지 시 경고 및 복원
        if (Mathf.Approximately(preservedPos.x, 0f) && Mathf.Approximately(preservedPos.y, 0f))
        {
            Debug.LogError("ApplyCameraConfiner: 카메라가 (0,0) 위치에 있음!");
            return; // Confiner 변경을 중단하여 추가 문제 방지
        }
        
        if (confiner.BoundingShape2D != col)
        {
            confiner.BoundingShape2D = col;
            confiner.InvalidateBoundingShapeCache();
            
            // 위치 변경 확인 및 복원
            Vector3 newPos = cineCamera.transform.position;
            if (Vector3.Distance(preservedPos, newPos) > 0.1f || 
                (Mathf.Approximately(newPos.x, 0f) && Mathf.Approximately(newPos.y, 0f)))
            {
                cineCamera.transform.position = preservedPos;
                Debug.LogWarning($"카메라 위치 복원: {newPos} → {preservedPos}");
            }
        }
    }


    private void DestroyAllEnemies()
    {
        string[] enemyTags = { "Enemy", "DashEnemy", "LongRangeEnemy", "PotionEnemy" };
        foreach (string tag in enemyTags)
        {
            foreach (GameObject enemy in GameObject.FindGameObjectsWithTag(tag))
                Destroy(enemy);
        }
    }
}