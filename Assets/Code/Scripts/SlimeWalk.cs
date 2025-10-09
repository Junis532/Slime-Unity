using UnityEngine;

public class SlimeWalkSimple : MonoBehaviour
{
    [Header("Sprite Animation")]
    public SpriteRenderer spriteRenderer;   // 슬라임의 SpriteRenderer
    public Sprite[] frames;                 // 0 = Idle, 1..N = 걷기
    public float frameRate = 0.12f;         // 프레임 전환 간격(초)

    [Header("Movement")]
    public float moveSpeed = 1.2f;          // 오른쪽 이동 속도
    public Vector2 moveDir = Vector2.right; // 기본 오른쪽

    // 내부
    bool isWalking = false;
    float walkLeft = 0f;
    float t = 0f;
    int loopStart = 1, loopEnd = 1;

    void Awake()
    {
        if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
        if (frames != null && frames.Length > 0) spriteRenderer.sprite = frames[0];
        if (frames != null && frames.Length > 1) { loopStart = 1; loopEnd = frames.Length - 1; }
        moveDir = (moveDir.sqrMagnitude < 0.0001f) ? Vector2.right : moveDir.normalized;
    }

    void Update()
    {
        // 이동/애니메이션
        if (isWalking)
        {
            walkLeft -= Time.deltaTime;
            if (walkLeft <= 0f) isWalking = false;

            // 이동
            transform.position += (Vector3)(moveDir * moveSpeed * Time.deltaTime);

            // 애니(간단 루프)
            if (frames != null && frames.Length > 1)
            {
                t += Time.deltaTime;
                if (t >= frameRate)
                {
                    t = 0f;
                    // 현재 프레임이 idle(0)이면 루프 시작으로 점프
                    int cur = System.Array.IndexOf(frames, spriteRenderer.sprite);
                    if (cur < loopStart || cur >= loopEnd) cur = loopStart - 1;
                    spriteRenderer.sprite = frames[Mathf.Clamp(cur + 1, loopStart, loopEnd)];
                }
            }
        }
        else
        {
            // 정지: 항상 idle(0) 고정
            if (frames != null && frames.Length > 0) spriteRenderer.sprite = frames[0];
        }
    }

    public void StartWalking(float durationSeconds)
    {
        if (frames == null || frames.Length == 0) { Debug.LogWarning("SlimeWalkSimple: frames 비어있음"); return; }
        isWalking = true;
        walkLeft = Mathf.Max(0f, durationSeconds);
        t = 0f;
        spriteRenderer.sprite = frames[0]; // 시작은 idle에서
    }
}
