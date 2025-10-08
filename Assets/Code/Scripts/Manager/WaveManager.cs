
//using DG.Tweening;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;
//using UnityEngine.Video;
//using Unity.Cinemachine;

//[System.Serializable]
//public class RoomData
//{
//    public string roomName;
//    public GameObject roomPrefab;

//    [Header("Room 판정용 Collider")]
//    public Collider2D roomCollider;

//    [Header("Camera Confiner Collider")]
//    public Collider2D cameraCollider;

//    [Header("적 스폰 설정")]
//    public List<GameObject> enemyPrefabs;
//    public bool spawnImmediately = false;

//    public List<MovingWall> movingWalls;

//    [HideInInspector]
//    public bool activated = false;

//    [Header("카메라 Follow 설정")]
//    public bool CameraFollow = true;

//    [Header("이벤트 씬 설정")]
//    public bool eventSceneEnabled = false;
//    public Transform eventStartPos;
//    public Transform eventEndPos;
//    public GameObject eventObjectPrefab;
//    public float eventMoveDuration = 3f;

//    [Header("방 시작 시 기존 적 제거 여부")]
//    public bool clearPreviousEnemies = true;

//    [Header("MP4 영상 재생 설정")]
//    public bool playVideoOnEnter = false;
//    public VideoClip roomVideoClip;
//    public bool skipWithInput = true;
//    public float videoFadeDuration = 1f;
//}

//public class WaveManager : MonoBehaviour
//{
//    [Header("모든 방 데이터")]
//    public List<RoomData> rooms;

//    [Header("플레이어")]
//    public Transform playerTransform;

//    [Header("카메라")]
//    public CinemachineCamera cineCamera;
//    public float cameraMoveDuration = 0.5f;

//    [Header("문 프리팹 부모")]
//    public GameObject doorParentPrefab;

//    [Header("문 애니메이션 프리팹 부모")]
//    public GameObject doorAnimationParentPrefab;

//    [Tooltip("방 시작 시 기존 방 적을 모두 제거할지 여부")]
//    public bool clearPreviousEnemies = true;

//    private List<DoorController> allDoors = new List<DoorController>();
//    private List<DoorAnimation> allDoorAnimations = new List<DoorAnimation>();
//    private RoomData currentRoom;
//    private bool isSpawning = false;
//    private bool isFirstRoom = true;
//    private bool isEventRunning = false;

//    void Start()
//    {
//        if (doorParentPrefab != null)
//            allDoors.AddRange(doorParentPrefab.GetComponentsInChildren<DoorController>(true));
//        if (doorAnimationParentPrefab != null)
//            allDoorAnimations.AddRange(doorAnimationParentPrefab.GetComponentsInChildren<DoorAnimation>(true));
//    }

//    void Update()
//    {
//        if (!isSpawning && !isEventRunning)
//        {
//            RoomData room = GetPlayerRoom();
//            if (room != null && room != currentRoom)
//            {
//                if (cineCamera != null) cineCamera.Follow = null;
//                var confiner = cineCamera.GetComponent<CinemachineConfiner2D>();
//                if (confiner != null) confiner.BoundingShape2D = null;
//                currentRoom = room;
//                StartCoroutine(MoveCameraToRoomAndStart(room));
//            }
//        }
//    }

//    IEnumerator MoveCameraToRoomAndStart(RoomData room)
//    {
//        if (room.cameraCollider == null) yield break;

//        Vector3 targetPos = (room.eventSceneEnabled && room.eventStartPos != null)
//            ? room.eventStartPos.position
//            : room.cameraCollider.bounds.center;
//        targetPos.z = cineCamera.transform.position.z;

//        ApplyCameraConfiner(room, false);
//        cineCamera.transform.DOMove(targetPos, cameraMoveDuration).SetEase(Ease.InOutQuad);
//        yield return new WaitForSeconds(cameraMoveDuration);

//        if (room.CameraFollow && cineCamera != null)
//        {
//            cineCamera.Follow = playerTransform;
//            ApplyCameraConfiner(room, true);
//            cineCamera.Lens.OrthographicSize = 5.5f;
//        }
//        else
//        {
//            cineCamera.Follow = null;
//            ApplyCameraConfiner(room, false);
//        }

//        if (!room.activated)
//        {
//            room.activated = true;
//            if (room.movingWalls != null)
//                foreach (var wall in room.movingWalls)
//                    if (wall != null) wall.isActive = true;

//            yield return StartCoroutine(StartRoom(room));
//        }
//    }

//    public RoomData GetPlayerRoom()
//    {
//        if (playerTransform == null) return null;
//        Collider2D[] hits = Physics2D.OverlapCircleAll(playerTransform.position, 0.1f);
//        foreach (var hit in hits)
//            foreach (var room in rooms)
//                if (hit == room.roomCollider) return room;
//        return null;
//    }

//    IEnumerator StartRoom(RoomData room)
//    {
//        if (clearPreviousEnemies) DestroyAllEnemies();

//        isSpawning = true;

//        if (!isFirstRoom) CloseDoors();
//        yield return new WaitForSeconds(0.3f);

//        Coroutine videoCoroutine = null;
//        if (room.playVideoOnEnter && room.roomVideoClip != null)
//            videoCoroutine = StartCoroutine(PlayRoomVideo(room));

//        List<EnemyBase> spawnedEnemies = new List<EnemyBase>();
//        foreach (var prefab in room.enemyPrefabs)
//        {
//            GameObject tempObj = Instantiate(prefab, prefab.transform.position, prefab.transform.rotation);
//            EnemyBase enemyBase = tempObj.GetComponent<EnemyBase>();
//            if (enemyBase != null)
//            {
//                enemyBase.CanMove = false;
//                spawnedEnemies.Add(enemyBase);
//            }
//        }

//        if (videoCoroutine != null)
//            yield return videoCoroutine;

//        foreach (var enemy in spawnedEnemies)
//            if (enemy != null) enemy.CanMove = true;

//        // ✅ 적 제거 대기 (리스트 기반 안정화)
//        while (spawnedEnemies.Count > 0)
//        {
//            spawnedEnemies.RemoveAll(e => e == null);
//            yield return null;
//        }

//        OpenDoors();
//        isSpawning = false;
//        if (isFirstRoom) isFirstRoom = false;
//    }

//    private IEnumerator PlayRoomVideo(RoomData room)
//    {
//        if (!room.playVideoOnEnter || room.roomVideoClip == null) yield break;

//        isEventRunning = true;
//        float originalTimeScale = Time.timeScale;
//        Time.timeScale = 0f;

//        AudioManager.Instance?.bgmSource?.Pause();

//        Transform originalFollow = null;
//        if (cineCamera != null)
//        {
//            originalFollow = cineCamera.Follow;
//            cineCamera.Follow = null;
//        }

//        GameObject canvasObj = new GameObject("RoomVideoCanvas");
//        Canvas canvas = canvasObj.AddComponent<Canvas>();
//        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
//        canvas.sortingOrder = 999;
//        canvasObj.AddComponent<GraphicRaycaster>();

//        CanvasGroup canvasGroup = canvasObj.AddComponent<CanvasGroup>();
//        canvasGroup.alpha = 0f;
//        canvasGroup.blocksRaycasts = true;

//        GameObject rawImageObj = new GameObject("RoomVideo");
//        rawImageObj.transform.SetParent(canvasObj.transform, false);
//        RectTransform rect = rawImageObj.AddComponent<RectTransform>();
//        rect.anchorMin = Vector2.zero;
//        rect.anchorMax = Vector2.one;
//        rect.offsetMin = Vector2.zero;
//        rect.offsetMax = Vector2.zero;

//        RawImage rawImage = rawImageObj.AddComponent<RawImage>();
//        rawImage.color = Color.white;

//        AspectRatioFitter ar = rawImageObj.AddComponent<AspectRatioFitter>();
//        ar.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
//        ar.aspectRatio = (float)room.roomVideoClip.width / room.roomVideoClip.height;

//        VideoPlayer vp = rawImageObj.AddComponent<VideoPlayer>();
//        vp.playOnAwake = false;
//        vp.renderMode = VideoRenderMode.APIOnly;
//        vp.source = VideoSource.VideoClip;
//        vp.clip = room.roomVideoClip;
//        vp.isLooping = false;
//        vp.audioOutputMode = VideoAudioOutputMode.AudioSource;

//        AudioSource audioSource = rawImageObj.AddComponent<AudioSource>();
//        vp.SetTargetAudioSource(0, audioSource);

//        vp.Prepare();
//        while (!vp.isPrepared) yield return null;

//        rawImage.texture = vp.texture;
//        vp.Play();
//        audioSource.Play();

//        yield return canvasGroup.DOFade(1f, room.videoFadeDuration).SetUpdate(true).WaitForCompletion();

//        bool isVideoFinished = false;
//        vp.loopPointReached += (source) => isVideoFinished = true;

//        while (!isVideoFinished)
//        {
//            if (room.skipWithInput && Input.anyKeyDown)
//            {
//                vp.Stop();
//                isVideoFinished = true;
//            }
//            yield return null;
//        }

//        Time.timeScale = originalTimeScale;
//        yield return canvasGroup.DOFade(0f, room.videoFadeDuration).SetUpdate(true).WaitForCompletion();
//        Destroy(canvasObj);

//        AudioManager.Instance?.bgmSource?.UnPause();
//        if (cineCamera != null) cineCamera.Follow = originalFollow;

//        isEventRunning = false;
//    }

//    void CloseDoors()
//    {
//        foreach (var door in allDoors)
//        {
//            // door.CloseDoor();
//        }
//    }

//    void OpenDoors()
//    {
//        foreach (var door in allDoors)
//        {
//            // door.OpenDoor();
//        }
//    }

//    public void ApplyCameraConfiner(RoomData room, bool forcePlayerFollow = true)
//    {
//        if (cineCamera == null || room.cameraCollider == null) return;
//        var confiner = cineCamera.GetComponent<CinemachineConfiner2D>();
//        if (confiner != null && confiner.BoundingShape2D != room.cameraCollider)
//        {
//            confiner.BoundingShape2D = room.cameraCollider;
//            confiner.InvalidateBoundingShapeCache();
//        }
//    }

//    private void DestroyAllEnemies()
//    {
//        foreach (var room in rooms)
//            foreach (var prefab in room.enemyPrefabs)
//            {
//                EnemyBase e = prefab.GetComponent<EnemyBase>();
//                if (e != null) Destroy(e.gameObject);
//            }
//    }
//}


using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.AI;

[System.Serializable]
public class RoomData
{
    public string roomName;
    public GameObject roomPrefab;
    [Header("Room 판정용 Collider")]
    public Collider2D roomCollider;
    [Header("Camera Confiner Collider")]
    public Collider2D cameraCollider;
    public List<GameObject> enemyPrefabs;
    public List<MovingWall> movingWalls;
    [HideInInspector]
    public bool activated = false;
    [Header("카메라 Follow 설정")]
    public bool CameraFollow = true;
    [Header("이벤트 씬 설정")]
    public bool eventSceneEnabled = false;
    public Transform eventStartPos;
    public Transform eventEndPos;
    public GameObject eventObjectPrefab;
    public float eventMoveDuration = 3f;
    [Header("방 시작 시 기존 적 제거 여부")]
    public bool clearPreviousEnemies = true; // ← Room별로 설정 가능
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

    void Start()
    {
        if (doorParentPrefab != null) allDoors.AddRange(doorParentPrefab.GetComponentsInChildren<DoorController>(true));
        if (doorAnimationParentPrefab != null) allDoorAnimations.AddRange(doorAnimationParentPrefab.GetComponentsInChildren<DoorAnimation>(true));
    }

    void Update()
    {
        if (!isSpawning && !isEventRunning)
        {
            RoomData room = GetPlayerRoom();
            if (room != null && room != currentRoom)
            {
                // 이전 방 초기화
                if (cineCamera != null) cineCamera.Follow = null;
                var confiner = cineCamera.GetComponent<CinemachineConfiner2D>();
                if (confiner != null) confiner.BoundingShape2D = null;
                currentRoom = room;

                // 🔹 카메라 먼저 이동 (이벤트 유무에 따라 목표 위치 변경)
                StartCoroutine(MoveCameraToRoomAndStart(room));
            }
        }
    }

    /// <summary>
    /// 새 방으로 진입 시 카메라를 먼저 이동시키고, 완료 후 StartRoom 실행
    /// </summary>
    IEnumerator MoveCameraToRoomAndStart(RoomData room)
    {
        if (room.cameraCollider == null) yield break;

        // 💡 카메라 목표 위치 설정: 이벤트 씬이 활성화된 경우 이벤트 시작 위치, 아니면 방 중앙
        Vector3 targetPos;
        if (room.eventSceneEnabled && room.eventStartPos != null)
        {
            // 이벤트 시작 위치로 설정
            targetPos = room.eventStartPos.position;
        }
        else
        {
            // 방 중앙으로 설정 (기존 로직)
            targetPos = room.cameraCollider.bounds.center;
        }

        targetPos.z = cineCamera.transform.position.z;

        // 카메라 Confiner 적용 (이동 중에는 Confiner만 적용하고 Follow는 설정하지 않음)
        ApplyCameraConfiner(room, forcePlayerFollow: false);

        // 카메라 이동
        cineCamera.transform.DOMove(targetPos, cameraMoveDuration).SetEase(Ease.InOutQuad);
        yield return new WaitForSeconds(cameraMoveDuration);

        // ✅ 카메라 Follow 설정: 이동 완료 후 CameraFollow 설정에 따름
        if (room.CameraFollow && cineCamera != null)
        {
            cineCamera.Follow = playerTransform;
            ApplyCameraConfiner(room, forcePlayerFollow: true);
            // 여기에서 Size를 5.5로 고정
            cineCamera.Lens.OrthographicSize = 5.5f; // <-------
        }
        else if (cineCamera != null)
        {
            cineCamera.Follow = null;
            ApplyCameraConfiner(room, forcePlayerFollow: false);
        }

        if (!room.activated)
        {
            room.activated = true;
            if (room.movingWalls != null)
            {
                foreach (var wall in room.movingWalls)
                    if (wall != null) wall.isActive = true;
            }
            yield return StartCoroutine(StartRoom(room));
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

    IEnumerator StartRoom(RoomData room)
    {
        if (clearPreviousEnemies) DestroyAllEnemies();

        isSpawning = true;
        cleared = false;
        if (!isFirstRoom) CloseDoors();
        yield return new WaitForSeconds(0.3f);

        // -------- 이벤트 씬 처리 --------
        if (room.eventSceneEnabled && room.eventObjectPrefab != null && room.eventStartPos != null && room.eventEndPos != null)
        {
            // MoveCameraToRoomAndStart에서 이미 이벤트 시작 위치로 카메라를 이동했기 때문에
            // 여기서는 추가적인 카메라 이동 없이 바로 이벤트 오브젝트 팔로우로 전환
            isEventRunning = true;

            // 🔹 플레이어 이동 제한
            // ... (GameManager 로직 생략) ...

            // 🔹 이벤트 오브젝트 생성
            GameObject eventObj = Instantiate(room.eventObjectPrefab, room.eventStartPos.position, Quaternion.identity);

            // 🔹 카메라 Follow 이벤트 오브젝트로 전환 (이동 중이라도)
            if (cineCamera != null)
            {
                // Follow를 null로 설정하고 Confiner 업데이트 후,
                // 즉시 이벤트 오브젝트를 Follow하여 이벤트 시퀀스 시작
                cineCamera.Follow = null;
                ApplyCameraConfiner(room, forcePlayerFollow: false);
                yield return new WaitForSeconds(0.05f); // Confiner 반영 대기
                cineCamera.Follow = eventObj.transform;
            }

            // 🔹 이벤트 이동 애니메이션
            eventObj.transform.DOMove(room.eventEndPos.position, room.eventMoveDuration)
                .SetEase(Ease.Linear)
                .OnComplete(() => {
                    Destroy(eventObj);

                    // ✅ 이벤트 종료 후 플레이어 팔로우 복귀
                    if (cineCamera != null)
                    {
                        // room.CameraFollow 설정에 따라 Follow 복귀
                        cineCamera.Follow = room.CameraFollow ? playerTransform : null;
                    }
                    ApplyCameraConfiner(room);
                    // ... (플레이어 이동 제한 해제 로직 생략) ...
                    isEventRunning = false;
                });

            yield return new WaitForSeconds(room.eventMoveDuration + 0.2f);
        }

        // -------- 적 스폰 --------
        // ... (나머지 StartRoom 로직 유지) ...

        foreach (var prefab in room.enemyPrefabs)
        {
            GameObject tempObj = Instantiate(prefab, prefab.transform.position, prefab.transform.rotation);
            tempObj.SetActive(false);
            foreach (Transform child in tempObj.transform) ShowWarningEffect(child.position);
            yield return new WaitForSeconds(warningDuration);
            tempObj.SetActive(true);
            EnemyBase enemyBase = tempObj.GetComponent<EnemyBase>();
            if (enemyBase != null)
            {
                enemyBase.CanMove = false;
                yield return new WaitForSeconds(spawnStop);
                enemyBase.CanMove = true;
            }
            while (true)
            {
                int enemiesLeft = GameObject.FindGameObjectsWithTag("Enemy").Length + GameObject.FindGameObjectsWithTag("DashEnemy").Length + GameObject.FindGameObjectsWithTag("LongRangeEnemy").Length + GameObject.FindGameObjectsWithTag("PotionEnemy").Length;
                if (enemiesLeft == 0) break;
                yield return new WaitForSeconds(0.5f);
            }
        }
        cleared = true;
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
            foreach (var wall in room.movingWalls) wall?.ResetWall();
        }
        isSpawning = false;
        if (isFirstRoom) isFirstRoom = false;
    }

    // ... (ShowWarningEffect, CloseDoors, OpenDoors, ApplyCameraConfiner, DestroyAllEnemies 함수는 동일) ...

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
        foreach (var anim in allDoorAnimations) anim.PlayAnimation(DoorAnimation.DoorState.Closed);
    }

    void OpenDoors()
    {
        foreach (var door in allDoors)
        {
            door.OpenDoor();
            if (door.TryGetComponent<Collider2D>(out var col)) col.isTrigger = true;
        }
        foreach (var anim in allDoorAnimations) anim.PlayAnimation(DoorAnimation.DoorState.Open);
    }

    public void ApplyCameraConfiner(RoomData room, bool forcePlayerFollow = true)
    {
        if (cineCamera == null || room.cameraCollider == null) return;
        var confiner = cineCamera.GetComponent<CinemachineConfiner2D>();
        if (confiner != null && confiner.BoundingShape2D != room.cameraCollider)
        {
            confiner.BoundingShape2D = room.cameraCollider;
            confiner.InvalidateBoundingShapeCache();
        }

        Camera cam = Camera.main;
        if (cam == null || !cam.orthographic) return;

        Bounds bounds = room.cameraCollider.bounds;
        float screenRatio = (float)Screen.width / Screen.height;
        float boundsRatio = bounds.size.x / bounds.size.y;
        float orthoSize;

        if (room.eventSceneEnabled && !forcePlayerFollow) return;

        if (room.CameraFollow && playerTransform != null)
        {
            // Follow 켜진 방: 기존대로 유지
            orthoSize = cam.orthographicSize;
            cineCamera.Follow = playerTransform;
        }
        else
        {
            // Follow 꺼진 방: OrthographicSize를 5.5로 고정
            orthoSize = 5.5f;
            cam.orthographicSize = orthoSize;
            var vCam = cineCamera.GetComponent<CinemachineCamera>();
            if (vCam != null) vCam.Lens.OrthographicSize = orthoSize;

            Vector3 center = bounds.center;
            cam.transform.position = new Vector3(center.x, center.y, cam.transform.position.z);
            cineCamera.transform.position = cam.transform.position;
            cineCamera.Follow = null;
        }
    }


    private void DestroyAllEnemies()
    {
        string[] enemyTags = { "Enemy", "DashEnemy", "LongRangeEnemy", "PotionEnemy" };
        foreach (string tag in enemyTags)
        {
            foreach (GameObject enemy in GameObject.FindGameObjectsWithTag(tag)) Destroy(enemy);
        }
    }
}

//using DG.Tweening;
//using System.Collections;
//using System.Collections.Generic;
//using Unity.Cinemachine;
//using UnityEngine;
//using UnityEngine.AI;

//[System.Serializable]
//public class RoomData
//{
//    public string roomName;
//    public GameObject roomPrefab;
//    [Header("Room 판정용 Collider")]
//    public Collider2D roomCollider;
//    [Header("Camera Confiner Collider")]
//    public Collider2D cameraCollider;
//    public List<GameObject> enemyPrefabs;
//    public List<MovingWall> movingWalls;
//    [HideInInspector]
//    public bool activated = false;
//    [Header("카메라 Follow 설정")]
//    public bool CameraFollow = true;
//    [Header("이벤트 씬 설정")]
//    public bool eventSceneEnabled = false;
//    public Transform eventStartPos;
//    public Transform eventEndPos;
//    public GameObject eventObjectPrefab;
//    public float eventMoveDuration = 3f;
//    [Header("방 시작 시 기존 적 제거 여부")]
//    public bool clearPreviousEnemies = true; // ← Room별로 설정 가능
//}

//public class WaveManager : MonoBehaviour
//{
//    [Header("모든 방 데이터")]
//    public List<RoomData> rooms;
//    [Header("플레이어")]
//    public Transform playerTransform;
//    [Header("카메라")]
//    public CinemachineCamera cineCamera;
//    public float cameraMoveDuration = 0.5f;
//    [Header("경고 이펙트")]
//    public GameObject warningEffectPrefab;
//    public float warningDuration = 1f;
//    [Header("문 프리팹 부모")]
//    public GameObject doorParentPrefab;
//    [Header("문 애니메이션 프리팹 부모")]
//    public GameObject doorAnimationParentPrefab;
//    [Header("스폰 관련")]
//    public float spawnStop = 0f;
//    [Tooltip("방 시작 시 기존 방 적을 모두 제거할지 여부")]
//    public bool clearPreviousEnemies = true;

//    private List<DoorController> allDoors = new List<DoorController>();
//    private List<DoorAnimation> allDoorAnimations = new List<DoorAnimation>();
//    private RoomData currentRoom;
//    private bool cleared = false;
//    private bool isSpawning = false;
//    private bool isFirstRoom = true;
//    private bool isEventRunning = false;

//    void Start()
//    {
//        if (doorParentPrefab != null) allDoors.AddRange(doorParentPrefab.GetComponentsInChildren<DoorController>(true));
//        if (doorAnimationParentPrefab != null) allDoorAnimations.AddRange(doorAnimationParentPrefab.GetComponentsInChildren<DoorAnimation>(true));
//    }

//    void Update()
//    {
//        if (!isSpawning && !isEventRunning)
//        {
//            RoomData room = GetPlayerRoom();
//            if (room != null && room != currentRoom)
//            {
//                // 이전 방 초기화
//                if (cineCamera != null) cineCamera.Follow = null;
//                var confiner = cineCamera.GetComponent<CinemachineConfiner2D>();
//                if (confiner != null) confiner.BoundingShape2D = null;
//                currentRoom = room;

//                // 🔹 카메라 먼저 이동 (이벤트 유무에 따라 목표 위치 변경)
//                StartCoroutine(MoveCameraToRoomAndStart(room));
//            }
//        }
//    }

//    /// <summary>
//    /// 새 방으로 진입 시 카메라를 먼저 이동시키고, 완료 후 StartRoom 실행
//    /// </summary>
//    IEnumerator MoveCameraToRoomAndStart(RoomData room)
//    {
//        if (room.cameraCollider == null) yield break;

//        // 💡 카메라 목표 위치 설정: 이벤트 씬이 활성화된 경우 이벤트 시작 위치, 아니면 방 중앙
//        Vector3 targetPos;
//        if (room.eventSceneEnabled && room.eventStartPos != null)
//        {
//            // 이벤트 시작 위치로 설정
//            targetPos = room.eventStartPos.position;
//        }
//        else
//        {
//            // 방 중앙으로 설정 (기존 로직)
//            targetPos = room.cameraCollider.bounds.center;
//        }

//        targetPos.z = cineCamera.transform.position.z;

//        // 카메라 Confiner 적용 (이동 중에는 Confiner만 적용하고 Follow는 설정하지 않음)
//        ApplyCameraConfiner(room, forcePlayerFollow: false);

//        // 카메라 이동
//        cineCamera.transform.DOMove(targetPos, cameraMoveDuration).SetEase(Ease.InOutQuad);
//        yield return new WaitForSeconds(cameraMoveDuration);

//        // ✅ 카메라 Follow 설정: 이동 완료 후 CameraFollow 설정에 따름
//        if (room.CameraFollow && cineCamera != null)
//        {
//            cineCamera.Follow = playerTransform;
//            ApplyCameraConfiner(room, forcePlayerFollow: true);
//            // 여기에서 Size를 5.5로 고정
//            cineCamera.Lens.OrthographicSize = 5.5f; // <-------
//        }
//        else if (cineCamera != null)
//        {
//            cineCamera.Follow = null;
//            ApplyCameraConfiner(room, forcePlayerFollow: false);
//        }

//        if (!room.activated)
//        {
//            room.activated = true;
//            if (room.movingWalls != null)
//            {
//                foreach (var wall in room.movingWalls)
//                    if (wall != null) wall.isActive = true;
//            }
//            yield return StartCoroutine(StartRoom(room));
//        }
//    }

//    public RoomData GetPlayerRoom()
//    {
//        if (playerTransform == null) return null;
//        Collider2D[] hits = Physics2D.OverlapCircleAll(playerTransform.position, 0.1f);
//        foreach (var hit in hits)
//        {
//            foreach (var room in rooms)
//            {
//                if (hit == room.roomCollider) return room;
//            }
//        }
//        return null;
//    }

//    IEnumerator StartRoom(RoomData room)
//    {
//        if (clearPreviousEnemies) DestroyAllEnemies();

//        isSpawning = true;
//        cleared = false;
//        if (!isFirstRoom) CloseDoors();
//        yield return new WaitForSeconds(0.3f);

//        // -------- 이벤트 씬 처리 --------
//        if (room.eventSceneEnabled && room.eventObjectPrefab != null && room.eventStartPos != null && room.eventEndPos != null)
//        {
//            // MoveCameraToRoomAndStart에서 이미 이벤트 시작 위치로 카메라를 이동했기 때문에
//            // 여기서는 추가적인 카메라 이동 없이 바로 이벤트 오브젝트 팔로우로 전환
//            isEventRunning = true;

//            // 🔹 플레이어 이동 제한
//            // ... (GameManager 로직 생략) ...

//            // 🔹 이벤트 오브젝트 생성
//            GameObject eventObj = Instantiate(room.eventObjectPrefab, room.eventStartPos.position, Quaternion.identity);

//            // 🔹 카메라 Follow 이벤트 오브젝트로 전환 (이동 중이라도)
//            if (cineCamera != null)
//            {
//                // Follow를 null로 설정하고 Confiner 업데이트 후,
//                // 즉시 이벤트 오브젝트를 Follow하여 이벤트 시퀀스 시작
//                cineCamera.Follow = null;
//                ApplyCameraConfiner(room, forcePlayerFollow: false);
//                yield return new WaitForSeconds(0.05f); // Confiner 반영 대기
//                cineCamera.Follow = eventObj.transform;
//            }

//            // 🔹 이벤트 이동 애니메이션
//            eventObj.transform.DOMove(room.eventEndPos.position, room.eventMoveDuration)
//                .SetEase(Ease.Linear)
//                .OnComplete(() => {
//                    Destroy(eventObj);

//                    // ✅ 이벤트 종료 후 플레이어 팔로우 복귀
//                    if (cineCamera != null)
//                    {
//                        // room.CameraFollow 설정에 따라 Follow 복귀
//                        cineCamera.Follow = room.CameraFollow ? playerTransform : null;
//                    }
//                    ApplyCameraConfiner(room);
//                    // ... (플레이어 이동 제한 해제 로직 생략) ...
//                    isEventRunning = false;
//                });

//            yield return new WaitForSeconds(room.eventMoveDuration + 0.2f);
//        }

//        // -------- 적 스폰 --------
//        // ... (나머지 StartRoom 로직 유지) ...

//        foreach (var prefab in room.enemyPrefabs)
//        {
//            GameObject tempObj = Instantiate(prefab, prefab.transform.position, prefab.transform.rotation);
//            tempObj.SetActive(false);
//            foreach (Transform child in tempObj.transform) ShowWarningEffect(child.position);
//            yield return new WaitForSeconds(warningDuration);
//            tempObj.SetActive(true);
//            EnemyBase enemyBase = tempObj.GetComponent<EnemyBase>();
//            if (enemyBase != null)
//            {
//                enemyBase.CanMove = false;
//                yield return new WaitForSeconds(spawnStop);
//                enemyBase.CanMove = true;
//            }
//            while (true)
//            {
//                int enemiesLeft = GameObject.FindGameObjectsWithTag("Enemy").Length + GameObject.FindGameObjectsWithTag("DashEnemy").Length + GameObject.FindGameObjectsWithTag("LongRangeEnemy").Length + GameObject.FindGameObjectsWithTag("PotionEnemy").Length;
//                if (enemiesLeft == 0) break;
//                yield return new WaitForSeconds(0.5f);
//            }
//        }
//        cleared = true;
//        if (GameManager.Instance.cameraShake != null)
//        {
//            for (int i = 0; i < 7; i++)
//            {
//                GameManager.Instance.cameraShake.GenerateImpulse();
//                yield return new WaitForSeconds(0.1f);
//            }
//        }
//        OpenDoors();
//        if (room.movingWalls != null)
//        {
//            foreach (var wall in room.movingWalls) wall?.ResetWall();
//        }
//        isSpawning = false;
//        if (isFirstRoom) isFirstRoom = false;
//    }

//    // ... (ShowWarningEffect, CloseDoors, OpenDoors, ApplyCameraConfiner, DestroyAllEnemies 함수는 동일) ...

//    void ShowWarningEffect(Vector3 pos)
//    {
//        if (warningEffectPrefab == null) return;
//        GameObject warning = Instantiate(warningEffectPrefab, pos, Quaternion.identity);
//        SpriteRenderer sr = warning.GetComponent<SpriteRenderer>();
//        if (sr != null)
//        {
//            sr.color = new Color(1, 0, 0, 0);
//            sr.DOFade(1f, 0.3f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutQuad);
//        }
//        Destroy(warning, warningDuration);
//    }

//    void CloseDoors()
//    {
//        foreach (var door in allDoors)
//        {
//            door.CloseDoor();
//            if (door.TryGetComponent<Collider2D>(out var col)) col.isTrigger = false;
//        }
//        foreach (var anim in allDoorAnimations) anim.PlayAnimation(DoorAnimation.DoorState.Closed);
//    }

//    void OpenDoors()
//    {
//        foreach (var door in allDoors)
//        {
//            door.OpenDoor();
//            if (door.TryGetComponent<Collider2D>(out var col)) col.isTrigger = true;
//        }
//        foreach (var anim in allDoorAnimations) anim.PlayAnimation(DoorAnimation.DoorState.Open);
//    }

//    public void ApplyCameraConfiner(RoomData room, bool forcePlayerFollow = true)
//    {
//        if (cineCamera == null || room.cameraCollider == null) return;
//        var confiner = cineCamera.GetComponent<CinemachineConfiner2D>();
//        if (confiner != null && confiner.BoundingShape2D != room.cameraCollider)
//        {
//            confiner.BoundingShape2D = room.cameraCollider;
//            confiner.InvalidateBoundingShapeCache();
//        }

//        Camera cam = Camera.main;
//        if (cam == null || !cam.orthographic) return;

//        Bounds bounds = room.cameraCollider.bounds;
//        float screenRatio = (float)Screen.width / Screen.height;
//        float boundsRatio = bounds.size.x / bounds.size.y;
//        float orthoSize;

//        if (room.eventSceneEnabled && !forcePlayerFollow) return;

//        if (room.CameraFollow && playerTransform != null)
//        {
//            // Follow 켜진 방: 기존대로 유지
//            orthoSize = cam.orthographicSize;
//            cineCamera.Follow = playerTransform;
//        }
//        else
//        {
//            // Follow 꺼진 방: OrthographicSize를 5.5로 고정
//            orthoSize = 5.5f;
//            cam.orthographicSize = orthoSize;
//            var vCam = cineCamera.GetComponent<CinemachineCamera>();
//            if (vCam != null) vCam.Lens.OrthographicSize = orthoSize;

//            Vector3 center = bounds.center;
//            cam.transform.position = new Vector3(center.x, center.y, cam.transform.position.z);
//            cineCamera.transform.position = cam.transform.position;
//            cineCamera.Follow = null;
//        }
//    }


//    private void DestroyAllEnemies()
//    {
//        string[] enemyTags = { "Enemy", "DashEnemy", "LongRangeEnemy", "PotionEnemy" };
//        foreach (string tag in enemyTags)
//        {
//            foreach (GameObject enemy in GameObject.FindGameObjectsWithTag(tag)) Destroy(enemy);
//        }
//    }
//}

