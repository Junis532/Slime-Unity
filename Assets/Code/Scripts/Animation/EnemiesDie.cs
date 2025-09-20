using DG.Tweening;
using UnityEngine;

public class EnemiesDie : MonoBehaviour
{
    private bool isLive = true;
    private GroupController groupController;

    //[Header("죽을 때 드랍할 코인")]
    //public GameObject coinPrefab;

    [Header("죽을 때 포션")]
    public GameObject potionPrefab; // 풀에 등록 필요
    public float potionDropChance = 0.1f; // 10% 확률로 포션 드랍

    public void SetGroupController(GroupController group)
    {
        this.groupController = group;
    }

    public void Die()
    {
        if (!isLive) return;
        isLive = false;

        //if (coinPrefab != null)
        //{
        //    PoolManager.Instance.SpawnFromPool(coinPrefab.name, transform.position, Quaternion.identity);
        //}

        if (potionPrefab != null)
        {
            if (Random.value <= potionDropChance)
            {
                PoolManager.Instance.SpawnFromPool(potionPrefab.name, transform.position, Quaternion.identity);
            }
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
            // 플레이어 없으면 기존 기본값 사용
            backwardDir = -transform.right * 0.5f;
        }

        Sequence deathSequence = DOTween.Sequence();

        deathSequence.Append(transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack));
        deathSequence.Join(transform.DOMove(transform.position + backwardDir, 0.5f).SetEase(Ease.OutQuad));
        deathSequence.Join(transform.DORotate(new Vector3(0, 0, 360), 0.5f, RotateMode.FastBeyond360));

        deathSequence.OnComplete(() =>
        {
            DOTween.Kill(transform);
            gameObject.SetActive(false);

            if (groupController != null)
                groupController.OnChildDie();
        });
    }


    void OnEnable()
    {
        isLive = true;
    }

    void OnDisable()
    {
        DOTween.Kill(transform);
    }
}
