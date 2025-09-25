using UnityEngine;
using System.Collections.Generic;

public class EffectAnimation : MonoBehaviour
{
    [Header("이펙트 애니메이션 스프라이트")]
    public List<Sprite> moveSprites;

    [Header("프레임 속도")]
    public float frameRate = 0.1f;

    [Header("한 번만 재생할지 여부")]
    public bool playOnce = false; // true면 한 번만 재생, false면 루프

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

            if (currentFrame >= moveSprites.Count)
            {
                if (playOnce)
                {
                    // 한 번 재생 후 오브젝트 제거
                    Destroy(gameObject);
                    return;
                }
                else
                {
                    currentFrame = 0; // 루프
                }
            }

            spriteRenderer.sprite = moveSprites[currentFrame];
        }
    }
}
