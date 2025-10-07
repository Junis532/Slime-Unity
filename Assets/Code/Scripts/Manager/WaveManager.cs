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

    [Header("적 스폰 설정")]
    public List<GameObject> enemyPrefabs;
    public List<MovingWall> movingWalls;

    [HideInInspector]
    public bool activated = false;

    [Header("카메라 Follow 설정")]
    public bool CameraFollow = true;

    [Header("카메라 줌인 팔로우 설정")]
    public bool zoomInCameraFollow = false; // 🔹 전체 → 줌인 전환
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
                if (cineCamera != null) cineCamera.Follow = null;
                var confiner = cineCamera.GetComponent<CinemachineConfiner2D>();
                if (confiner != null) confiner.BoundingShape2D = null;

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
    IEnumerator MoveCameraToRoomAndStart(RoomData room)
    {
        if (room == null || room.cameraCollider == null)
        {
            Debug.LogWarning("Room or cameraCollider is null!");
            yield break;
        }

        Camera cam = Camera.main;
        if (cam == null) yield break;

        PlayerController playerCtrl = playerTransform.GetComponent<PlayerController>();
        if (playerCtrl != null) playerCtrl.canMove = false; // 플레이어 이동 금지

        cleared = false;
        CloseDoors();
        SetAllEnemiesAI(false);
        SetAllBulletSpawnersActive(false);

        // 방 적 미리 소환
        foreach (var prefab in room.enemyPrefabs)
        {
            GameObject tempObj = Instantiate(prefab, prefab.transform.position, prefab.transform.rotation);
            tempObj.SetActive(false);
            EnemyBase enemyBase = tempObj.GetComponent<EnemyBase>();
            if (enemyBase != null) enemyBase.CanMove = false;

            foreach (Transform child in tempObj.transform)
                ShowWarningEffect(child.position);

            yield return null;
            tempObj.SetActive(true);
        }

        // 1️⃣ 줌 아웃 (Room 전체가 보이도록)
        Bounds bounds = room.cameraCollider.bounds;
        float screenRatio = (float)Screen.width / Screen.height;

        float targetOrthoSize = bounds.size.y / 2f;
        float camHalfWidth = targetOrthoSize * screenRatio;
        if (camHalfWidth < bounds.size.x / 2f)
            targetOrthoSize = bounds.size.x / 2f / screenRatio;

        targetOrthoSize = Mathf.Clamp(targetOrthoSize, 3f, 12f);

        Sequence zoomOutSeq = DOTween.Sequence();
        zoomOutSeq.Append(cineCamera.transform.DOMove(new Vector3(bounds.center.x, bounds.center.y, cineCamera.transform.position.z), cameraMoveDuration).SetEase(Ease.InOutSine));
        zoomOutSeq.Join(DOTween.To(() => cam.orthographicSize, x => cam.orthographicSize = x, targetOrthoSize, 0.6f).SetEase(Ease.InOutSine));
        zoomOutSeq.Join(DOTween.To(() => cineCamera.Lens.OrthographicSize, x => cineCamera.Lens.OrthographicSize = x, targetOrthoSize, 0.6f).SetEase(Ease.InOutSine));
        yield return zoomOutSeq.WaitForCompletion();

        // 줌인 Delay
        yield return new WaitForSeconds(room.zoomInDelay);

        // 2️⃣ 줌 인 (플레이어 중심, Collider 안으로 제한)
        if (room.zoomInCameraFollow)
        {
            Bounds camBounds = room.cameraCollider.bounds;
            Vector3 targetPos = playerTransform.position;
            targetPos.z = cineCamera.transform.position.z;

            float camHalfHeight = room.zoomInTargetSize;
            float camHalfWidthIn = camHalfHeight * cam.aspect;

            targetPos.x = Mathf.Clamp(targetPos.x, camBounds.min.x + camHalfWidthIn, camBounds.max.x - camHalfWidthIn);
            targetPos.y = Mathf.Clamp(targetPos.y, camBounds.min.y + camHalfHeight, camBounds.max.y - camHalfHeight);

            // 줌 인 시작 전에는 Follow 끄기
            cineCamera.Follow = null;

            Sequence zoomInSeq = DOTween.Sequence();
            zoomInSeq.Append(cineCamera.transform.DOMove(targetPos, room.zoomInDuration).SetEase(Ease.InOutSine));
            zoomInSeq.Join(DOTween.To(() => cineCamera.Lens.OrthographicSize, x => cineCamera.Lens.OrthographicSize = x, room.zoomInTargetSize, room.zoomInDuration).SetEase(Ease.InOutSine));

            // 줌인 완료까지 대기
            yield return zoomInSeq.WaitForCompletion();
        }

        // 3️⃣ 줌인 완료 후 Follow 적용 + Confiner 적용 + 플레이어 이동 허용
        cineCamera.Follow = playerTransform;
        ApplyCameraConfiner(room);

        if (!room.activated && room.movingWalls != null)
        {
            room.activated = true;
            foreach (var wall in room.movingWalls)
                wall.isActive = true;
        }

        if (playerCtrl != null)
            playerCtrl.canMove = true;

        DOVirtual.DelayedCall(1.5f, () => SetAllBulletSpawnersActive(true));
        SetAllEnemiesAI(true);
        StartCoroutine(CheckEnemiesCleared(room));
    }

    IEnumerator CheckEnemiesCleared(RoomData room)
    {
        if (cleared) yield break;

        while (true)
        {
            int enemiesLeft =
                GameObject.FindGameObjectsWithTag("Enemy").Length +
                GameObject.FindGameObjectsWithTag("DashEnemy").Length +
                GameObject.FindGameObjectsWithTag("LongRangeEnemy").Length +
                GameObject.FindGameObjectsWithTag("PotionEnemy").Length;

            if (enemiesLeft == 0)
            {
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
                    foreach (var wall in room.movingWalls)
                        wall?.ResetWall();
                }

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
        Collider2D col = (room != null && room.cameraCollider != null) ? room.cameraCollider : null;
        if (confiner != null && confiner.BoundingShape2D != col)
        {
            confiner.BoundingShape2D = col;
            confiner.InvalidateBoundingShapeCache();
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