using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class LoadingAnimation : MonoBehaviour
{
    public List<Sprite> moveSprites;
    public float frameRate = 0.1f;

    private Image image;
    private float timer;
    private int currentFrame;

    void Start()
    {
        image = GetComponent<Image>();
        if (moveSprites != null && moveSprites.Count > 0)
        {
            image.sprite = moveSprites[0];
        }
    }

    void Update()
    {
        if (moveSprites == null || moveSprites.Count == 0) return;

        timer += Time.unscaledDeltaTime;

        if (timer >= frameRate)
        {
            timer = 0f;
            currentFrame = (currentFrame + 1) % moveSprites.Count;
            image.sprite = moveSprites[currentFrame];
        }
    }
}
