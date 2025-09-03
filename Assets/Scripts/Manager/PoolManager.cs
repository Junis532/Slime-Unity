using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PoolInfo
{
    public string prefabName;
    public GameObject prefab;
    public int initialSize = 10;
}

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }
    public PoolInfo[] pools;
    private Dictionary<string, Queue<GameObject>> poolDict;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        poolDict = new Dictionary<string, Queue<GameObject>>();
        foreach (var pool in pools)
        {
            var queue = new Queue<GameObject>();
            for (int i = 0; i < pool.initialSize; i++)
            {
                GameObject obj = Instantiate(pool.prefab);
                obj.name = pool.prefabName;  // ★★★ 이름 강제 세팅!
                obj.SetActive(false);
                obj.transform.SetParent(transform);
                queue.Enqueue(obj);
            }
            poolDict.Add(pool.prefabName, queue);
        }
    }

    public GameObject SpawnFromPool(string prefabName, Vector3 pos, Quaternion rot)
    {
        if (!poolDict.ContainsKey(prefabName))
        {
            Debug.LogWarning($"{prefabName}에 해당하는 풀 없음!");
            return null;
        }
        GameObject obj = null;
        if (poolDict[prefabName].Count > 0)
        {
            obj = poolDict[prefabName].Dequeue();
        }
        else
        {
            var poolInfo = System.Array.Find(pools, x => x.prefabName == prefabName);
            if (poolInfo != null)
            {
                obj = Instantiate(poolInfo.prefab);
                obj.name = prefabName;
            }
        }
        if (obj == null) return null;

        obj.transform.SetParent(null);
        obj.transform.position = pos;
        obj.transform.rotation = rot;
        obj.SetActive(true);
        return obj;
    }

    public void ReturnToPool(GameObject obj)
    {
        obj.SetActive(false);
        obj.transform.SetParent(transform);

        // ★ 이름이 없거나 잘못되어 있으면 Destroy, 아니면 풀에 반환
        if (poolDict.ContainsKey(obj.name))
        {
            poolDict[obj.name].Enqueue(obj);
        }
        else
        {
            Debug.LogWarning($"[{obj.name}]은(는) 풀에 등록되지 않은 오브젝트라 Destroy합니다.");
            Destroy(obj);
        }
    }
}
