using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
public class Bridge : MonoBehaviour
{
    [HideInInspector] public Vector3 bridgeDelta;
    private Vector3 lastPosition;
    private bool isPlayerOnBridge = false;

    [Header("플레이어 Transform")]
    public Transform player;

    [Header("레이어 이름")]
    public string passableLayer = "Wall_Passable";
    public string blockLayer = "Wall_Block";
    public string bridgePassLayer = "BridgePass";   // 다리 위에서만 통과 가능
    public string projectileLayer = "Projectile";   // Projectile 레이어 (항상 충돌)

    private Collider2D bridgeCollider;
    private Collider2D playerCollider;

    private List<Collider2D> bridgePassColliders = new List<Collider2D>();

    void Start()
    {
        lastPosition = transform.position;
        bridgeCollider = GetComponent<Collider2D>();

        if (bridgeCollider == null)
            Debug.LogError("❌ 다리에 Collider2D가 없습니다!");

        if (player != null)
        {
            playerCollider = player.GetComponent<Collider2D>();
            if (playerCollider == null)
                Debug.LogError("❌ 플레이어에 Collider2D가 필요합니다!");
        }

        // BridgePass 레이어 콜라이더 수집
        CollectLayerColliders(bridgePassLayer, bridgePassColliders);
    }

    void Update()
    {
        if (playerCollider == null || bridgeCollider == null) return;

        // 플레이어가 다리 안에 완전히 포함되어 있는지 확인
        bool nowOnBridge = bridgeCollider.bounds.Contains(playerCollider.bounds.min) &&
                           bridgeCollider.bounds.Contains(playerCollider.bounds.max);

        if (nowOnBridge != isPlayerOnBridge)
        {
            isPlayerOnBridge = nowOnBridge;
            gameObject.layer = LayerMask.NameToLayer(isPlayerOnBridge ? passableLayer : blockLayer);

            // 플레이어만 통과 가능하게 BridgePass 레이어 콜라이더 설정
            foreach (var col in bridgePassColliders)
            {
                if (col != null)
                    Physics2D.IgnoreCollision(playerCollider, col, isPlayerOnBridge);
            }
        }

        // 다리 이동량 계산
        bridgeDelta = transform.position - lastPosition;
        lastPosition = transform.position;
    }


    private void CollectLayerColliders(string layerName, List<Collider2D> targetList)
    {
        int layerIndex = LayerMask.NameToLayer(layerName);
        if (layerIndex < 0)
        {
            Debug.LogError($"❌ '{layerName}' 레이어가 존재하지 않습니다.");
            return;
        }

        targetList.Clear();
        var allCols = Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None);
        foreach (var col in allCols)
        {
            if (col.gameObject.layer == layerIndex && !col.isTrigger)
                targetList.Add(col);
        }
    }

    public bool PlayerOnBridge() => isPlayerOnBridge;
}
