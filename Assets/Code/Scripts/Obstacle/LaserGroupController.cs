using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserGroupController : MonoBehaviour
{
    [Header("Laser Group Settings")]
    public List<LaserObject> lasers = new List<LaserObject>(); // 🔹 관리할 레이저들
    public bool controlIndividually = false;                   // 🔹 false면 한 번에, true면 순차 제어

    [Header("Group Timing Settings")]
    public float delayBetweenLasers = 0.5f;                    // 🔹 순차 발사 간격
    public float startDelay = 0f;                              // 🔹 전체 시작 딜레이

    [Header("Group Warning Override (선택사항)")]
    public bool overrideWarning = false;
    public bool useWarning = true;
    public float warningDuration = 1f;

    [Header("Group Active Duration (자동 비활성용)")]
    public bool autoDeactivate = false;
    public float activeDuration = 3f;

    private Coroutine patternRoutine;

    // ===============================
    // 🔹 그룹 제어 메서드
    // ===============================

    // 그룹 일괄 발사
    public void ActivateGroup()
    {
        if (patternRoutine != null)
            StopCoroutine(patternRoutine);

        patternRoutine = StartCoroutine(ActivateGroupRoutine());
    }

    // 그룹 일괄 종료
    public void DeactivateGroup()
    {
        if (patternRoutine != null)
            StopCoroutine(patternRoutine);

        foreach (var laser in lasers)
        {
            if (laser != null)
                laser.Deactivate();
        }
    }

    // 순차적으로 발사 (딜레이 간격 있음)
    private IEnumerator ActivateGroupRoutine()
    {
        yield return new WaitForSeconds(startDelay);

        if (controlIndividually)
        {
            for (int i = 0; i < lasers.Count; i++)
            {
                var laser = lasers[i];
                if (laser == null) continue;

                ApplyOverrides(laser);
                laser.Activate();

                yield return new WaitForSeconds(delayBetweenLasers);
            }
        }
        else
        {
            foreach (var laser in lasers)
            {
                if (laser == null) continue;
                ApplyOverrides(laser);
                laser.Activate();
            }
        }

        // 자동 비활성 기능
        if (autoDeactivate)
        {
            yield return new WaitForSeconds(activeDuration);
            DeactivateGroup();
        }
    }

    // ===============================
    // 🔹 유틸리티 함수
    // ===============================
    private void ApplyOverrides(LaserObject laser)
    {
        if (!overrideWarning) return;

        laser.useWarning = useWarning;
        laser.warningDuration = warningDuration;
    }

    // 특정 레이저 하나만 활성화 (이름 또는 인덱스)
    public void ActivateLaser(int index)
    {
        if (index < 0 || index >= lasers.Count) return;
        lasers[index]?.Activate();
    }

    public void DeactivateLaser(int index)
    {
        if (index < 0 || index >= lasers.Count) return;
        lasers[index]?.Deactivate();
    }
}
