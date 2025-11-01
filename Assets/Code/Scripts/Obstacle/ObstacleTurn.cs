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
                // Rigidbody2D X/Y 이동만 허용, Z 회전은 스크립트로 제어
                rb2D.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezePositionY;
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
            // Rigidbody2D Z축만 회전
            float newRotation = rb2D.rotation + rotationAmount;
            rb2D.MoveRotation(newRotation);
        }
        else
        {
            // Transform 회전 시 X/Y 회전 강제 고정, Z축만 회전
            Vector3 rotation = transform.eulerAngles;
            rotation.z += rotationAmount;
            rotation.x = 0f;
            rotation.y = 0f;
            transform.eulerAngles = rotation;
        }
    }

    // ───────────── 제어 메서드 ─────────────

    public void StartTurning() => isTurning = true;

    public void StopTurning()
    {
        isTurning = false;
        if (timedTurnRoutine != null)
        {
            StopCoroutine(timedTurnRoutine);
            timedTurnRoutine = null;
        }
    }

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

    public void ToggleDirection()
    {
        turnDirection = (turnDirection == TurnDirection.Clockwise)
            ? TurnDirection.CounterClockwise
            : TurnDirection.Clockwise;
    }

    public void SetSpeed(float speed) => turnSpeed = Mathf.Max(0f, speed);

    public void SetDirection(TurnDirection dir) => turnDirection = dir;
}
