using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class SlowSkill : MonoBehaviour
{
    [Header("슬로우/스턴 설정")]
    public float slowDuration = 3f;      // 슬로우 지속 시간
    public float slowRatio = 0.5f;       // 슬로우 비율
    public int hitsToStun = 5;           // 스턴에 필요한 피격 횟수
    public float stunDuration = 2f;      // 스턴 지속 시간

    private Dictionary<EnemyBase, int> hitCounts = new Dictionary<EnemyBase, int>();
    private HashSet<EnemyBase> stunnedEnemies = new HashSet<EnemyBase>();
    private Dictionary<EnemyBase, Coroutine> slowCoroutines = new Dictionary<EnemyBase, Coroutine>();

    public void ApplySlow(EnemyBase enemy)
    {
        if (enemy == null) return;
        if (stunnedEnemies.Contains(enemy)) return;

        if (!hitCounts.ContainsKey(enemy)) hitCounts[enemy] = 0;

        hitCounts[enemy]++;

        // 스턴 적용 조건
        if (hitCounts[enemy] >= hitsToStun)
        {
            hitCounts[enemy] = 0;
            if (slowCoroutines.ContainsKey(enemy))
            {
                StopCoroutine(slowCoroutines[enemy]);
                slowCoroutines.Remove(enemy);
            }
            StartCoroutine(StunCoroutine(enemy));
        }
        else
        {
            // 기존 슬로우 Coroutine이 있으면 종료 후 재시작 → 지속시간 초기화
            if (slowCoroutines.ContainsKey(enemy))
            {
                StopCoroutine(slowCoroutines[enemy]);
            }
            slowCoroutines[enemy] = StartCoroutine(SlowCoroutine(enemy));
        }
    }

    private IEnumerator SlowCoroutine(EnemyBase enemy)
    {
        float originalSpeed = enemy.originalSpeed;
        SpriteRenderer sr = enemy.GetComponent<SpriteRenderer>();

        // 속도 감소
        enemy.SetSpeed(originalSpeed * slowRatio);

        if (sr != null)
        {
            sr.DOKill();
            sr.DOColor(new Color(0f, 0.5f, 1f, 1f), 0.2f);
        }

        // 슬로우 지속
        yield return new WaitForSeconds(slowDuration);

        // 속도 복구
        enemy.SetSpeed(originalSpeed);

        if (sr != null)
        {
            sr.DOKill();
            sr.DOColor(Color.white, 0.2f);
            sr.color = Color.white;
        }

        slowCoroutines.Remove(enemy);
    }

    private IEnumerator StunCoroutine(EnemyBase enemy)
    {
        float originalSpeed = enemy.originalSpeed;
        SpriteRenderer sr = enemy.GetComponent<SpriteRenderer>();

        stunnedEnemies.Add(enemy);

        // 스턴 시 속도 0
        enemy.SetSpeed(0);

        if (sr != null)
        {
            sr.DOKill();
            sr.DOColor(new Color(0f, 0f, 1f, 1f), 0.2f);
        }

        yield return new WaitForSeconds(stunDuration);

        enemy.SetSpeed(originalSpeed);

        if (sr != null)
        {
            sr.DOKill();
            sr.DOColor(Color.white, 0.2f);
            sr.color = Color.white;
        }

        stunnedEnemies.Remove(enemy);
    }
}
