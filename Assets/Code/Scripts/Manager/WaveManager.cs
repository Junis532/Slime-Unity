using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using UnityEngine.Video;

[System.Serializable]
public class RoomData
{
    public string roomName;
    public GameObject roomPrefab;

    [Header("Room 판정용 Collider")]
    public Collider2D roomCollider;

    [Header("Camera Confiner Collider")]
    public Collider2D cameraCollider;

    [Header("적 스폰 설정")]
    public List<GameObject> enemyPrefabs;
    public bool spawnImmediately = false; // true면 경고 없이 바로 소환

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
    public bool clearPreviousEnemies = true;

    [Header("MP4 영상 재생 설정")]
    public bool playVideoOnEnter = false;       // 방 입장 시 영상 재생 여부
    public VideoClip roomVideoClip;             // 재생할 영상
    public bool skipWithInput = true;           // 스킵 가능 여부
    public float videoFadeDuration = 1f;        // 페이드 아웃 시간
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
                // 이전 방 초기화 (카메라 Follow 및 Confiner 초기화는 MoveCameraToRoomAndStart에서 수행됨)
                if (cineCamera != null) cineCamera.Follow = null;
                var confiner = cineCamera.GetComponent<CinemachineConfiner2D>();
                if (confiner != null) confiner.BoundingShape2D = null;
                currentRoom = room;

                // 🔹 카메라 이동 + 방 시작
                StartCoroutine(MoveCameraToRoomAndStart(room));
            }
        }
    }

    IEnumerator MoveCameraToRoomAndStart(RoomData room)
    {
        if (room.cameraCollider == null) yield break;

        // 목표 위치 설정
        Vector3 targetPos;
        if (room.eventSceneEnabled && room.eventStartPos != null)
            targetPos = room.eventStartPos.position;
        else
            targetPos = room.cameraCollider.bounds.center;

        targetPos.z = cineCamera.transform.position.z;

        ApplyCameraConfiner(room, forcePlayerFollow: false);

        // 카메라 이동
        cineCamera.transform.DOMove(targetPos, cameraMoveDuration).SetEase(Ease.InOutQuad);
        yield return new WaitForSeconds(cameraMoveDuration);

        // Follow 설정
        if (room.CameraFollow && cineCamera != null)
        {
            cineCamera.Follow = playerTransform;
            ApplyCameraConfiner(room, forcePlayerFollow: true);
            cineCamera.Lens.OrthographicSize = 5.5f;
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

        // 🎬 영상 재생 시작
        Coroutine videoCoroutine = null;
        if (room.playVideoOnEnter && room.roomVideoClip != null)
        {
            videoCoroutine = StartCoroutine(PlayRoomVideo(room));
            yield return null; // 영상 준비 바로 후 다음 코드 실행
        }

        // -------- 영상 시작과 동시에 적 소환 --------
        List<EnemyBase> spawnedEnemies = new List<EnemyBase>();
        foreach (var prefab in room.enemyPrefabs)
        {
            GameObject tempObj = Instantiate(prefab, prefab.transform.position, prefab.transform.rotation);

            EnemyBase enemyBase = tempObj.GetComponent<EnemyBase>();
            if (enemyBase != null)
            {
                enemyBase.CanMove = false; // 영상 끝날 때까지 정지
                spawnedEnemies.Add(enemyBase);
            }
        }

        // -------- 영상 종료 후 적 이동 재개 --------
        if (videoCoroutine != null)
        {
            yield return videoCoroutine;

            foreach (var enemy in spawnedEnemies)
            {
                if (enemy != null)
                    enemy.CanMove = true;
            }
        }

        // -------- 적 제거 대기 --------
        while (true)
        {
            int enemiesLeft = GameObject.FindGameObjectsWithTag("Enemy").Length +
                              GameObject.FindGameObjectsWithTag("DashEnemy").Length +
                              GameObject.FindGameObjectsWithTag("LongRangeEnemy").Length +
                              GameObject.FindGameObjectsWithTag("PotionEnemy").Length;
            if (enemiesLeft == 0) break;
            yield return new WaitForSeconds(0.5f);
        }

        cleared = true;
        OpenDoors();
        // MovingWall은 Reset하지 않음 → 게임 끝나도 유지
        isSpawning = false;
        if (isFirstRoom) isFirstRoom = false;
    }
    private IEnumerator PlayRoomVideo(RoomData room)
    {
        if (!room.playVideoOnEnter || room.roomVideoClip == null)
            yield break;

        isEventRunning = true;

        // 🔹 게임 시간 정지
        float originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        // 🔹 BGM 일시정지
        if (AudioManager.Instance?.bgmSource != null)
            AudioManager.Instance.bgmSource.Pause();

        // 🔹 카메라 Follow 해제
        Transform originalFollow = null;
        if (cineCamera != null)
        {
            originalFollow = cineCamera.Follow;
            cineCamera.Follow = null;
            yield return null;
        }

        // 🔹 Canvas & RawImage & VideoPlayer 생성
        GameObject canvasObj = new GameObject("RoomVideoCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        canvasObj.AddComponent<GraphicRaycaster>();

        CanvasGroup canvasGroup = canvasObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = true;

        GameObject rawImageObj = new GameObject("RoomVideo");
        rawImageObj.transform.SetParent(canvasObj.transform, false);
        RectTransform rect = rawImageObj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        RawImage rawImage = rawImageObj.AddComponent<RawImage>();
        rawImage.color = Color.white;

        // 🔹 AspectRatioFitter 적용: 화면 꽉 채우면서 원본 비율 유지
        AspectRatioFitter ar = rawImageObj.AddComponent<AspectRatioFitter>();
        ar.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        ar.aspectRatio = (float)room.roomVideoClip.width / room.roomVideoClip.height;

        VideoPlayer vp = rawImageObj.AddComponent<VideoPlayer>();
        vp.playOnAwake = false;
        vp.renderMode = VideoRenderMode.APIOnly;
        vp.source = VideoSource.VideoClip;
        vp.clip = room.roomVideoClip;
        vp.isLooping = false;
        vp.audioOutputMode = VideoAudioOutputMode.AudioSource;

        AudioSource audioSource = rawImageObj.AddComponent<AudioSource>();
        vp.SetTargetAudioSource(0, audioSource);

        vp.Prepare();
        while (!vp.isPrepared)
            yield return null;

        rawImage.texture = vp.texture;
        vp.Play();
        audioSource.Play();

        // 🔹 DOTween 페이드인 (Time.timeScale = 0에서도 진행되도록 SetUpdate(true))
        canvasGroup.alpha = 0f;
        yield return canvasGroup.DOFade(1f, room.videoFadeDuration).SetUpdate(true).WaitForCompletion();

        // 🔹 영상 스킵 또는 종료 대기
        bool isVideoFinished = false;
        vp.loopPointReached += (source) => isVideoFinished = true;

        while (!isVideoFinished)
        {
            if (room.skipWithInput && Input.anyKeyDown)
            {
                vp.Stop();
                isVideoFinished = true;
            }
            yield return null;
        }

        // 🔹 페이드아웃 전에 게임 시간 원래대로 복구
        Time.timeScale = originalTimeScale;

        // 🔹 DOTween 페이드아웃
        yield return canvasGroup.DOFade(0f, room.videoFadeDuration).SetUpdate(true).WaitForCompletion();

        Destroy(canvasObj);

        // 🔹 BGM 다시 재생
        if (AudioManager.Instance?.bgmSource != null)
            AudioManager.Instance.bgmSource.UnPause();

        // 🔹 카메라 Follow 복구
        if (cineCamera != null)
            cineCamera.Follow = originalFollow;

        isEventRunning = false;

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

    // DoorController, DoorAnimation 클래스는 정의되지 않았지만,
    // 이 메서드들은 해당 클래스의 존재를 가정하고 작성되었습니다.
    void CloseDoors()
    {
        foreach (var door in allDoors)
        {
            // door.CloseDoor() 및 TryGetComponent 로직은 DoorController 정의 필요
            // door.CloseDoor(); 
            // if (door.TryGetComponent<Collider2D>(out var col)) col.isTrigger = false;
        }
        // foreach (var anim in allDoorAnimations) anim.PlayAnimation(DoorAnimation.DoorState.Closed);
    }

    void OpenDoors()
    {
        foreach (var door in allDoors)
        {
            // door.OpenDoor() 및 TryGetComponent 로직은 DoorController 정의 필요
            // door.OpenDoor();
            // if (door.TryGetComponent<Collider2D>(out var col)) col.isTrigger = true;
        }
        // foreach (var anim in allDoorAnimations) anim.PlayAnimation(DoorAnimation.DoorState.Open);
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
        if (room.eventSceneEnabled && !forcePlayerFollow) return;

        if (room.CameraFollow && playerTransform != null)
        {
            cineCamera.Follow = playerTransform;
        }
        else
        {
            float orthoSize = 5.5f;
            cam.orthographicSize = orthoSize;
            var vCam = cineCamera.GetComponent<CinemachineCamera>();
            if (vCam != null) vCam.Lens.OrthographicSize = orthoSize;

            Vector3 center = bounds.center;
            cam.transform.position = new Vector3(center.x, center.y, cam.transform.position.z);
            // vCam의 위치도 업데이트
            if (vCam != null) vCam.transform.position = cam.transform.position;
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