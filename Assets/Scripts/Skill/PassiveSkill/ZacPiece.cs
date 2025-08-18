using UnityEngine;

public class ZacPiece : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.Instance.playerStats.currentHP += 1;
            if (GameManager.Instance.playerStats.currentHP > GameManager.Instance.playerStats.maxHP)
            {
                GameManager.Instance.playerStats.currentHP = GameManager.Instance.playerStats.maxHP;
            }
            PoolManager.Instance.ReturnToPool(gameObject);
        }
    }
}
