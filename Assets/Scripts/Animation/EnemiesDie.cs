using DG.Tweening;
using UnityEngine;

public class EnemiesDie : MonoBehaviour
{
    private bool isLive = true;
    private GroupController groupController;

    [Header("죽을 때 드랍할 코인")]
    public GameObject coinPrefab;

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

        if (coinPrefab != null)
        {
            // PoolManager로 코인 소환
            PoolManager.Instance.SpawnFromPool(coinPrefab.name, transform.position, Quaternion.identity);
        }

        if (potionPrefab != null)
        {
            if (Random.value <= potionDropChance)
            {
                PoolManager.Instance.SpawnFromPool(potionPrefab.name, transform.position, Quaternion.identity);
            }
            
        }

        Sequence deathSequence = DOTween.Sequence();

        Vector3 backwardDir = -transform.right * 0.5f;

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
