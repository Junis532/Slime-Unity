using UnityEngine;

public class MovingWall : MonoBehaviour
{
    [Header("이동 설정")]
    public float moveDistance = 3f;
    public float moveSpeed = 2f;
    public float returnSpeed = 1f;
    public float waitTime = 1f;

    private Vector3 startPos;
    private bool movingRight = true;
    private bool returning = false;
    private float waitTimer = 0f;

    [HideInInspector]
    public bool isActive = false; // 룸 진입 시 활성화

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        if (!isActive) return; // 활성화 전에는 이동하지 않음

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
                else
                {
                    return;
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
    }
}
