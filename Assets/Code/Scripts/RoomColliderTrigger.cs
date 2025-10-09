using UnityEngine;

public class RoomColliderTrigger : MonoBehaviour
{
    [Header("이동할 오브젝트")]
    public GameObject targetObject; // 위로 움직일 오브젝트

    [Header("이동 설정")]
    public float moveDistance = 7f;   // 위로 이동할 거리
    public float moveDuration = 7f;   // 이동 시간(초)
    public float moveDelay = 8.5f;    // 트리거 후 이동 시작 대기 시간

    [Header("레이저")]
    public LaserObject laser;         // 레이저 오브젝트
    public string triggerTag = "Player";

    private bool triggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (!other.CompareTag(triggerTag)) return;

        triggered = true;

        // 이동 및 레이저 동기 실행
        if (targetObject != null) StartCoroutine(DelayedMoveAndLaser());
    }

    private System.Collections.IEnumerator DelayedMoveAndLaser()
    {
        // 이동 지연
        yield return new WaitForSeconds(moveDelay);

        // 이동 시작과 동시에 레이저 켜기
        if (laser != null)
        {
            // LaserObject 오브젝트가 꺼져있다면 먼저 활성화
            if (!laser.gameObject.activeSelf)
                laser.gameObject.SetActive(true);

            laser.Activate(); // 바로 켜기
        }

        Vector3 startPos = targetObject.transform.position;
        Vector3 endPos = startPos + Vector3.up * moveDistance;
        float elapsed = 0f;

        // 지정된 시간 동안 이동
        while (elapsed < moveDuration)
        {
            targetObject.transform.position = Vector3.Lerp(startPos, endPos, elapsed / moveDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        targetObject.transform.position = endPos; // 최종 위치 고정
    }
}
