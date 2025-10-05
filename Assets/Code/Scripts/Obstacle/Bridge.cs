using UnityEngine;

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

    [Header("겹침 허용 오차")]
    public float overlapMargin = 0.05f;

    void Start()
    {
        lastPosition = transform.position;
    }

    void Update()
    {
        // 플레이어가 겹쳐 있는지 체크
        if (player != null)
        {
            float diff = Mathf.Abs(player.position.y - transform.position.y);
            isPlayerOnBridge = diff <= overlapMargin;

            // 레이어 변경
            if (isPlayerOnBridge)
                gameObject.layer = LayerMask.NameToLayer(passableLayer);
            else
                gameObject.layer = LayerMask.NameToLayer(blockLayer);
        }

        // Bridge 이동량 계산
        bridgeDelta = transform.position - lastPosition;
        lastPosition = transform.position;
    }

    public bool PlayerOnBridge() => isPlayerOnBridge;
}
