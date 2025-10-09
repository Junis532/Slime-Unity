using UnityEngine;
using System.Collections;

public class CameraCutsceneSimple : MonoBehaviour
{
    [Header("카메라 이동")]
    public float moveDistance = 10f;   // 아래로 이동할 거리
    public float moveDuration = 5f;    // 이동 시간(초)

    [Header("슬라임 연동 (옵션)")]
    public SlimeWalkSimple slime;      // 슬라임 드래그(없으면 비워도 됨)
    public float slimeLeadTime = 0.5f; // 컷씬 끝나기 0.5초 전 시작
    public float slimeWalkDuration = 3f;

    Vector3 startPos, targetPos;

    void Start()
    {
        startPos = transform.position;
        targetPos = startPos + Vector3.down * moveDistance;
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        float elapsed = 0f;
        bool slimeStarted = false;
        float slimeStartAt = Mathf.Max(0f, moveDuration - slimeLeadTime);

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);
            // 부드러운 보간
            transform.position = Vector3.Lerp(startPos, targetPos, Mathf.SmoothStep(0f, 1f, t));

            if (!slimeStarted && slime != null && elapsed >= slimeStartAt)
            {
                slimeStarted = true;
                slime.StartWalking(slimeWalkDuration);
            }
            yield return null;
        }
        transform.position = targetPos; // 최종 스냅
    }
}
