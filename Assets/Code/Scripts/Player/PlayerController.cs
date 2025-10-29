using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public Vector2 inputVec;
    public Vector2 InputVector => inputVec;

    [Header("이동/입력")]
    public float smoothTime = 0.1f;
    public bool canMove = true;

    [Header("정지 감지")]
    [Tooltip("입력이 이 값 미만이면 정지로 판단(Stop 트리거 에지)")]
    public float stopTriggerThreshold = 0.05f;

    [Header("조이스틱")]
    public Joystick joystick;

    [Header("조이스틱 방향 표시")]
    public GameObject directionIndicatorPrefab;
    public float directionIndicatorDistance = 0.3f;

    [Header("Bridge")]
    public Bridge bridge; // Scene에서 연결

    [Header("Mesh Reflection")]
    public GameObject meshReflection; // 🔹 물 반사 오브젝트 연결

    private Vector2 keyboardInput;
    private Vector2 currentVelocity;
    private Vector2 currentDirection;

    private PlayerAnimation playerAnimation;
    private SpriteRenderer spriteRenderer;
    private GameObject directionIndicatorInstance;

    private bool wasMovingInput = false;

    void Start()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        playerAnimation = GetComponentInChildren<PlayerAnimation>();

        if (directionIndicatorPrefab != null)
        {
            directionIndicatorInstance = Instantiate(directionIndicatorPrefab, transform.position, Quaternion.identity);
            directionIndicatorInstance.transform.SetParent(transform);
        }
    }

    void Update()
    {
        if (!canMove) return;

        var kb = Keyboard.current;
        float ax = 0f, ay = 0f;
        if (kb != null)
        {
            ax = kb.aKey.isPressed ? -1 : (kb.dKey.isPressed ? 1 : 0);
            ay = kb.sKey.isPressed ? -1 : (kb.wKey.isPressed ? 1 : 0);
        }
        keyboardInput = new Vector2(ax, ay);

        Vector2 joystickInput = Vector2.zero;
        if (joystick != null)
        {
            joystickInput = new Vector2(joystick.Horizontal, joystick.Vertical);
            joystickInput = (joystickInput.magnitude > 0.05f) ? Vector2.ClampMagnitude(joystickInput, 1f) : Vector2.zero;
        }

        inputVec = keyboardInput + joystickInput;
        if (inputVec.magnitude > 1f) inputVec = inputVec.normalized;

        bool isMovingInput = inputVec.magnitude > stopTriggerThreshold;
        if (wasMovingInput && !isMovingInput && playerAnimation != null)
            playerAnimation.OnStopMoving();
        wasMovingInput = isMovingInput;

        currentDirection = Vector2.SmoothDamp(currentDirection, inputVec, ref currentVelocity, smoothTime);

        float flipInput = Mathf.Abs(inputVec.x) > 0.05f ? inputVec.x : currentDirection.x;
        if (spriteRenderer != null)
        {
            if (flipInput < -0.01f)
            {
                spriteRenderer.flipX = true;
                FlipMeshReflection(true);  // 🔹 같이 플립
            }
            else if (flipInput > 0.01f)
            {
                spriteRenderer.flipX = false;
                FlipMeshReflection(false); // 🔹 같이 플립
            }
        }

        UpdateDirectionIndicator();

        if (playerAnimation != null)
        {
            playerAnimation.PlayAnimation(isMovingInput ? PlayerAnimation.State.Move
                                                        : PlayerAnimation.State.Idle);
        }
    }

    void LateUpdate()
    {
        if (!canMove) return;

        Vector2 moveDelta = currentDirection * GameManager.Instance.playerStats.speed * Time.deltaTime;

        if (bridge != null && bridge.PlayerOnBridge())
            moveDelta += (Vector2)bridge.bridgeDelta;

        transform.Translate(moveDelta);
    }

    void UpdateDirectionIndicator()
    {
        if (directionIndicatorInstance == null) return;

        float playerSize = GetPlayerSize();
        float inputStrength = Mathf.Clamp01(inputVec.magnitude);
        float dynamicDistance = directionIndicatorDistance * inputStrength * playerSize;

        if (inputStrength > 0.05f)
        {
            directionIndicatorInstance.SetActive(true);
            directionIndicatorInstance.transform.localPosition = inputVec.normalized * dynamicDistance;
        }
        else
        {
            directionIndicatorInstance.SetActive(false);
        }
    }

    float GetPlayerSize()
    {
        if (spriteRenderer != null && spriteRenderer.sprite != null)
            return Mathf.Max(spriteRenderer.bounds.size.x, spriteRenderer.bounds.size.y);
        return 1f;
    }

    void OnMove(InputValue value) { }

    // 🔹 Mesh Reflection 좌우 반전 처리 함수
    void FlipMeshReflection(bool flip)
    {
        if (meshReflection == null) return;

        Vector3 scale = meshReflection.transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (flip ? -1 : 1);
        meshReflection.transform.localScale = scale;
    }
}
