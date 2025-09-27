using UnityEngine;

public class MovingWall : MonoBehaviour
{
    [Header("이동 설정")]
    public float moveDistance = 3f;
    public float moveSpeed = 2f;
    public float returnSpeed = 1f;
    public float waitTime = 1f;

    [Header("시작 지연")]
    public float startDelay = 1.5f; // 방 입장 후 대기 시간

    private Vector3 startPos;
    private bool movingRight = true;
    private bool returning = false;
    private float waitTimer = 0f;
    private float delayTimer = 0f;

    [HideInInspector]
    public bool isActive = false; // 룸 진입 시 활성화

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        if (!isActive) return;

        // 시작 지연 처리
        if (delayTimer < startDelay)
        {
            delayTimer += Time.deltaTime;
            return;
        }

        // 벽 이동 로직
        if (returning)
        {
            transform.position = Vector3.MoveTowards(transform.position, startPos, returnSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, startPos) < 0.01f)
            {
                returning = false;
                movingRight = true;
            }
        }
        else
        {
            float targetX = startPos.x + (movingRight ? moveDistance : -moveDistance);
            Vector3 targetPos = new Vector3(targetX, startPos.y, startPos.z);

            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPos) < 0.01f)
            {
                waitTimer += Time.deltaTime;
                if (waitTimer >= waitTime)
                {
                    waitTimer = 0f;
                    if (movingRight) returning = true;
                    else movingRight = true;
                }
            }
        }
    }

    public void ResetWall()
    {
        isActive = false;
        transform.position = startPos;
        movingRight = true;
        returning = false;
        waitTimer = 0f;
        delayTimer = 0f; // 지연 타이머 초기화
    }
}
