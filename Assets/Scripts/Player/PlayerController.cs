using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public Vector2 InputVector => inputVec;

    public Vector2 inputVec;
    private Vector2 currentVelocity;
    private Vector2 currentDirection;
    private PlayerAnimation playerAnimation;
    private SpriteRenderer spriteRenderer;

    public float smoothTime = 0.1f;
    public bool canMove = true;

    [Header("조이스틱")]
    public VariableJoystick joystick;

    [Header("장애물 레이어")]
    public LayerMask obstacleLayer;  // Obstacle 레이어만 포함

    private Vector2 keyboardInput;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerAnimation = GetComponent<PlayerAnimation>();
    }

    void Update()
    {
        if (GameManager.Instance.CurrentState == "Shop")
        {
            //canMove = false;
        }
        else if (GameManager.Instance.CurrentState == "Game")
        {
            canMove = true;
        }

        if (!canMove) return;

        // 1) 키보드 입력
        keyboardInput = new Vector2(
            Keyboard.current.aKey.isPressed ? -1 : Keyboard.current.dKey.isPressed ? 1 : 0,
            Keyboard.current.sKey.isPressed ? -1 : Keyboard.current.wKey.isPressed ? 1 : 0
        );

        // 2) 조이스틱 입력
        Vector2 joystickInput = new Vector2(joystick.Horizontal, joystick.Vertical);

        // 3) 두 입력 합치기
        inputVec = keyboardInput + joystickInput;

        // 4) 대각선 과속 방지
        if (inputVec.magnitude > 1f)
            inputVec = inputVec.normalized;

        currentDirection = Vector2.SmoothDamp(currentDirection, inputVec, ref currentVelocity, smoothTime);
        Vector2 moveDelta = currentDirection * GameManager.Instance.playerStats.speed * Time.deltaTime;

        Vector2 moveX = new Vector2(moveDelta.x, 0f);
        Vector2 moveY = new Vector2(0f, moveDelta.y);

        bool canMoveX = !IsObstacleAhead(moveX);
        bool canMoveY = !IsObstacleAhead(moveY);

        Vector2 finalMove = Vector2.zero;
        if (canMoveX) finalMove.x = moveDelta.x;
        if (canMoveY) finalMove.y = moveDelta.y;

        transform.Translate(finalMove);

        // 방향 전환
        float flipInput = Mathf.Abs(inputVec.x) > 0.05f ? inputVec.x : currentDirection.x;
        if (flipInput < -0.01f) spriteRenderer.flipX = true;
        else if (flipInput > 0.01f) spriteRenderer.flipX = false;

        // 애니메이션
        if (currentDirection == Vector2.zero)
            playerAnimation.PlayAnimation(PlayerAnimation.State.Idle);
        else
            playerAnimation.PlayAnimation(PlayerAnimation.State.Move);
    }

    bool IsObstacleAhead(Vector2 moveDelta)
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return false;

        Vector2 newPos = (Vector2)transform.position + moveDelta;
        Vector2 boxSize = col.bounds.size * 0.9f;

        Collider2D hit = Physics2D.OverlapBox(newPos, boxSize, 0f, obstacleLayer);
        return hit != null;
    }



    void OnMove(InputValue value)
    {
        // Unity InputSystem용 (필요시 추가 구현)
    }
}
