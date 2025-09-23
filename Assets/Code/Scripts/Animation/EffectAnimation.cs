using UnityEngine;
using System.Collections.Generic;

public class EffectAnimation : MonoBehaviour
{
    [Header("이펙트 애니메이션 스프라이트")]
    public List<Sprite> moveSprites;

    [Header("프레임 속도")]
    public float frameRate = 0.1f;

    private SpriteRenderer spriteRenderer;
    private float timer;
    private int currentFrame;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (moveSprites != null && moveSprites.Count > 0)
            spriteRenderer.sprite = moveSprites[0];
    }

    void Update()
    {
        if (moveSprites == null || moveSprites.Count == 0) return;

        timer += Time.deltaTime;
        if (timer >= frameRate)
        {
            timer = 0f;
            currentFrame++;

            // 끝까지 가면 다시 처음으로 (루프)
            if (currentFrame >= moveSprites.Count)
                currentFrame = 0;

            spriteRenderer.sprite = moveSprites[currentFrame];
        }
    }
}
