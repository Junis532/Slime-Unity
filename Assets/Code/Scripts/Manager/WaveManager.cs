using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.UI;

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

    [Header("클리어 시 사라질 오브젝트들")]
    public List<GameObject> objectsToDisappear = new List<GameObject>();

    [Header("방 안 회전 장애물")]
    public List<ObstacleTurn> obstacleTurns = new List<ObstacleTurn>();


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

    [Header("맵 즉시 클리어 설정")]
    public bool instantClear = false;


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

    [Header("Collider 오브젝트")]
    public GameObject ColliderObject;

    private Dictionary<int, List<Transform>> doorsByRoom = new Dictionary<int, List<Transform>>();
    private Dictionary<int, List<Transform>> specialDoorsByRoom = new Dictionary<int, List<Transform>>();
    private Dictionary<Transform, Vector3> originalDoorPositions = new Dictionary<Transform, Vector3>();

    [Header("7스테이지 클리어 오브젝트")]
    public GameObject stg7ClearObject;

    void Start()
    {
        // ✅ 일반 Door 초기화
        if (ColliderObject != null)
        {
            foreach (Transform childGroup in ColliderObject.transform)
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

        //if (playerTransform != null)
        //{
        //    PlayerController playerCtrl = playerTransform.GetComponent<PlayerController>();
        //    if (playerCtrl != null)
        //        playerCtrl.canMove = !isEventRunning;
        //}
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
        //if (playerCtrl != null) playerCtrl.canMove = false;

        eventObj.transform.DOMove(room.eventEndPos.position, room.eventMoveDuration)
          .SetEase(Ease.InOutSine);

        yield return new WaitForSeconds(room.eventMoveDuration);

        Destroy(eventObj);
        cineCamera.Follow = null;
        isEventRunning = false;
    }

    IEnumerator MoveCameraToRoomAndStart(RoomData room)
    {
        if (!room.instantClear)
        {
            GameManager.Instance.playerController.LockMovement();
        }

        if (room == null || room.cameraCollider == null)
            yield break;

        Vector3 currentCameraPos = cineCamera.transform.position;
        Vector3 roomCenter = room.cameraCollider.bounds.center;
        roomCenter.z = currentCameraPos.z;
        cineCamera.Follow = null;

        //PlayerController playerCtrl = playerTransform.GetComponent<PlayerController>();
        //if (playerCtrl != null) playerCtrl.canMove = false;

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

        // 📌 카메라 이동 (줌인 Sequence와 상관없이)
        Sequence camMoveSeq = DOTween.Sequence();
        camMoveSeq.Append(cineCamera.transform.DOMove(
            new Vector3(roomCenter.x, roomCenter.y, cineCamera.transform.position.z),
            cameraMoveDuration
        ).SetEase(Ease.InOutSine));
        yield return camMoveSeq.WaitForCompletion();

        float smoothDuration = 0.8f; // 부드럽게 바뀌는 시간

        Camera mainCam = Camera.main;
        if (mainCam != null)
            DOTween.To(() => mainCam.orthographicSize,
                       x => mainCam.orthographicSize = x,
                       room.zoomInTargetSize,
                       smoothDuration);

        if (cineCamera != null)
            DOTween.To(() => cineCamera.Lens.OrthographicSize,
                       x => cineCamera.Lens.OrthographicSize = x,
                       room.zoomInTargetSize,
                       smoothDuration);

        // 🔍 카메라 줌인 연출
        if (room.enableZoomInSequence)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                Bounds bounds = room.cameraCollider.bounds;
                float screenRatio = (float)Screen.width / Screen.height;

                // 가로 기준으로 OrthographicSize 계산
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

                // 잠깐 대기 후 줌인 연출
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
        //if (playerCtrl != null) playerCtrl.canMove = true;
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

    IEnumerator ContinuousCameraShake()
    {
        while (true)
        {
            if (GameManager.Instance != null && GameManager.Instance.cameraShake != null)
                GameManager.Instance.cameraShake.GenerateImpulse();

            yield return new WaitForSeconds(0.1f); // 0.1초 간격으로 흔들림
        }
    }

    public void Stage7ClearSequence()
    {
        StartCoroutine(Stage7ClearRoutine());
    }



    public void Stage8ClearSequence()
    {
        StartCoroutine(Stage8ClearRoutine());
    }

    private IEnumerator Stage7ClearRoutine()
    {
        Debug.Log("🎬 7번째 방 클리어! 특별 연출 시작");
        GameManager.Instance.playerController.LockMovement();
        GameManager.Instance.audioManager.StoneFalling(1.2f);

        // ✅ 페이드용 UI 오브젝트 자동 생성
        GameObject fadeObj = new GameObject("FullScreenFade_Auto");
        Canvas canvas = fadeObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;
        fadeObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        fadeObj.AddComponent<GraphicRaycaster>();

        GameObject imgObj = new GameObject("FadeImage");
        imgObj.transform.SetParent(fadeObj.transform, false);
        Image fadeImage = imgObj.AddComponent<Image>();
        fadeImage.color = Color.black; // 검은색 페이드
        RectTransform rect = fadeImage.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        CanvasGroup fadeGroup = fadeImage.gameObject.AddComponent<CanvasGroup>();
        fadeGroup.alpha = 0f;

        // ✅ 카메라 흔들림 코루틴 시작
        Coroutine shakeCoroutine = StartCoroutine(ContinuousCameraShake());
        yield return new WaitForSeconds(3f);

        // ✅ 두 번 깜빡임
        for (int i = 0; i < 2; i++)
        {
            yield return fadeGroup.DOFade(1f, 0.05f).WaitForCompletion();
            yield return fadeGroup.DOFade(0f, 0.05f).WaitForCompletion();
            yield return new WaitForSeconds(0.05f);
        }
        yield return new WaitForSeconds(1f);

        // ✅ 완전 암전
        yield return fadeGroup.DOFade(1f, 0.15f).WaitForCompletion();

        if (playerTransform != null)
            playerTransform.position = new Vector3(19f, 76.5f, 0f);

        // ✅ 카메라 흔들림 중지
        StopCoroutine(shakeCoroutine);

        // ✅ 암전 상태 유지 (2초)
        yield return new WaitForSeconds(2f);

        // ✅ 천천히 화면 다시 밝아짐 (페이드 인)
        yield return fadeGroup.DOFade(0f, 2f).WaitForCompletion();
        GameManager.Instance.playerController.UnLockMovement();
        // ✅ 자동 생성된 페이드 오브젝트 삭제
        Destroy(fadeObj);
    }
    private IEnumerator Stage8ClearRoutine()
    {
        GameManager.Instance.playerController.LockMovement();
        // ✅ 페이드용 UI 오브젝트 자동 생성
        GameObject fadeObj = new GameObject("FullScreenFade_Auto");
        Canvas canvas = fadeObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;
        fadeObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        fadeObj.AddComponent<GraphicRaycaster>();

        GameObject imgObj = new GameObject("FadeImage");
        imgObj.transform.SetParent(fadeObj.transform, false);
        Image fadeImage = imgObj.AddComponent<Image>();
        fadeImage.color = Color.black;
        RectTransform rect = fadeImage.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        CanvasGroup fadeGroup = fadeImage.gameObject.AddComponent<CanvasGroup>();
        fadeGroup.alpha = 0f;

        // ✅ 완전 암전
        yield return fadeGroup.DOFade(1f, 0.15f).WaitForCompletion();

        // 플레이어 위치 세팅 (높은 곳에 배치)
        if (playerTransform != null)
        {
            playerTransform.position = new Vector3(65.77f, 72f, 0f); // 살짝 위에서 시작
        }

        // ✅ 암전 상태 유지 (2초)
        yield return new WaitForSeconds(2f);
        // ✅ 화면 서서히 밝아짐
        fadeGroup.DOFade(0f, 1.5f).WaitForCompletion();

        if (playerTransform != null)
        {
            Vector3 groundPos = new Vector3(65.77f, 67.74f, 0f);
            float originalX = playerTransform.position.x;

            // 1️⃣ 바닥까지 떨어짐 (X축 고정)
            yield return playerTransform.DOMoveY(groundPos.y, 1.0f)
                .SetEase(Ease.InQuad)
                .OnUpdate(() =>
                {
                    Vector3 pos = playerTransform.position;
                    pos.x = originalX;
                    playerTransform.position = pos;
                })
                .WaitForCompletion();

            // 2️⃣ 첫 번째 튕김: 높게, X 앞으로
            float bounce1Height = 0.6f;
            float forward1 = 0.3f;
            Tween moveX1 = playerTransform.DOMoveX(originalX + forward1, 0.4f).SetEase(Ease.Linear);
            Tween moveY1 = playerTransform.DOMoveY(groundPos.y + bounce1Height, 0.2f)
                .SetEase(Ease.OutSine)
                .OnComplete(() =>
                {
                    playerTransform.DOMoveY(groundPos.y, 0.2f).SetEase(Ease.InSine);
                });
            yield return DOTween.Sequence().Join(moveX1).Join(moveY1).WaitForCompletion();

            // 3️⃣ 두 번째 튕김: 낮게, 조금 앞으로
            float bounce2Height = 0.3f;
            float forward2 = 0.2f;
            Tween moveX2 = playerTransform.DOMoveX(originalX + forward1 + forward2, 0.35f).SetEase(Ease.Linear);
            Tween moveY2 = playerTransform.DOMoveY(groundPos.y + bounce2Height, 0.15f)
                .SetEase(Ease.OutSine)
                .OnComplete(() =>
                {
                    playerTransform.DOMoveY(groundPos.y, 0.15f).SetEase(Ease.InSine);
                });
            yield return DOTween.Sequence().Join(moveX2).Join(moveY2).WaitForCompletion();
        }

        GameManager.Instance.playerController.UnLockMovement();
        // ✅ 자동 생성된 페이드 오브젝트 삭제
        Destroy(fadeObj);
    }


    IEnumerator StartWaveSystem(RoomData room)
    {

        // 🟢 맵 즉시 클리어 모드 활성화된 경우
        if (room.instantClear)
        {
            cleared = true;
            room.isCleared = true;

            OpenDoors();
            RaiseSpecialDoors(currentRoomIndex);

            yield break;
        }

        if (room.waves == null || room.waves.Count == 0)
        {
            cleared = true;
            room.isCleared = true;
            OpenDoors();

            yield break;
        }

        // 🔵 웨이브 루프
        for (currentWaveIndex = 0; currentWaveIndex < room.waves.Count; currentWaveIndex++)
        {
            RoomWaveData currentWave = room.waves[currentWaveIndex];
            yield return new WaitForSeconds(currentWave.waveDelay);
            yield return StartCoroutine(SpawnWaveEnemies(currentWave));

            GameManager.Instance.playerController.UnLockMovement();
            // 현재 방의 장애물 회전 시작
            StartObstacleTurns(room);

            yield return StartCoroutine(WaitForWaveCleared());

        }

        cleared = true;
        room.isCleared = true;

        // HPPotion 자석 이동 호출
        AutoCollectItems();

        // ✅ 클리어 시 오브젝트 DOTween으로 사라지기
        if (room.objectsToDisappear != null && room.objectsToDisappear.Count > 0)
        {
            foreach (var obj in room.objectsToDisappear)
            {
                if (obj != null)
                {
                    CanvasGroup cg = obj.GetComponent<CanvasGroup>();
                    SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();

                    // CanvasGroup이 있으면 UI 페이드 아웃
                    if (cg != null)
                    {
                        cg.DOFade(0f, 1f).OnComplete(() => Destroy(obj));
                    }
                    // SpriteRenderer면 시각적 오브젝트 페이드 아웃
                    else if (sr != null)
                    {
                        sr.DOFade(0f, 1f).OnComplete(() => Destroy(obj));
                    }
                    // 그 외엔 그냥 스케일 축소
                    else
                    {
                        obj.transform.DOScale(Vector3.zero, 0.6f)
                            .SetEase(Ease.InBack)
                            .OnComplete(() => Destroy(obj));
                    }
                }
            }
        }


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

    private void StartObstacleTurns(RoomData room)
    {
        if (room != null && room.obstacleTurns != null)
        {
            foreach (var obstacle in room.obstacleTurns)
            {
                if (obstacle != null)
                    obstacle.StartTurning();
            }
        }
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
        {
            // ✅ 깜빡임 제거 — 단일 페이드 인 효과만 적용
            sr.color = new Color(1, 0, 0, 0);
            sr.DOFade(1f, 0.3f);
        }
        Destroy(warning, warningDuration);
    }

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

    private void CloseDoors() // is Trigger 끄기
    {
        // 모든 방의 문 전부 닫기
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

    private void OpenDoors() // is Trigger 켜기
    {
        // 현재 방 인덱스의 문만 열기
        if (!doorsByRoom.ContainsKey(currentRoomIndex)) return;

        foreach (var door in doorsByRoom[currentRoomIndex])
        {
            if (door == null) continue;

            door.DOKill();
            Collider2D col = door.GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }
    }


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
    public void RestoreCameraAndRoom()
    {
        StartCoroutine(RestoreCameraRoutine());
    }

    private IEnumerator RestoreCameraRoutine()
    {
        if (cineCamera != null)
        {
            // 🔹 1️⃣ 우선 트래킹 완전히 해제
            var ct = cineCamera.Target;
            ct.TrackingTarget = null;
            cineCamera.Target = ct;

            // 🔹 2️⃣ 한 프레임 기다려서 카메라 업데이트 반영
            yield return null;

            // 🔹 3️⃣ 카메라 크기, 우선순위 복원
            cineCamera.Lens.OrthographicSize = 5.6f;
            cineCamera.Priority = 10;
            cineCamera.enabled = true;

            // 🔹 4️⃣ 방 중심으로 이동
            RoomData currentRoom = GetPlayerRoom();
            if (currentRoom != null)
            {
                Vector3 roomCenter = currentRoom.roomCollider.bounds.center;
                DOTween.Kill(cineCamera.transform);
                cineCamera.transform.DOMove(
                    new Vector3(roomCenter.x, roomCenter.y, cineCamera.transform.position.z),
                    0.8f
                ).SetEase(Ease.OutQuad);
            }
        }

        // 🔹 5️⃣ 룸 및 이벤트 상태 복원
        if (currentRoom != null)
        {
            currentRoom.activated = true;
            isEventRunning = false;
        }

        Debug.Log("[WaveManager] Dialogue ended — camera tracking disabled and centered on room.");
    }
    private IEnumerator MoveCoinToPlayer(GameObject zac, float duration) // 플레이어 위치로 이동시키는 코루틴
    {
        float elapsed = 0f;
        Transform coinTransform = zac.transform;
        Vector3 startPos = coinTransform.position;

        while (elapsed < duration)
        {
            if (GameManager.Instance.playerController != null)
            {
                Vector3 playerPos = GameManager.Instance.playerController.transform.position;
                coinTransform.position = Vector3.Lerp(startPos, playerPos, elapsed / duration);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        coinTransform.position = GameManager.Instance.playerController.transform.position;
    }

    private void AutoCollectItems() // 아이템 자동 수집 처리 함수
    {
        GameObject[] zacs = GameObject.FindGameObjectsWithTag("HPPotion");
        foreach (GameObject zac in zacs)
        {
            StartCoroutine(MoveCoinToPlayer(zac, 0.5f));
        }
    }
}