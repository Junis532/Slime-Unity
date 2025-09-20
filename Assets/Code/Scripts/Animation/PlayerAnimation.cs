using UnityEngine;
using System.Collections.Generic;

public class PlayerAnimation : MonoBehaviour
{
    [System.Serializable]
    public enum State { Idle, Move }

    public List<Sprite> idleSprites;
    public List<Sprite> moveSprites;

    public float frameRate = 0.1f;

    [Header("Move 상태 이펙트")]
    public GameObject effectPrefab; // 생성할 이펙트 프리팹
    public float effectSpawnInterval = 1f; // 1초마다 생성
    public float effectLifeTime = 0.3f;    // 0.3초 후 파괴

    private SpriteRenderer spriteRenderer;
    private float timer;
    private int currentFrame;
    public State currentState;
    private List<Sprite> currentSprites;

    // 이펙트 관련 타이머
    private float effectTimer = 0f;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // 기본 상태로 초기화
        PlayAnimation(currentState);
    }

    void Update()
    {
        if (currentSprites == null || currentSprites.Count == 0) return;

        // 애니메이션 프레임 갱신
        timer += Time.deltaTime;
        if (timer >= frameRate)
        {
            timer = 0f;
            currentFrame = (currentFrame + 1) % currentSprites.Count;
            spriteRenderer.sprite = currentSprites[currentFrame];
        }

        // Move 상태일 때만 이펙트 생성 로직
        if (currentState == State.Move && effectPrefab != null)
        {
            effectTimer += Time.deltaTime;
            if (effectTimer >= effectSpawnInterval)
            {
                effectTimer = 0f;

                // 플레이어 위치(로컬 0,0,0) 기준 생성
                GameObject effect = Instantiate(effectPrefab, transform.position, Quaternion.identity);

                Destroy(effect, effectLifeTime);
            }
        }

    }

    public void PlayAnimation(State newState)
    {
        if (newState == currentState && currentSprites != null && currentSprites.Count > 0) return;

        currentState = newState;
        currentFrame = 0;
        timer = 0f;

        currentSprites = (currentState == State.Idle) ? idleSprites : moveSprites;

        if (spriteRenderer != null && currentSprites.Count > 0)
            spriteRenderer.sprite = currentSprites[0];
    }
}
