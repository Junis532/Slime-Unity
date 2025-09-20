using UnityEngine;

public class ESCKey : MonoBehaviour
{
    public GameObject targetCanvas; // UI 캔버스
    private bool isPaused = false; // 현재 일시정지 상태
    
    void Awake()
    {
        targetCanvas.SetActive(false); // 게임 시작 시 비활성화
        Time.timeScale = 1.0f; // 게임 시작 시 확실하게 1로 설정

        openOption();
    }

    void Update()
    {
        openOption();
    }


    // 옵션창 띄우기 함수
    void openOption()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) // ESC 키 입력 감지
        {
            isPaused = !isPaused; // 상태 반전
            Debug.Log(isPaused);
            targetCanvas.SetActive(isPaused); // 옵션창 표시 여부 변경
            
            // 시간 제어
            Time.timeScale = isPaused ? 0f : 1f; // 옵션창 활성화 시 정지, 비활성화 시 원래대로
        }
    }
}
