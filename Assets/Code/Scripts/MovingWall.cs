using UnityEngine;

public class MovingWall : MonoBehaviour
{
    [Header("이동 포인트")]
    public Vector2 startPoint;
    public Vector2 endPoint1;
    public Vector2 endPoint2;

    [Header("이동 속도")]
    public float moveSpeed = 2f;
    public float returnSpeed = 1f;
    public float waitTime = 1f;

    [Header("시작 지연")]
    public float startDelay = 1.5f;

    private Vector2 targetPoint;
    private int targetIndex = 0; // 0 = End1, 1 = End2
    private float waitTimer = 0f;
    private float delayTimer = 0f;

    [HideInInspector]
    public bool isActive = false;

    void Start()
    {
        transform.position = startPoint;
        targetPoint = endPoint1; // 처음 이동할 목표
        targetIndex = 0;
    }

    void Update()
    {
        if (!isActive) return;

        // 시작 지연
        if (delayTimer < startDelay)
        {
            delayTimer += Time.deltaTime;
            return;
        }

        // 이동
        float speed = (targetIndex == 0) ? moveSpeed : returnSpeed;
        transform.position = Vector2.MoveTowards(transform.position, targetPoint, speed * Time.deltaTime);

        // 목표점 도달
        if (Vector2.Distance(transform.position, targetPoint) < 0.01f)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitTime)
            {
                waitTimer = 0f;

                // 다음 목표점 설정 (End1 ↔ End2 반복)
                if (targetIndex == 0)
                {
                    targetPoint = endPoint2;
                    targetIndex = 1;
                }
                else
                {
                    targetPoint = endPoint1;
                    targetIndex = 0;
                }
            }
        }
    }

    public void ResetWall()
    {
        isActive = false;
        transform.position = startPoint;
        targetPoint = endPoint1;
        targetIndex = 0;
        waitTimer = 0f;
        delayTimer = 0f;
    }
}
