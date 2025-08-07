using System.Collections;
using UnityEngine;
using DG.Tweening;

public class SlowSkill : MonoBehaviour
{
    public float slowDuration = 3f;
    public float slowRatio = 0.5f;

    public void ApplySlow(EnemyBase enemy)
    {
        if (enemy != null)
        {
            StartCoroutine(SlowCoroutine(enemy));
        }
    }

    private IEnumerator SlowCoroutine(EnemyBase enemy)
    {
        float originalSpeed = enemy.originalSpeed;

        // 속도 절반으로 감소
        enemy.SetSpeed(originalSpeed * slowRatio);

        // SpriteRenderer 가져오기
        SpriteRenderer sr = enemy.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            // 원래 색 저장
            Color originalColor = sr.color;

            // 파란색으로 변경 (알파 유지), 완료까지 기다림
            yield return sr.DOColor(new Color(0f, 0.5f, 1f, originalColor.a), 0.2f).WaitForCompletion();

            // slowDuration 동안 파란색 유지
            yield return new WaitForSeconds(slowDuration);

            // 원래 색으로 복원, 완료까지 기다림
            yield return sr.DOColor(originalColor, 0.2f).WaitForCompletion();
        }
        else
        {
            // SpriteRenderer가 없으면 그냥 슬로우 지속시간만 대기
            yield return new WaitForSeconds(slowDuration);
        }

        // 속도 원래대로 복원
        enemy.SetSpeed(originalSpeed);
    }

}
