using System.Collections.Generic;
using UnityEngine;

public class RoomColliderTrigger : MonoBehaviour
{
    public enum MoveAxis
    {
        Up, Down, Left, Right, CustomVector
    }

    [Header("트리거 설정")]
    public string triggerTag = "Player";

    [Header("이동할 오브젝트들")]
    public List<GameObject> objectsToMove = new List<GameObject>();
    public MoveAxis moveAxis = MoveAxis.Up;
    public Vector2 customDirection = Vector2.up;
    public float moveDistance = 7f;
    public float moveDuration = 7f;
    public float moveDelay = 8.5f;

    [Header("레이저 관련")]
    public List<LaserObject> lasersToActivate = new List<LaserObject>();
    public bool setLaserAngleFromMove = true;

    [Header("레이저 종료 옵션")]
    [Tooltip("이 값이 true면 이동이 끝난 시점에 모든 레이저를 자동으로 끕니다.")]
    public bool turnOffLaserOnArrival = false;

    private bool triggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (!other.CompareTag(triggerTag)) return;

        triggered = true;
        StartCoroutine(DelayedMoveAndLaser());
    }

    private System.Collections.IEnumerator DelayedMoveAndLaser()
    {
        yield return new WaitForSeconds(moveDelay);

        Vector2 dir = GetMoveDirection().normalized;
        if (dir.sqrMagnitude < 0.0001f) yield break;

        // 🔹 레이저 활성화
        if (lasersToActivate != null && lasersToActivate.Count > 0)
        {
            float deg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            foreach (var laser in lasersToActivate)
            {
                if (laser == null) continue;
                if (!laser.gameObject.activeSelf)
                    laser.gameObject.SetActive(true);

                if (setLaserAngleFromMove)
                    laser.transform.rotation = Quaternion.Euler(0f, 0f, deg);

                laser.Activate();
            }
        }

        // 🔹 이동 처리
        int count = objectsToMove.Count;
        Vector3[] startPos = new Vector3[count];
        Vector3[] endPos = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            var go = objectsToMove[i];
            if (go == null) continue;
            startPos[i] = go.transform.position;
            endPos[i] = startPos[i] + (Vector3)(dir * moveDistance);
        }

        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            float t = elapsed / moveDuration;
            for (int i = 0; i < count; i++)
            {
                var go = objectsToMove[i];
                if (go == null) continue;
                go.transform.position = Vector3.Lerp(startPos[i], endPos[i], t);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 🔹 최종 위치 고정
        for (int i = 0; i < count; i++)
        {
            var go = objectsToMove[i];
            if (go == null) continue;
            go.transform.position = endPos[i];
        }

        // 🔹 이동 완료 시 레이저 끄기 (옵션)
        if (turnOffLaserOnArrival && lasersToActivate != null)
        {
            foreach (var laser in lasersToActivate)
            {
                if (laser == null) continue;
                // LaserObject 내부에 Deactivate() 메서드가 있으면 사용
                // 없으면 그냥 비활성화 처리
                if (laser.gameObject.activeSelf)
                    laser.gameObject.SetActive(false);
            }
        }
    }

    private Vector2 GetMoveDirection()
    {
        switch (moveAxis)
        {
            case MoveAxis.Up: return Vector2.up;
            case MoveAxis.Down: return Vector2.down;
            case MoveAxis.Left: return Vector2.left;
            case MoveAxis.Right: return Vector2.right;
            case MoveAxis.CustomVector:
            default: return customDirection == Vector2.zero ? Vector2.up : customDirection.normalized;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (objectsToMove == null || objectsToMove.Count == 0) return;
        Gizmos.color = Color.cyan;
        Vector2 dir = GetMoveDirection().normalized;
        foreach (var go in objectsToMove)
        {
            if (go == null) continue;
            Vector3 start = go.transform.position;
            Vector3 end = start + (Vector3)(dir * moveDistance);
            Gizmos.DrawLine(start, end);
            Gizmos.DrawSphere(end, 0.08f);
        }
    }
#endif
}
