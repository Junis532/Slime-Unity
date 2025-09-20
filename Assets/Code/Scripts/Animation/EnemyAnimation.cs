using UnityEngine;
using System.Collections.Generic;

public class EnemyAnimation : MonoBehaviour
{
    [System.Serializable]
    public enum State { Idle, Move };

    public List<Sprite> idleSprites;
    public List<Sprite> moveSprites;

    public float frameRate = 0.1f;

    private SpriteRenderer spriteRenderer;
    private float timer;                   // 애니메이션 프레임 전환을 위한 타이머
    private int currentFrame;              // 현재 표시 중인 프레임 인덱스
    public State currentState;            // 현재 플레이어 상태
    private List<Sprite> currentSprites;   // 현재 상태에 해당하는 스프라이트 리스트

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // 스프라이트 리스트가 비어있으면 실행하지 않음
        if (currentSprites == null || currentSprites.Count == 0) return;

        // 시간 경과 측정
        timer += Time.deltaTime;

        // 설정된 프레임 간격을 넘겼다면 다음 프레임으로 전환
        if (timer >= frameRate)
        {
            timer = 0f;
            currentFrame = (currentFrame + 1) % currentSprites.Count;
            spriteRenderer.sprite = currentSprites[currentFrame];
        }
    }

    // 상태를 받아서 해당 상태의 애니메이션을 재생
    public void PlayAnimation(State newState)
    {
        // 같은 상태로 다시 전환하려고 하면 무시
        if (newState == currentState) return;

        // 상태 갱신 및 초기화
        currentState = newState;
        currentFrame = 0;
        timer = 0f;

        // 상태에 맞는 스프라이트 리스트를 설정
        currentSprites = (currentState == State.Idle) ? idleSprites : moveSprites;

        // 첫 번째 스프라이트로 초기화
        spriteRenderer.sprite = currentSprites[0];
    }
}