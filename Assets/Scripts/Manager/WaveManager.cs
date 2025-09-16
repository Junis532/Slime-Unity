using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

[System.Serializable]
public class RoomData
{
    public string roomName;
    public GameObject roomPrefab;        // 방 프리팹
    public Collider2D roomCollider;      // 방 Collider
    public List<GameObject> enemyPrefabs; // 적 리스트
    public List<DoorController> doors;    // 문 리스트

    [HideInInspector]
    public bool activated = false;       // 이미 활성화 여부

    [Header("카메라 Follow 설정")]
    public bool CameraFollow = true;     // 이 방에서 카메라 Follow 여부
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

    private RoomData currentRoom;
    private bool cleared = false;
    private bool isSpawning = false;
    void Update()
    {
        if (!isSpawning)
        {
            RoomData room = GetPlayerRoom();
            if (room != null)
            {
                // 카메라 이동은 항상 수행
                ApplyCameraConfiner(room);

                // 아직 활성화되지 않은 방만 적 스폰
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

        // 카메라 Confiner 적용 & 방 중앙으로 이동
        ApplyCameraConfiner(room);

        // 방 문 닫기
        CloseDoors(room);

        yield return new WaitForSeconds(0.5f);

        // 적 스폰 (방 중심 좌표)
        Vector3 spawnPosition = room.roomPrefab.transform.position;

        foreach (var prefab in room.enemyPrefabs)
        {
            // 임시 오브젝트 생성 (비활성)
            GameObject tempObj = Instantiate(prefab, spawnPosition, Quaternion.identity);
            tempObj.SetActive(false);

            // 자식만 경고 표시 (자식의 자식 제외)
            foreach (Transform child in tempObj.transform)
            {
                ShowWarningEffect(child.position);
            }

            // 경고 시간 대기
            yield return new WaitForSeconds(warningDuration);

            // 실제 스폰
            tempObj.SetActive(true);
        }

        // 모든 적이 죽었는지 확인
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
                OpenDoors(room);
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

    void CloseDoors(RoomData room)
    {
        foreach (var door in room.doors)
            door.CloseDoor();
    }

    void OpenDoors(RoomData room)
    {
        foreach (var door in room.doors)
            door.OpenDoor();
    }

    void ApplyCameraConfiner(RoomData room)
    {
        if (cineCamera == null || room.roomCollider == null) return;

        var confiner = cineCamera.GetComponent<CinemachineConfiner2D>();
        if (confiner != null && confiner.BoundingShape2D != room.roomCollider)
        {
            confiner.BoundingShape2D = room.roomCollider;
            confiner.InvalidateBoundingShapeCache();
        }

        if (room.CameraFollow)
        {
            // Follow 켜기: 플레이어 위치만 따라가고, 방 중앙 이동 무시
            cineCamera.Follow = playerTransform;
        }
        else
        {
            // Follow 끄기
            cineCamera.Follow = null;

            // Follow가 꺼진 경우만 DOTween으로 방 중앙으로 이동
            Vector3 center = room.roomCollider.bounds.center;
            cineCamera.transform.DOMove(new Vector3(center.x, center.y, cineCamera.transform.position.z), cameraMoveDuration);
        }
    }


}
