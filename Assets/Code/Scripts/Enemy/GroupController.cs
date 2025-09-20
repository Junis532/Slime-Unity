using UnityEngine;

public class GroupController : MonoBehaviour
{
    private int aliveCount;

    void Start()
    {
        // 자식 개수로 aliveCount 초기화
        aliveCount = transform.childCount;

        foreach (Transform child in transform)
        {
            EnemiesDie dieScript = child.GetComponent<EnemiesDie>();
            if (dieScript != null)
            {
                dieScript.SetGroupController(this);
            }
        }
    }

    // 개별 자식이 죽을 때마다 호출됨
    public void OnChildDie()
    {
        aliveCount--;
        if (aliveCount <= 0)
        {
            // 그룹 전체 반환
            PoolManager.Instance.ReturnToPool(this.gameObject);
        }
    }

    // 그룹 재사용될 때 aliveCount도 재초기화 필요
    void OnEnable()
    {
        aliveCount = transform.childCount;
    }
}
