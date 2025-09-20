using UnityEngine;
using TMPro;

public class Timer : MonoBehaviour
{
    public TMP_Text timerText;
    public float timeRemaining;
    public bool timerRunning = true;

    public void UpdateTimerDisplay()
    {
        int seconds = Mathf.CeilToInt(timeRemaining);
        timerText.text = seconds.ToString();

        // 알파값 조절
        Color c = timerText.color;
        c.a = (timeRemaining > 0f) ? 1f : 0f;
        timerText.color = c;
    }

    public void ResetTimer(float newTime)
    {
        timeRemaining = newTime;
        timerRunning = true;
        UpdateTimerDisplay();
    }

}
