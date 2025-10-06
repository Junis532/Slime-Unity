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

    IEnumerator MoveCameraToRoomAndStart(RoomData room)
    {
        if (room.cameraCollider == null) yield break;

        Vector3 roomCenter = room.cameraCollider.bounds.center;
        roomCenter.z = cineCamera.transform.position.z;

        ApplyCameraConfiner(room, false);
        cineCamera.Follow = null;

        //// 🔹 플레이어 이동 제한
        //PlayerController playerCtrl = playerTransform.GetComponent<PlayerController>();
        //if (playerCtrl != null) playerCtrl.canMove = false;

        // 🔹 BulletSpawner 일시 중지
        SetAllBulletSpawnersActive(false);

        // 🔹 적 미리 소환 (즉시 활성화)
        foreach (var prefab in room.enemyPrefabs)
        {
            GameObject tempObj = Instantiate(prefab, prefab.transform.position, prefab.transform.rotation);
            tempObj.SetActive(false);

            foreach (Transform child in tempObj.transform)
                ShowWarningEffect(child.position);

            yield return null;
            tempObj.SetActive(true);
        }

        // 🔹 카메라 이동 & 줌아웃
        yield return cineCamera.transform.DOMove(roomCenter, cameraMoveDuration)
            .SetEase(Ease.InOutQuad)
            .WaitForCompletion();

        CloseDoors();

        // 🔹 카메라 줌 조정
        Camera cam = Camera.main;
        if (cam != null)
        {
            Bounds bounds = room.cameraCollider.bounds;
            float screenRatio = (float)Screen.width / Screen.height;
            float boundsRatio = bounds.size.x / bounds.size.y;
            float targetOrthoSize = (boundsRatio >= screenRatio)
                ? bounds.size.x / 2f / screenRatio
                : bounds.size.y / 2f;

            Sequence zoomOutSeq = DOTween.Sequence();
            zoomOutSeq.AppendInterval(0.2f);
            zoomOutSeq.Append(DOTween.To(() => cam.orthographicSize, x => cam.orthographicSize = x,
                targetOrthoSize, 0.6f).SetEase(Ease.InOutSine));
            zoomOutSeq.Join(DOTween.To(() => cineCamera.Lens.OrthographicSize, x => cineCamera.Lens.OrthographicSize = x,
                targetOrthoSize, 0.6f).SetEase(Ease.InOutSine));
            yield return zoomOutSeq.WaitForCompletion();
        }

        yield return new WaitForSeconds(0.2f);

        // 🔹 카메라 Follow 설정
        if (room.zoomInCameraFollow)
        {
            DOVirtual.DelayedCall(1.5f, () => SetAllBulletSpawnersActive(true));

            Sequence zoomInSeq = DOTween.Sequence();
            Vector3 endPos = playerTransform.position;
            endPos.z = cineCamera.transform.position.z;

            zoomInSeq.Append(cineCamera.transform.DOMove(endPos, room.zoomInDuration).SetEase(Ease.InOutSine));
            zoomInSeq.Join(DOTween.To(() => cineCamera.Lens.OrthographicSize, x => cineCamera.Lens.OrthographicSize = x,
                room.zoomInTargetSize, room.zoomInDuration).SetEase(Ease.InOutSine));
            yield return zoomInSeq.WaitForCompletion();

            cineCamera.Follow = playerTransform;
        }
        else if (room.CameraFollow)
        {
            cineCamera.Follow = playerTransform;
            cineCamera.Lens.OrthographicSize = 5.5f;
            DOVirtual.DelayedCall(1.5f, () => SetAllBulletSpawnersActive(true));
        }
        else
        {
            cineCamera.Follow = null;
            DOVirtual.DelayedCall(1.5f, () => SetAllBulletSpawnersActive(true));
        }

        //if (playerCtrl != null) playerCtrl.canMove = true;

        if (!room.activated)
        {
            room.activated = true;
            if (room.movingWalls != null)
                foreach (var wall in room.movingWalls)
                    wall.isActive = true;
        }

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

    public void ApplyCameraConfiner(RoomData room, bool forcePlayerFollow = true)
    {
        if (cineCamera == null || room.cameraCollider == null) return;
        var confiner = cineCamera.GetComponent<CinemachineConfiner2D>();
        if (confiner != null && confiner.BoundingShape2D != room.cameraCollider)
        {
            confiner.BoundingShape2D = room.cameraCollider;
            confiner.InvalidateBoundingShapeCache();
        }
    }
}
