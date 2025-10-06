using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
public class Bridge : MonoBehaviour
{
    private Vector3 lastPosition;
    [HideInInspector] public Vector3 bridgeDelta;
    private bool isPlayerOnBridge = false;

    [Header("플레이어 Transform")]
    public Transform player;

    [Header("레이어 이름")]
    public string passableLayer = "Wall_Passable";
    public string blockLayer = "Wall_Block";
    public string bridgePassLayer = "BridgePass"; // 다리 위에서만 통과 가능한 레이어

    private Collider2D bridgeCollider;
    private Collider2D playerCollider;
    private List<Collider2D> bridgePassColliders = new List<Collider2D>();

    void Start()
    {
        lastPosition = transform.position;

        bridgeCollider = GetComponent<Collider2D>();

        // ⚠️ 반드시 Trigger일 필요는 없음 (여기선 bounds로만 판정)
        if (bridgeCollider == null)
        {
            Debug.LogError("❌ 다리에 Collider2D가 없습니다!");
        }

        // 플레이어 Collider 찾기
        if (player != null)
        {
            playerCollider = player.GetComponent<Collider2D>();
            if (playerCollider == null)
                Debug.LogError("❌ 플레이어에 Collider2D가 필요합니다!");
        }

        // BridgePass 레이어의 콜라이더들 수집
        int bridgePassLayerIndex = LayerMask.NameToLayer(bridgePassLayer);
        if (bridgePassLayerIndex < 0)
        {
            Debug.LogError($"❌ '{bridgePassLayer}' 레이어가 존재하지 않습니다. Project Settings > Tags and Layers 에서 추가하세요.");
        }
        else
        {
            var allCols = Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None);
            foreach (var col in allCols)
            {
                if (col.gameObject.layer == bridgePassLayerIndex && !col.isTrigger)
                    bridgePassColliders.Add(col);
            }
        }
    }

    void Update()
    {
        if (playerCollider == null || bridgeCollider == null) return;

        bool nowOnBridge = IsPlayerFullyInsideBridge();

        // 상태 변경 시만 처리
        if (nowOnBridge != isPlayerOnBridge)
        {
            isPlayerOnBridge = nowOnBridge;
            gameObject.layer = LayerMask.NameToLayer(isPlayerOnBridge ? passableLayer : blockLayer);
            UpdateBridgePassColliders(isPlayerOnBridge);
        }

        // 이동량 계산
        bridgeDelta = transform.position - lastPosition;
        lastPosition = transform.position;
    }

    /// <summary>
    /// 플레이어 콜라이더가 다리 콜라이더 내부에 완전히 포함되어 있는지 확인
    /// </summary>
    private bool IsPlayerFullyInsideBridge()
    {
        Bounds playerBounds = playerCollider.bounds;
        Bounds bridgeBounds = bridgeCollider.bounds;

        // 완전 포함 여부 (min, max 모두 다리 bounds 안에 있어야 함)
        return bridgeBounds.Contains(playerBounds.min) && bridgeBounds.Contains(playerBounds.max);
    }

    /// <summary>
    /// 다리 위/밖 상태에 따라 BridgePass 콜라이더의 isTrigger 상태 전환
    /// </summary>
    private void UpdateBridgePassColliders(bool canPass)
    {
        foreach (var col in bridgePassColliders)
        {
            if (col != null)
                col.isTrigger = canPass;
        }
    }

    public bool PlayerOnBridge() => isPlayerOnBridge;
}
