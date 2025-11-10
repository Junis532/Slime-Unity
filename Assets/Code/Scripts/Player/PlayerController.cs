using System.Collections.Generic;
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

    [Header("Mesh Reflection")]
    public List<GameObject> meshReflections = new List<GameObject>();

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
        if (!canMove) return;  // ← 잠금 시 전부 무시

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

        if (inputVec.magnitude > 0.05f)
            currentDirection = inputVec.normalized;
        else
            currentDirection = Vector2.zero;

        float flipInput = Mathf.Abs(inputVec.x) > 0.05f ? inputVec.x : currentDirection.x;
        if (spriteRenderer != null)
        {
            if (flipInput < -0.01f)
            {
                spriteRenderer.flipX = true;
                FlipMeshReflection(true);
            }
            else if (flipInput > 0.01f)
            {
                spriteRenderer.flipX = false;
                FlipMeshReflection(false);
            }
        }

        UpdateDirectionIndicator();

        if (playerAnimation != null)
        {
            // Start 애니메이션 재생 중이면 Idle/Move 재생 금지
            if (!playerAnimation.IsPlayingStart())
            {
                playerAnimation.PlayAnimation(isMovingInput ? PlayerAnimation.State.Move
                                                            : PlayerAnimation.State.Idle);
            }
        }

    }

    void LateUpdate()
    {
        if (!canMove) return;  // ← 잠금 시 이동 계산도 안 함

        Vector2 moveDelta = currentDirection * GameManager.Instance.playerStats.speed * Time.deltaTime;
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

    // 좌우 반전(리스트 전부)
    void FlipMeshReflection(bool flip)
    {
        if (meshReflections == null || meshReflections.Count == 0) return;

        foreach (GameObject reflection in meshReflections)
        {
            if (reflection == null) continue;

            Vector3 scale = reflection.transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (flip ? -1 : 1);
            reflection.transform.localScale = scale;
        }
    }

    // ===== 잠금/해제 공개 API =====
    public void LockMovement(bool resetImmediate = true)
    {
        canMove = false;

        if (resetImmediate)
        {
            // 입력/방향 즉시 초기화
            inputVec = Vector2.zero;
            keyboardInput = Vector2.zero;
            currentDirection = Vector2.zero;
            currentVelocity = Vector2.zero;

            // 방향표시 숨김
            if (directionIndicatorInstance != null)
                directionIndicatorInstance.SetActive(false);

            // 애니메이션 정지
            if (playerAnimation != null)
                playerAnimation.PlayAnimation(PlayerAnimation.State.Idle);
        }
    }

    public void UnLockMovement()
    {
        canMove = true;
    }
}
