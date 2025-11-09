using UnityEngine;

public class Stage7ClearTrigger : MonoBehaviour
{
    private bool triggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (other.CompareTag("Player"))
        {
            triggered = true;
            GameManager.Instance.waveManager.Stage7ClearSequence();
        }
    }
}
