using Unity.VisualScripting;
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
    
    [Header("Mesh Object")]
    public GameObject meshObject; // Mesh 오브젝트 참조

    public float smoothTime = 0.1f;
    public bool canMove = true;

    [Header("조이스틱")]
    public Joystick joystick;

    [Header("조이스틱 방향 표시")]
    public GameObject directionIndicatorPrefab;
    private GameObject directionIndicatorInstance;

    [Header("Bridge")]
    public Bridge bridge; // Scene에서 연결

    public float directionIndicatorDistance = 0.3f; // 원이 플레이어로부터 떨어진 거리

    private Vector2 keyboardInput;

    void Start()
    {
        // Mesh 오브젝트가 지정되지 않았다면 자동으로 찾기
        if (meshObject == null)
        {
            meshObject = transform.Find("Mesh")?.gameObject;
        }
        
        // Mesh 오브젝트에서 컴포넌트 가져오기
        if (meshObject != null)
        {
            spriteRenderer = meshObject.GetComponent<SpriteRenderer>();
            playerAnimation = meshObject.GetComponent<PlayerAnimation>();
        }
        else
        {
            Debug.LogError("Mesh 오브젝트를 찾을 수 없습니다. Player의 하위에 'Mesh' 오브젝트가 있는지 확인하거나 meshObject 필드에 직접 할당해주세요.");
        }

        if (directionIndicatorPrefab != null)
        {
            directionIndicatorInstance = Instantiate(directionIndicatorPrefab, transform.position, Quaternion.identity);
            directionIndicatorInstance.transform.SetParent(transform); // 플레이어에 자식으로 붙임
        }
    }

    void Update()
    {
        if (!canMove) return;

        // 1) 키보드 입력
        keyboardInput = new Vector2(
            Keyboard.current.aKey.isPressed ? -1 : Keyboard.current.dKey.isPressed ? 1 : 0,
            Keyboard.current.sKey.isPressed ? -1 : Keyboard.current.wKey.isPressed ? 1 : 0
        );

        // 2) 조이스틱 입력
        Vector2 joystickInput = new Vector2(joystick.Horizontal, joystick.Vertical);

        // 조이스틱 입력 세기 반영 (Floating Joystick 지원)
        if (joystickInput.magnitude > 0.05f)
        {
            joystickInput = Vector2.ClampMagnitude(joystickInput, 1f);
        }
        else
        {
            joystickInput = Vector2.zero;
        }

        // 3) 두 입력 합치기
        inputVec = keyboardInput + joystickInput;

        // 4) 대각선 과속 방지
        if (inputVec.magnitude > 1f)
            inputVec = inputVec.normalized;

        currentDirection = Vector2.SmoothDamp(currentDirection, inputVec, ref currentVelocity, smoothTime);

        UpdateDirectionIndicator(); // 방향 원 업데이트

        // 방향 전환 (null 체크 추가)
        if (spriteRenderer != null)
        {
            float flipInput = Mathf.Abs(inputVec.x) > 0.05f ? inputVec.x : currentDirection.x;
            if (flipInput < -0.01f) spriteRenderer.flipX = true;
            else if (flipInput > 0.01f) spriteRenderer.flipX = false;
        }

        // 애니메이션 (null 체크 추가)
        if (playerAnimation != null)
        {
            if (currentDirection == Vector2.zero)
                playerAnimation.PlayAnimation(PlayerAnimation.State.Idle);
            else
                playerAnimation.PlayAnimation(PlayerAnimation.State.Move);
        }
    }

    void LateUpdate()
    {
        if (!canMove) return;

        // 기본 이동량
        Vector2 moveDelta = currentDirection * GameManager.Instance.playerStats.speed * Time.deltaTime;

        // Bridge가 있고, 플레이어가 위에 있다면 Bridge 이동량 합산
        if (bridge != null && bridge.PlayerOnBridge())
        {
            moveDelta += (Vector2)bridge.bridgeDelta;
        }

        transform.Translate(moveDelta);
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
        // Mesh 오브젝트의 SpriteRenderer 사용
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            return Mathf.Max(spriteRenderer.bounds.size.x, spriteRenderer.bounds.size.y);
        }
        return 1f;
    }

    void OnMove(InputValue value)
    {
        // Unity InputSystem용 (필요시 추가 구현)
    }
}
