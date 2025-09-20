using System.Collections;
using UnityEngine;

public class MucusProjectile : MonoBehaviour
{
    public float speed = 10f;
    public GameObject slowAreaPrefab;

    private Vector3 targetPos;
    public float slowAreaDuration = 3f;

    // Init을 목표 위치를 직접 받도록 수정
    public void Init(Vector3 targetPosition)
    {
        targetPos = targetPosition;
        StartCoroutine(MoveToTarget());
    }

    private IEnumerator MoveToTarget()
    {
        while (Vector3.Distance(transform.position, targetPos) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
            yield return null;
        }

        SpawnSlowArea();
        Destroy(gameObject);
    }

    private void SpawnSlowArea()
    {
        if (slowAreaPrefab != null)
        {
            GameObject slowArea = Instantiate(slowAreaPrefab, targetPos, Quaternion.identity);
            Destroy(slowArea, slowAreaDuration);
        }
    }
}
