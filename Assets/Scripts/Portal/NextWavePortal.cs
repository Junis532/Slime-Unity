using UnityEngine;

public class NextWavePortal : MonoBehaviour
{
    private bool waveStarted = false;
    private bool playerInside = false;
    private float stayTimer = 0f;
    public float requiredStayTime = 3f;

    private void Update()
    {
        if (waveStarted || !playerInside) return;

        stayTimer += Time.deltaTime;

        if (stayTimer >= requiredStayTime)
        {
            waveStarted = true;
            GameManager.Instance.waveManager.StartNextWave();
            Debug.Log("플레이어가 3초간 포탈 안에 머물러 다음 웨이브 시작!");

            Destroy(gameObject);  // 포탈 오브젝트 삭제 (한 번만 작동)
        }

    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = true;
            stayTimer = 0f; // 시간 초기화
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
            stayTimer = 0f; // 나가면 타이머 리셋
        }
    }
}
