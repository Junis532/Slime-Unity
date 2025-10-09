using System.Collections;
using UnityEngine;
using DG.Tweening;

public class FallingRock : MonoBehaviour
{
    public GameObject warningPrefab;
    public float warningHeightOffset = 5f;
    public float fallDuration = 1f;

    public void StartFall(Vector3 targetPosition)
    {
        StartCoroutine(FallRoutine(targetPosition));
    }

    private IEnumerator FallRoutine(Vector3 targetPosition)
    {
        // 1️⃣ 경고 표시
        if (warningPrefab != null)
        {
            Vector3 warningPos = targetPosition;
            warningPos.y += warningHeightOffset;
            GameObject warning = Instantiate(warningPrefab, warningPos, Quaternion.identity);

            SpriteRenderer sr = warning.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = new Color(1, 0, 0, 0);
                sr.DOFade(1f, 0.3f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutQuad);
            }

            yield return new WaitForSeconds(0.5f);
            Destroy(warning);
        }

        // 2️⃣ 낙석 소환
        Vector3 spawnPos = targetPosition;
        spawnPos.y += warningHeightOffset;
        GameObject rock = Instantiate(gameObject, spawnPos, Quaternion.identity);

        // 3️⃣ 낙석 이동
        rock.transform.DOMove(targetPosition, fallDuration).SetEase(Ease.InSine);

    }
}
