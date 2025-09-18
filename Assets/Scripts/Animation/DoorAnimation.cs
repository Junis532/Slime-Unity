using UnityEngine;
using System.Collections.Generic;

public class DoorAnimation : MonoBehaviour
{
    [System.Serializable]
    public enum DoorState { Closed, Open }

    [Header("문 애니메이션 스프라이트")]
    public List<Sprite> openSprites;
    public List<Sprite> closeSprites;

    [Header("프레임 속도")]
    public float frameRate = 0.1f;

    private SpriteRenderer spriteRenderer;
    private float timer;
    private int currentFrame;
    private DoorState currentState;
    private List<Sprite> currentSprites;
    private bool isPlaying;   // 현재 애니메이션 실행 중인지 여부

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

    }

    void Update()
    {
        if (!isPlaying || currentSprites == null || currentSprites.Count == 0) return;

        timer += Time.deltaTime;
        if (timer >= frameRate)
        {
            timer = 0f;
            currentFrame++;

            if (currentFrame >= currentSprites.Count)
            {
                currentFrame = currentSprites.Count - 1; // 마지막 프레임 유지
                isPlaying = false; // 애니메이션 종료
            }

            spriteRenderer.sprite = currentSprites[currentFrame];
        }
    }

    /// <summary>
    /// 문 상태에 따라 애니메이션 실행
    /// </summary>
    public void PlayAnimation(DoorState newState)
    {
        // 상태 갱신
        currentState = newState;
        currentFrame = 0;
        timer = 0f;
        isPlaying = true;

        currentSprites = (currentState == DoorState.Open) ? openSprites : closeSprites;

        if (currentSprites != null && currentSprites.Count > 0)
            spriteRenderer.sprite = currentSprites[0];
    }
}
