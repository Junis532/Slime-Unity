using UnityEngine;

public class PotionBehavior : MonoBehaviour
{
    private float timer = 0f;
    private float lifeTime = 2f;

    public void StartLifetime(float t)
    {
        lifeTime = t;
        timer = 0f;
    }

    void OnEnable()
    {
        timer = 0f;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= lifeTime)
        {
            PoolManager.Instance.ReturnToPool(gameObject);
        }
    }
}
