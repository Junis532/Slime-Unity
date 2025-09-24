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

    [HideInInspector]
    public bool activated = false;

    [Header("카메라 Follow 설정")]
    public bool CameraFollow = true;
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

    [Header("문 프리팹 부모 (한 번만 넣기)")]
    public GameObject doorParentPrefab;

    [Header("문 애니메이션 프리팹 부모 (한 번만 넣기)")]
    public GameObject doorAnimationParentPrefab;

    public float spawnStop = 0f;

    private List<DoorController> allDoors = new List<DoorController>();
    private List<DoorAnimation> allDoorAnimations = new List<DoorAnimation>();
    private RoomData currentRoom;
    private bool cleared = false;
    private bool isSpawning = false;

    private bool isFirstRoom = true; // 첫 방 여부


    void Start()
    {
        if (doorParentPrefab != null)
            allDoors.AddRange(doorParentPrefab.GetComponentsInChildren<DoorController>(true));

        if (doorAnimationParentPrefab != null)
            allDoorAnimations.AddRange(doorAnimationParentPrefab.GetComponentsInChildren<DoorAnimation>(true));
    }

    void Update()
    {
        if (!isSpawning)
        {
            RoomData room = GetPlayerRoom();
            if (room != null)
            {
                ApplyCameraConfiner(room);

                if (!room.activated)
                {
                    room.activated = true;
                    currentRoom = room;
                    StartCoroutine(StartRoom(room));
                }
            }
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
                if (hit == room.roomCollider)
                    return room;
            }
        }
        return null;
    }

    IEnumerator StartRoom(RoomData room)
    {
        isSpawning = true;
        cleared = false;

        ApplyCameraConfiner(room);

        if (!isFirstRoom)
            CloseDoors();

        yield return new WaitForSeconds(0.5f);

        foreach (var prefab in room.enemyPrefabs)
        {
            GameObject tempObj = Instantiate(prefab, prefab.transform.position, prefab.transform.rotation);
            tempObj.SetActive(false);

            // 경고 이펙트
            foreach (Transform child in tempObj.transform)
                ShowWarningEffect(child.position);

            yield return new WaitForSeconds(warningDuration);

            tempObj.SetActive(true);

            // EnemyBase 계열이면 스폰 후 0.4초 멈춤 적용
            EnemyBase enemyBase = tempObj.GetComponent<EnemyBase>();
            if (enemyBase != null)
            {
                enemyBase.CanMove = false;           // 이동 막기
                yield return new WaitForSeconds(spawnStop);
                enemyBase.CanMove = true;            // 이동 허용
            }
            else
            {
                yield return new WaitForSeconds(spawnStop);
            }

            // 지금 소환한 적이 다 죽을 때까지 대기
            while (true)
            {
                int enemiesLeft = 0;
                enemiesLeft += GameObject.FindGameObjectsWithTag("Enemy").Length;
                enemiesLeft += GameObject.FindGameObjectsWithTag("DashEnemy").Length;
                enemiesLeft += GameObject.FindGameObjectsWithTag("LongRangeEnemy").Length;
                enemiesLeft += GameObject.FindGameObjectsWithTag("PotionEnemy").Length;

                if (enemiesLeft == 0)
                    break;

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
        Debug.Log($"[WaveManager] 방 '{room.roomName}' 클리어됨!");

        isSpawning = false;

        if (isFirstRoom)
            isFirstRoom = false;
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

            if (door.TryGetComponent<Collider2D>(out var col))
                col.isTrigger = false;
        }

        foreach (var anim in allDoorAnimations)
        {
            anim.PlayAnimation(DoorAnimation.DoorState.Closed);
        }
    }

    void OpenDoors()
    {
        foreach (var door in allDoors)
        {
            door.OpenDoor();

            if (door.TryGetComponent<Collider2D>(out var col))
                col.isTrigger = true;
        }

        foreach (var anim in allDoorAnimations)
        {
            anim.PlayAnimation(DoorAnimation.DoorState.Open);
        }
    }

    void ApplyCameraConfiner(RoomData room)
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
        if (screenRatio >= boundsRatio)
            orthoSize = bounds.size.y / 2f;
        else
            orthoSize = (bounds.size.x / 2f) / screenRatio;

        orthoSize *= 1.0f;

        cam.orthographicSize = orthoSize;

        var vCam = cineCamera.GetComponent<CinemachineCamera>();
        if (vCam != null)
            vCam.Lens.OrthographicSize = orthoSize;

        Vector3 center = bounds.center;

        if (room.CameraFollow)
            cineCamera.Follow = playerTransform;
        else
        {
            cineCamera.Follow = null;
            cam.transform.position = new Vector3(center.x, center.y, cam.transform.position.z);
            cineCamera.transform.position = cam.transform.position;
        }
    }
}
