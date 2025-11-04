using DG.Tweening;
using Mono.Cecil.Cil;
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
    public float waveDelay = 2f;
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
    public List<RoomWaveData> waves = new List<RoomWaveData>();
    public List<MovingWall> movingWalls;

    [HideInInspector] public bool activated = false;

    [Header("카메라 Follow 설정")]
    public bool CameraFollow = true;

    [Header("카메라 연출 설정")]
    public bool enableZoomInSequence = true;
    public bool zoomInCameraFollow = false;
    public float zoomInDelay = 0.8f;
    public float zoomInDuration = 1.2f;
    public float zoomInTargetSize = 5.5f;

    [Header("이벤트 씬 설정")]
    public bool eventSceneEnabled = false;
    public Transform eventStartPos;
    public Transform eventEndPos;
    public GameObject eventObjectPrefab;
    public float eventMoveDuration = 3f;

    [Header("방 시작 시 기존 적 제거 여부")]
    public bool clearPreviousEnemies = true;

    [Header("문 초기 상태")]
    public bool doorsInitiallyOpen = true;

    [HideInInspector] public bool isCleared = false;
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

    //[Header("문 프리팹 부모")]
    //public GameObject doorParentPrefab;

    //[Header("문 애니메이션 프리팹 부모")]
    //public GameObject doorAnimationParentPrefab;

    [Header("스폰 관련")]
    public float spawnStop = 0f;

    [Tooltip("방 시작 시 기존 방 적을 모두 제거할지 여부")]
    public bool clearPreviousEnemies = true;

    private List<DoorController> allDoors = new List<DoorController>();
    private List<DoorAnimation> allDoorAnimations = new List<DoorAnimation>();
    private RoomData currentRoom;
    private bool cleared = false;
    private bool isSpawning = false;
    private bool isEventRunning = false;
    private int currentRoomIndex = 0;

    [Header("웨이브 진행 상태")]
    private int currentWaveIndex = 0;
    private bool isWaveActive = false;

    // ✅ 추가: 방 인덱스별 문 제어용
    [Header("클리어 시 올라가는 문 프리팹 부모")]
    public GameObject specialDoorParentPrefab;

    [Header("문 프리팹 부모 (일반 문용)")]
    public GameObject doorParentPrefab; // ✅ 추가

    private Dictionary<int, List<Transform>> doorsByRoom = new Dictionary<int, List<Transform>>(); // ✅ 추가
    private Dictionary<int, List<Transform>> specialDoorsByRoom = new Dictionary<int, List<Transform>>();
    private Dictionary<Transform, Vector3> originalDoorPositions = new Dictionary<Transform, Vector3>();

    void Start()
    {
        // ✅ 일반 Door 초기화
        if (doorParentPrefab != null)
        {
            foreach (Transform childGroup in doorParentPrefab.transform)
            {
                if (int.TryParse(childGroup.name, out int index))
                {
                    doorsByRoom[index] = new List<Transform>();
                    foreach (Transform door in childGroup)
                    {
                        if (door.CompareTag("Door"))
                        {
                            doorsByRoom[index].Add(door);
                            originalDoorPositions[door] = door.position;
                        }
                    }
                }
            }
        }

        // ✅ 특수문 초기화
        if (specialDoorParentPrefab != null)
        {
            foreach (Transform childGroup in specialDoorParentPrefab.transform)
            {
                if (int.TryParse(childGroup.name, out int index))
                {
                    specialDoorsByRoom[index] = new List<Transform>();
                    foreach (Transform door in childGroup)
                    {
                        specialDoorsByRoom[index].Add(door);
                        originalDoorPositions[door] = door.position;
                    }
                }
            }
        }

        // 첫 번째 방만 문 열기
        for (int i = 0; i < rooms.Count; i++)
            rooms[i].doorsInitiallyOpen = (i == 0);

        // 0번 방 특수문 시작 시 열기
        if (specialDoorsByRoom.ContainsKey(0))
        {
            foreach (var door in specialDoorsByRoom[0])
            {
                if (door == null) continue;
                Vector3 targetPos = originalDoorPositions[door] + Vector3.up * 1f;
                door.position = targetPos;
            }
        }
    }
    void Update()
    {
        if (!isSpawning && !isEventRunning)
        {
            RoomData room = GetPlayerRoom();
            if (room != null && room != currentRoom)
            {
                currentRoomIndex = rooms.IndexOf(room);
                ApplyCameraConfiner(room);

                if (cineCamera != null) cineCamera.Follow = null;
                currentRoom = room;
                StartCoroutine(MoveCameraToRoomAndStart(room));
            }
        }

        if (playerTransform != null)
        {
            PlayerController playerCtrl = playerTransform.GetComponent<PlayerController>();
            if (playerCtrl != null)
                playerCtrl.canMove = !isEventRunning;
        }
    }

    public void SetAllEnemiesAI(bool enabled)
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var obj in enemies)
        {
            TurretEnemy_PlayerTracking enemyScript = obj.GetComponent<TurretEnemy_PlayerTracking>();
            if (enemyScript != null) enemyScript.AIEnabled = enabled;

            Enemy enemyBase = obj.GetComponent<Enemy>();
            if (enemyBase != null)
            {
                if (enabled) enemyBase.EnableAI();
                else enemyBase.DisableAI();
            }
        }
    }

    IEnumerator RunEventScene(RoomData room)
    {
        if (!room.eventSceneEnabled || room.eventObjectPrefab == null ||
          room.eventStartPos == null || room.eventEndPos == null)
            yield break;

        isEventRunning = true;
        GameObject eventObj = Instantiate(room.eventObjectPrefab, room.eventStartPos.position, Quaternion.identity);
        cineCamera.Follow = eventObj.transform;

        PlayerController playerCtrl = playerTransform.GetComponent<PlayerController>();
        if (playerCtrl != null) playerCtrl.canMove = false;

        eventObj.transform.DOMove(room.eventEndPos.position, room.eventMoveDuration)
          .SetEase(Ease.InOutSine);

        yield return new WaitForSeconds(room.eventMoveDuration);

        Destroy(eventObj);
        cineCamera.Follow = null;
        isEventRunning = false;
    }

    IEnumerator MoveCameraToRoomAndStart(RoomData room)
    {
        if (room == null || room.cameraCollider == null)
            yield break;

        Vector3 currentCameraPos = cineCamera.transform.position;
        Vector3 roomCenter = room.cameraCollider.bounds.center;
        roomCenter.z = currentCameraPos.z;
        cineCamera.Follow = null;

        PlayerController playerCtrl = playerTransform.GetComponent<PlayerController>();
        if (playerCtrl != null) playerCtrl.canMove = false;

        if (!room.isCleared)
        {
            cleared = false;
            CloseDoors();

            // 이전 방 specialDoors 안전하게 내리기
            ResetSpecialDoors(currentRoomIndex);
        }

        SetAllEnemiesAI(false);
        SetAllBulletSpawnersActive(false);

        if (room.doorsInitiallyOpen && !room.isCleared) OpenDoors();

        if (room.eventSceneEnabled)
            yield return StartCoroutine(RunEventScene(room));

        // 🔍 카메라 줌인 연출
        if (room.enableZoomInSequence)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                Bounds bounds = room.cameraCollider.bounds;
                float screenRatio = (float)Screen.width / Screen.height;

                // ✅ 가로 기준으로 OrthographicSize 계산
                float targetOrthoSize = (bounds.size.x / 2f) / screenRatio;

                // 최소~최대 범위 설정
                targetOrthoSize = Mathf.Clamp(targetOrthoSize, 3f, 12f);

                // 줌 아웃(방으로 이동 + 시야 맞추기)
                Sequence zoomOutSeq = DOTween.Sequence();
                zoomOutSeq.Append(cineCamera.transform.DOMove(
                  new Vector3(bounds.center.x, bounds.center.y, cineCamera.transform.position.z),
                  cameraMoveDuration
                ).SetEase(Ease.InOutSine));

                zoomOutSeq.Join(DOTween.To(
                  () => cam.orthographicSize,
                  x => cam.orthographicSize = x,
                  targetOrthoSize,
                  0.6f
                ));

                zoomOutSeq.Join(DOTween.To(
                  () => cineCamera.Lens.OrthographicSize,
                  x => cineCamera.Lens.OrthographicSize = x,
                  targetOrthoSize,
                  0.6f
                ));

                yield return zoomOutSeq.WaitForCompletion();

                // 🔎 잠깐 대기 후 줌인 연출
                yield return new WaitForSeconds(room.zoomInDelay);

                Vector3 zoomTargetPos = room.zoomInCameraFollow ? playerTransform.position : bounds.center;
                zoomTargetPos.z = cineCamera.transform.position.z;

                Sequence zoomInSeq = DOTween.Sequence();
                zoomInSeq.Append(cineCamera.transform.DOMove(zoomTargetPos, room.zoomInDuration).SetEase(Ease.InOutSine));
                zoomInSeq.Join(DOTween.To(
                  () => cineCamera.Lens.OrthographicSize,
                  x => cineCamera.Lens.OrthographicSize = x,
                  room.zoomInTargetSize,
                  room.zoomInDuration
                ));
                yield return zoomInSeq.WaitForCompletion();
            }
        }


        cineCamera.Follow = playerTransform;
        if (playerCtrl != null) playerCtrl.canMove = true;
        SetAllEnemiesAI(true);
        SetAllBulletSpawnersActive(true);

        if (!room.activated && room.movingWalls != null)
        {
            room.activated = true;
            foreach (var wall in room.movingWalls)
                wall.isActive = true;
        }

        if (!room.isCleared)
        {
            currentWaveIndex = 0;
            isWaveActive = false;
            StartCoroutine(StartWaveSystem(room));
        }


    }

    IEnumerator StartWaveSystem(RoomData room)
    {
        if (room.waves == null || room.waves.Count == 0)
        {
            cleared = true;
            room.isCleared = true;
            OpenDoors();
            yield break;
        }

        for (currentWaveIndex = 0; currentWaveIndex < room.waves.Count; currentWaveIndex++)
        {
            RoomWaveData currentWave = room.waves[currentWaveIndex];
            yield return new WaitForSeconds(currentWave.waveDelay);
            yield return StartCoroutine(SpawnWaveEnemies(currentWave));
            yield return StartCoroutine(WaitForWaveCleared());
        }

        cleared = true;
        room.isCleared = true;

        if (GameManager.Instance.cameraShake != null)
        {
            for (int i = 0; i < 7; i++)
            {
                GameManager.Instance.cameraShake.GenerateImpulse();
                yield return new WaitForSeconds(0.1f);
            }
        }

        OpenDoors();
        RaiseSpecialDoors(currentRoomIndex);
    }

    IEnumerator SpawnWaveEnemies(RoomWaveData wave)
    {
        isWaveActive = true;

        foreach (var prefab in wave.enemyPrefabs)
        {
            foreach (Transform child in prefab.transform)
                ShowWarningEffect(child.position);
        }

        yield return new WaitForSeconds(warningDuration);

        foreach (var prefab in wave.enemyPrefabs)
        {
            GameObject tempObj = Instantiate(prefab, prefab.transform.position, prefab.transform.rotation);
            EnemyBase enemyBase = tempObj.GetComponent<EnemyBase>();
            if (enemyBase != null) enemyBase.CanMove = true;
        }
    }

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
            spawner.enabled = enabled;
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
            sr.DOFade(1f, 0.3f).SetLoops(-1, LoopType.Yoyo);
        }
        Destroy(warning, warningDuration);
    }

    //void CloseDoors()
    //{
    //    foreach (var door in allDoors)
    //    {
    //        door.CloseDoor();
    //        if (door.TryGetComponent<Collider2D>(out var col)) col.isTrigger = false;
    //    }
    //    foreach (var anim in allDoorAnimations)
    //        anim.PlayAnimation(DoorAnimation.DoorState.Closed);
    //}

    //void OpenDoors()
    //{
    //    foreach (var door in allDoors)
    //    {
    //        door.OpenDoor();
    //        if (door.TryGetComponent<Collider2D>(out var col)) col.isTrigger = true;
    //    }
    //    foreach (var anim in allDoorAnimations)
    //        anim.PlayAnimation(DoorAnimation.DoorState.Open);
    //}

    public void ApplyCameraConfiner(RoomData room)
    {
        if (cineCamera == null) return;
        var confiner = cineCamera.GetComponent<CinemachineConfiner2D>();
        if (confiner == null) return;

        Collider2D col = (room != null && room.cameraCollider != null) ? room.cameraCollider : null;
        Vector3 preservedPos = cineCamera.transform.position;

        if (confiner.BoundingShape2D != col)
        {
            confiner.BoundingShape2D = col;
            confiner.InvalidateBoundingShapeCache();
            cineCamera.transform.position = preservedPos;
        }
    }

    // ========================================
    // Door 제어 (DoorController 제거, 태그 기반)
    // ========================================

    // ========================================
    // 일반 문 제어 (doorParentPrefab 기반)
    // ========================================

    private void CloseDoors()
    {
        // ✅ 모든 방의 문 전부 닫기
        foreach (var kvp in doorsByRoom)
        {
            foreach (var door in kvp.Value)
            {
                if (door == null) continue;

                door.DOKill(); // 트윈 중복 방지
                Collider2D col = door.GetComponent<Collider2D>();
                if (col != null) col.isTrigger = false;
            }
        }
    }

    private void OpenDoors()
    {
        // ✅ 현재 방 인덱스의 문만 열기
        if (!doorsByRoom.ContainsKey(currentRoomIndex)) return;

        foreach (var door in doorsByRoom[currentRoomIndex])
        {
            if (door == null) continue;

            door.DOKill();
            Collider2D col = door.GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }
    }


    // ========================================
    // 특수문 제어 (specialDoorParentPrefab 기반)
    // ========================================

    private void RaiseSpecialDoors(int roomIndex)
    {
        if (!specialDoorsByRoom.ContainsKey(roomIndex)) return;

        foreach (var door in specialDoorsByRoom[roomIndex])
        {
            if (door == null) continue;

            Vector3 targetPos = originalDoorPositions[door] + Vector3.up * 1f;
            Collider2D col = door.GetComponent<Collider2D>();

            door.DOMove(targetPos, 0.5f)
              .SetEase(Ease.InOutSine)
              .OnComplete(() =>
              {
                  if (col != null) col.isTrigger = true;
              });
        }
    }

    private void ResetSpecialDoors(int roomIndex)
    {
        int prev = roomIndex - 1;
        if (prev < 0 || !specialDoorsByRoom.ContainsKey(prev)) return;

        foreach (var door in specialDoorsByRoom[prev])
        {
            if (door == null) continue;

            door.DOKill();
            Vector3 orig = originalDoorPositions[door];
            door.DOMove(orig, 0.3f).SetEase(Ease.InOutSine);

            Collider2D col = door.GetComponent<Collider2D>();
            if (col != null) col.isTrigger = false;
        }
    }
}