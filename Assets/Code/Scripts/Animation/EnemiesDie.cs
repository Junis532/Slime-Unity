using DG.Tweening;
using UnityEngine;

public class EnemiesDie : MonoBehaviour
{
    private bool isLive = true;
    private GroupController groupController;

    [Header("죽을 때 포션")]
    public GameObject potionPrefab; // 풀에 등록 필요
    public float potionDropChance = 0.1f; // 10% 확률로 포션 드랍

    private SpriteRenderer spriter;

    void Awake()
    {
        spriter = GetComponent<SpriteRenderer>();
    }

    public void SetGroupController(GroupController group)
    {
        this.groupController = group;
    }

    public void Die()
    {
        if (!isLive) return;
        isLive = false;

        if (potionPrefab != null && Random.value <= potionDropChance)
        {
            PoolManager.Instance.SpawnFromPool(potionPrefab.name, transform.position, Quaternion.identity);
        }

        // 플레이어 위치 태그로 찾기
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        Vector3 backwardDir = Vector3.zero;
        if (playerObj != null)
        {
            Vector3 dirToPlayer = (playerObj.transform.position - transform.position).normalized;
            backwardDir = -dirToPlayer * 0.5f;  // 플레이어 반대 방향으로 0.5만큼 밀기
        }
        else
        {
            backwardDir = -transform.right * 0.7f;
        }

        Sequence deathSequence = DOTween.Sequence();

        // 뒤로 밀리면서 축소
        deathSequence.Append(transform.DOMove(transform.position + backwardDir, 0.5f).SetEase(Ease.OutQuad));
        deathSequence.Join(transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack));

        // 투명해지기
        if (spriter != null)
        {
            deathSequence.Join(spriter.DOFade(0f, 0.5f));
        }

        deathSequence.OnComplete(() =>
        {
            DOTween.Kill(transform);
            if (spriter != null) spriter.color = new Color(1, 1, 1, 1); // 재활성화 대비
            gameObject.SetActive(false);

            if (groupController != null)
                groupController.OnChildDie();
        });
    }

    void OnEnable()
    {
        isLive = true;
        if (spriter != null)
            spriter.color = new Color(1, 1, 1, 1); // 초기 투명도 복구
    }

    void OnDisable()
    {
        DOTween.Kill(transform);
    }
}
