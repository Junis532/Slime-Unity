using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

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

    private List<DoorController> allDoors = new List<DoorController>();
    private RoomData currentRoom;
    private bool cleared = false;
    private bool isSpawning = false;

    void Start()
    {
        // ✅ 부모 프리팹에서 모든 문 자동 수집
        if (doorParentPrefab != null)
        {
            allDoors.AddRange(doorParentPrefab.GetComponentsInChildren<DoorController>(true));
        }
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

    RoomData GetPlayerRoom()
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
        CloseDoors(); // ✅ 모든 문 닫기

        yield return new WaitForSeconds(0.5f);

        foreach (var prefab in room.enemyPrefabs)
        {
            GameObject tempObj = Instantiate(prefab, prefab.transform.position, prefab.transform.rotation);
            tempObj.SetActive(false);

            foreach (Transform child in tempObj.transform)
            {
                ShowWarningEffect(child.position);
            }

            yield return new WaitForSeconds(warningDuration);
            tempObj.SetActive(true);
        }

        while (!cleared)
        {
            yield return new WaitForSeconds(1f);

            int totalEnemies = 0;
            totalEnemies += GameObject.FindGameObjectsWithTag("Enemy").Length;
            totalEnemies += GameObject.FindGameObjectsWithTag("DashEnemy").Length;
            totalEnemies += GameObject.FindGameObjectsWithTag("LongRangeEnemy").Length;
            totalEnemies += GameObject.FindGameObjectsWithTag("PotionEnemy").Length;

            if (totalEnemies == 0)
            {
                cleared = true;
                if (GameManager.Instance.cameraShake != null)
                {
                    for (int i = 0; i < 7; i++)
                    {
                        GameManager.Instance.cameraShake.GenerateImpulse();
                        yield return new WaitForSeconds(0.1f);
                    }
                    OpenDoors();
                }
             
                Debug.Log($"[WaveManager] 방 '{room.roomName}' 클리어됨!");
            }
        }

        isSpawning = false;
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
            door.CloseDoor();
    }

    void OpenDoors()
    {
        foreach (var door in allDoors)
            door.OpenDoor();
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

        if (room.CameraFollow)
        {
            // 🚫 Follow 끄지 말고 그대로 둔다
            cineCamera.Follow = playerTransform;
        }
        else
        {
            cineCamera.Follow = null;
            Vector3 center = room.cameraCollider.bounds.center;
            cineCamera.transform.position = new Vector3(center.x, center.y, cineCamera.transform.position.z);
        }
    }

}
