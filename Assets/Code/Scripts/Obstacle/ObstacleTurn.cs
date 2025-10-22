using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class ObstacleTurn : MonoBehaviour
{
    public enum TurnDirection
    {
        Clockwise,        // 시계 방향
        CounterClockwise  // 반시계 방향
    }

    [Header("회전 설정")]
    public TurnDirection turnDirection = TurnDirection.Clockwise;
    [Tooltip("1초에 회전할 각도 (°/sec)")]
    public float turnSpeed = 90f;
    [Tooltip("처음부터 회전 시작할지 여부")]
    public bool isTurning = true;

    [Header("물리 회전 여부")]
    [Tooltip("Rigidbody2D를 이용해 회전할 경우 true")]
    public bool useRigidbody2D = false;

    private Rigidbody2D rb2D;
    private Coroutine timedTurnRoutine;

    void Awake()
    {
        if (useRigidbody2D)
        {
            rb2D = GetComponent<Rigidbody2D>();
            if (rb2D == null)
            {
                Debug.LogWarning($"[{name}] Rigidbody2D가 없어서 Transform 회전으로 대체됩니다.");
                useRigidbody2D = false;
            }
            else
            {
                rb2D.interpolation = RigidbodyInterpolation2D.Interpolate;
            }
        }
    }

    void Update()
    {
        if (!isTurning) return;

        float dir = (turnDirection == TurnDirection.Clockwise) ? -1f : 1f;
        float rotationAmount = turnSpeed * dir * Time.deltaTime;

        if (useRigidbody2D && rb2D != null)
        {
            rb2D.MoveRotation(rb2D.rotation + rotationAmount);
        }
        else
        {
            transform.Rotate(0f, 0f, rotationAmount);
        }
    }

    // ───────────── 제어 메서드 ─────────────

    /// <summary>회전 시작</summary>
    public void StartTurning()
    {
        isTurning = true;
    }

    /// <summary>회전 정지</summary>
    public void StopTurning()
    {
        isTurning = false;
        if (timedTurnRoutine != null)
        {
            StopCoroutine(timedTurnRoutine);
            timedTurnRoutine = null;
        }
    }

    /// <summary>지정 시간 동안만 회전</summary>
    public void StartTurningForSeconds(float seconds)
    {
        if (timedTurnRoutine != null)
            StopCoroutine(timedTurnRoutine);
        timedTurnRoutine = StartCoroutine(TurnForSeconds(seconds));
    }

    private IEnumerator TurnForSeconds(float seconds)
    {
        isTurning = true;
        yield return new WaitForSeconds(seconds);
        isTurning = false;
        timedTurnRoutine = null;
    }

    /// <summary>회전 방향 변경</summary>
    public void ToggleDirection()
    {
        turnDirection = (turnDirection == TurnDirection.Clockwise)
            ? TurnDirection.CounterClockwise
            : TurnDirection.Clockwise;
    }

    /// <summary>속도 변경</summary>
    public void SetSpeed(float speed)
    {
        turnSpeed = Mathf.Max(0f, speed);
    }

    /// <summary>방향 지정</summary>
    public void SetDirection(TurnDirection dir)
    {
        turnDirection = dir;
    }
}
