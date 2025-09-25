using UnityEngine;
using System.Collections;
using DG.Tweening;

public class ExplosionEnemySpawner : MonoBehaviour
{
    [Header("소환할 적 프리팹")]
    public GameObject enemyPrefab;

    [Header("소환 위치 (비워두면 자기 위치 사용)")]
    public Transform spawnPoint;

    [Header("경고 이펙트")]
    public GameObject warningEffectPrefab;
    public float warningDuration = 1f;

    [Header("한 번만 소환할지 여부")]
    public bool spawnOnce = true;

    private bool hasSpawned = false;
    private Collider2D col;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (spawnOnce && hasSpawned) return;

        StartCoroutine(SpawnEnemy());
        hasSpawned = true;

        // 👉 닿으면 바로 콜라이더 꺼지게
        if (col != null)
            col.enabled = false;
    }

    private IEnumerator SpawnEnemy()
    {
        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;

        // 경고 이펙트 표시 (WaveManager 방식으로 깜빡임)
        if (warningEffectPrefab != null)
        {
            GameObject warning = Instantiate(warningEffectPrefab, pos, Quaternion.identity);

            SpriteRenderer sr = warning.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = new Color(1, 0, 0, 0);
                sr.DOFade(1f, 0.3f)
                  .SetLoops(-1, LoopType.Yoyo)
                  .SetEase(Ease.InOutQuad);
            }

            Destroy(warning, warningDuration);
        }

        // ⚡ 경고 시간 + 추가 대기 시간 기다리기
        yield return new WaitForSeconds(warningDuration);

        // 적 소환
        if (enemyPrefab != null)
        {
            GameObject enemy = Instantiate(enemyPrefab, pos, Quaternion.identity);

            // EnemyBase 계열이면 잠깐 이동 막기
            EnemyBase enemyBase = enemy.GetComponent<EnemyBase>();
            if (enemyBase != null)
            {
                enemyBase.CanMove = false;
                yield return new WaitForSeconds(0.4f);
                enemyBase.CanMove = true;
            }
        }
    }
}
