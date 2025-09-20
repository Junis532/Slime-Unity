using DG.Tweening;
using System.Collections;
using UnityEngine;

public class BabyEnemy : EnemyBase
{
    private bool isLive = true;

    private SpriteRenderer spriter;
    private EnemyAnimation enemyAnimation;

    [Header("스폰 관련")]
    public GameObject spawnPrefab;          // 소환할 몬스터 프리팹
    public float spawnInterval = 2f;        // 스폰 간격

    void Start()
    {
        spriter = GetComponent<SpriteRenderer>();
        enemyAnimation = GetComponent<EnemyAnimation>();

        originalSpeed = GameManager.Instance.enemyStats.speed;
        speed = originalSpeed;

        if (spawnPrefab != null)
        {
            StartCoroutine(SpawnLoop());
        }
    }

    void Update()
    {
        if (!isLive) return;

        // 항상 Idle 애니메이션 재생
        enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);
    }

    IEnumerator SpawnLoop()
    {
        while (isLive)
        {
            SpawnMinion();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnMinion()
    {
        GameObject minion = Instantiate(spawnPrefab, transform.position, Quaternion.identity);
        minion.transform.localScale = spawnPrefab.transform.localScale * 0.5f; // 크기 0.5배로 축소
    }

    private void OnDestroy()
    {
        isLive = false; // 파괴 시 스폰 중단
    }
}
