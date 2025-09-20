using UnityEngine;

public class BowEffectFollow : MonoBehaviour
{
    public Vector3 offset;   // Player에서 떨어진 거리
    public float duration = 0.5f; // 사라지기 전까지 따라다니는 시간

    private Transform target;
    private float timer = 0f;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            target = playerObj.transform;
    }

    void Update()
    {
        if (target != null)
        {
            transform.position = target.position + offset;
        }

        timer += Time.deltaTime;
        if (timer >= duration)
        {
            Destroy(gameObject);
        }
    }
}
