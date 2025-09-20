using UnityEngine;

public class PlayerSize : MonoBehaviour
{
    void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.playerStats == null) return;

        float currentHP = GameManager.Instance.playerStats.currentHP;

        float scaleFactor = 0.3f;  // 기본 체력 10일 때 크기 3을 만들기 위한 계수

        // 최소 크기 1 유지
        float newScale = Mathf.Max(1f, currentHP * scaleFactor);

        transform.localScale = new Vector3(newScale, newScale, transform.localScale.z);
    }
}
