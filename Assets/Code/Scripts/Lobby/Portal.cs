using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class Portal : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            LoadingManager.LoadScene("InGame");
        }
    }
}
