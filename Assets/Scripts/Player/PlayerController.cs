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

    [Header("조이스틱 방향 표시")]
    public GameObject directionIndicatorPrefab;
    private GameObject directionIndicatorInstance;

    public float directionIndicatorDistance = 0.3f; // 원이 플레이어로부터 떨어진 거리

    private Vector2 keyboardInput;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerAnimation = GetComponent<PlayerAnimation>();

        if (directionIndicatorPrefab != null)
        {
            directionIndicatorInstance = Instantiate(directionIndicatorPrefab, transform.position, Quaternion.identity);
            directionIndicatorInstance.transform.SetParent(transform); // 플레이어에 자식으로 붙임
        }
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
        //Vector2 joystickInput = new Vector2(joystick.Horizontal, joystick.Vertical);

        // 2) 조이스틱 입력
        Vector2 joystickInput = new Vector2(joystick.Horizontal, joystick.Vertical);

        // 조이스틱 입력 세기 무시하고 항상 방향만 사용
        if (joystickInput.magnitude > 0.05f)
            joystickInput = joystickInput.normalized;
        else
            joystickInput = Vector2.zero;


        // 3) 두 입력 합치기
        inputVec = keyboardInput + joystickInput;

        // 4) 대각선 과속 방지
        if (inputVec.magnitude > 1f)
            inputVec = inputVec.normalized;

        currentDirection = Vector2.SmoothDamp(currentDirection, inputVec, ref currentVelocity, smoothTime);
        Vector2 moveDelta = currentDirection * GameManager.Instance.playerStats.speed * Time.deltaTime;

        // 장애물 체크 없이 바로 이동
        transform.Translate(moveDelta);

        UpdateDirectionIndicator(); // 방향 원 업데이트

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

    void UpdateDirectionIndicator()
    {
        if (directionIndicatorInstance == null) return;

        float playerSize = GetPlayerSize(); // 플레이어 크기 반영
        float inputStrength = Mathf.Clamp01(inputVec.magnitude); // 입력 세기 (0~1)

        // 거리 = 기본 거리 × 입력 세기 × 플레이어 크기
        float dynamicDistance = directionIndicatorDistance * inputStrength * playerSize;

        if (inputStrength > 0.05f) // 아주 미세한 입력은 표시 안 함
        {
            directionIndicatorInstance.SetActive(true);
            Vector2 offset = inputVec.normalized * dynamicDistance;
            directionIndicatorInstance.transform.localPosition = offset;
        }
        else
        {
            directionIndicatorInstance.SetActive(false);
        }
    }

    float GetPlayerSize()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            return Mathf.Max(sr.bounds.size.x, sr.bounds.size.y);
        }
        return 1f;
    }


    void OnMove(InputValue value)
    {
        // Unity InputSystem용 (필요시 추가 구현)
    }
}
