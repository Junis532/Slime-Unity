using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(SpriteRenderer))]
public class BossAnimation : MonoBehaviour
{
    public enum State
    {
        Idle,
        Move, MoveSide, MoveFront, MoveBack,

        // Skill 1, 2
        Skill1Fireball,
        Skill2Circle,

        // Skill 3 — 세분화(준비 → 대시 → 베기)
        Skill3DashStart,
        Skill3Dash,
        Skill3Slash
    }

    [Header("🎯 Target Renderer (자식 SR 사용 시 여기 지정)")]
    [SerializeField] private SpriteRenderer rendererOverride;

    [Header("📌 Idle / Move 스프라이트")]
    public List<Sprite> idleSprites;
    public List<Sprite> moveSprites;       // (옵션) 일반 Move
    public List<Sprite> moveSideSprites;   // 좌/우
    public List<Sprite> moveFrontSprites;  // 아래
    public List<Sprite> moveBackSprites;   // 위

    [Header("📌 스킬 1,2 스프라이트 (비루프)")]
    public List<Sprite> skill1FireballSprites;
    public List<Sprite> skill2CircleSprites;

    [Header("📌 스킬 3 (준비/대시/베기 각각 비루프)")]
    public List<Sprite> skill3DashStartSprites;
    public List<Sprite> skill3DashSprites;
    public List<Sprite> skill3SlashSprites;

    [Header("🎚️ 프레임 시간(초/프레임)")]
    public float idleFrameTime = 0.14f;
    public float moveFrameTime = 0.10f;
    public float skill1FrameTime = 0.12f;
    public float skill2FrameTime = 0.12f;
    public float skill3DashStartFrameTime = 0.09f;
    public float skill3DashFrameTime = 0.08f;
    public float skill3SlashFrameTime = 0.10f;

    [Header("⚡ 배속(전체/상태별) — 실제 대기 = frameTime / (global * stateSpeed)")]
    [Min(0.05f)] public float globalSpeed = 1.0f;
    [Min(0.05f)] public float idleSpeed = 1.0f;
    [Min(0.05f)] public float moveSpeed = 1.0f;
    [Min(0.05f)] public float skill1Speed = 1.0f;
    [Min(0.05f)] public float skill2Speed = 1.0f;
    [Min(0.05f)] public float skill3DashStartSpeed = 1.0f;
    [Min(0.05f)] public float skill3DashSpeed = 1.0f;
    [Min(0.05f)] public float skill3SlashSpeed = 1.0f;

    [Header("🧭 이동 방향 판정")]
    [Tooltip("수직/수평 전환 임계값 (클수록 수직 판정이 엄격)")]
    public float verticalThreshold = 0.5f;

    [Header("🛠 옵션/디버그")]
    public bool autoPlayIdle = true;
    public bool logDebug = false;

    private SpriteRenderer spr;
    private Coroutine animCo;
    private List<Sprite> currentSprites;
    private int frameIndex;
    private State currentState = State.Idle;
    private bool skillPlaying = false;

    private void Awake()
    {
        spr = rendererOverride != null ? rendererOverride : GetComponent<SpriteRenderer>();
        if (!spr) Debug.LogError("[BossAnimation] SpriteRenderer가 없습니다. rendererOverride를 지정하세요.");
    }

    private void Start()
    {
        if (autoPlayIdle) PlayAnimation(State.Idle);
    }

    // ================= 공개 API =================

    /// <summary>상태 전환: 스킬 중엔 Idle 복귀 또는 같은 스킬-패밀리 내 전환만 허용</summary>
    public void PlayAnimation(State newState)
    {
        if (logDebug) Debug.Log($"[BossAnimation] 요청: {newState} (현재:{currentState}, skill:{skillPlaying})");

        // 스킬 중이면 같은 스킬3 패밀리 내부 전환은 허용, 그 외는 Idle만 허용
        if (skillPlaying && newState != State.Idle && !IsSameSkillFamily(currentState, newState))
            return;

        if (newState == currentState && animCo != null) return;

        var sprites = GetSprites(newState);
        if (sprites == null || sprites.Count == 0)
        {
            if (logDebug) Debug.LogWarning($"[BossAnimation] {newState} 스프라이트 비어 전환 취소");
            return;
        }

        currentState = newState;

        bool isLoop =
            (newState == State.Idle ||
             newState == State.Move || newState == State.MoveSide ||
             newState == State.MoveFront || newState == State.MoveBack);

        // 어떤 상태가 끝나면 Idle로 돌아가야 하는지
        bool returnToIdleOnEnd =
            (newState == State.Skill1Fireball) ||
            (newState == State.Skill2Circle) ||
            (newState == State.Skill3Slash); // Skill3 최종단계만 Idle 복귀

        // 스킬 플래그
        skillPlaying = IsAnySkill(newState);

        if (animCo != null) StopCoroutine(animCo);
        animCo = StartCoroutine(PlayRoutine(sprites, GetWait(newState), isLoop, returnToIdleOnEnd));
    }

    /// <summary>대시 구간을 루프로 재생 (이동 동안 끊김 없이 보이도록)</summary>
    public void PlaySkill3DashLoop()
    {
        var sprites = GetSprites(State.Skill3Dash);
        if (sprites == null || sprites.Count == 0) return;

        if (animCo != null) StopCoroutine(animCo);
        currentState = State.Skill3Dash;
        skillPlaying = true; // 스킬 진행 중
        animCo = StartCoroutine(PlayRoutine(sprites, GetWait(State.Skill3Dash), isLoop: true, returnToIdleOnEnd: false));
    }

    /// <summary>방향 이동 애니메이션 (스킬 중에는 무시)</summary>
    public void PlayDirectionalMoveAnimation(Vector2 moveDir)
    {
        if (skillPlaying) return;

        if (moveDir.sqrMagnitude < 0.0001f)
        {
            PlayAnimation(State.Idle);
            return;
        }

        State targetState;
        List<Sprite> sprites;

        if (Mathf.Abs(moveDir.y) > verticalThreshold * Mathf.Abs(moveDir.x))
        {
            targetState = (moveDir.y > 0) ? State.MoveBack : State.MoveFront;
            sprites = (moveDir.y > 0) ? moveBackSprites : moveFrontSprites;
        }
        else
        {
            targetState = State.MoveSide;
            sprites = (moveSideSprites != null && moveSideSprites.Count > 0) ? moveSideSprites : moveSprites;
        }

        if (sprites == null || sprites.Count == 0) return;
        if (targetState == currentState && animCo != null) return;

        currentState = targetState;
        skillPlaying = false;
        if (animCo != null) StopCoroutine(animCo);
        animCo = StartCoroutine(PlayRoutine(sprites, GetWait(State.Move), true, false));
    }

    public State GetCurrentState() => currentState;

    /// <summary>비루프 애니 길이(초): 스프라이트수 × 프레임대기(현재 배속 반영)</summary>
    public float GetNonLoopDuration(State s)
    {
        var sprites = GetSprites(s);
        if (sprites == null || sprites.Count == 0) return 0f;
        return sprites.Count * GetWait(s);
    }

    // ================= 내부 구현 =================

    private IEnumerator PlayRoutine(List<Sprite> sprites, float wait, bool isLoop, bool returnToIdleOnEnd)
    {
        if (sprites == null || sprites.Count == 0) yield break;

        currentSprites = sprites;
        frameIndex = 0;

        while (true)
        {
            spr.sprite = currentSprites[frameIndex];

            // 루프/비루프 모두 런타임 배속 변경 반영
            float w = GetWait(currentState);
            yield return new WaitForSeconds(w);

            frameIndex++;

            if (isLoop)
            {
                frameIndex %= currentSprites.Count;
            }
            else
            {
                if (frameIndex >= currentSprites.Count) break;
            }
        }

        // 비루프 종료 처리
        if (!isLoop)
        {
            if (returnToIdleOnEnd)
            {
                skillPlaying = false;
                PlayAnimation(State.Idle);
            }
            // (스킬3 중간 단계: Idle로 자동복귀하지 않음)
        }
    }

    private List<Sprite> GetSprites(State s)
    {
        switch (s)
        {
            case State.Idle: return idleSprites;
            case State.Move: return moveSprites;
            case State.MoveSide: return moveSideSprites;
            case State.MoveFront: return moveFrontSprites;
            case State.MoveBack: return moveBackSprites;

            case State.Skill1Fireball: return skill1FireballSprites;
            case State.Skill2Circle: return skill2CircleSprites;

            case State.Skill3DashStart: return skill3DashStartSprites;
            case State.Skill3Dash: return skill3DashSprites;
            case State.Skill3Slash: return skill3SlashSprites;
        }
        return null;
    }

    private bool IsAnySkill(State s)
    {
        return s == State.Skill1Fireball ||
               s == State.Skill2Circle ||
               s == State.Skill3DashStart ||
               s == State.Skill3Dash ||
               s == State.Skill3Slash;
    }

    private bool IsSameSkillFamily(State a, State b)
    {
        // 현재 다단계는 Skill3
        bool a3 = (a == State.Skill3DashStart || a == State.Skill3Dash || a == State.Skill3Slash);
        bool b3 = (b == State.Skill3DashStart || b == State.Skill3Dash || b == State.Skill3Slash);
        if (a3 && b3) return true;

        // 스킬1/2는 단일 단계
        if (a == State.Skill1Fireball && b == State.Skill1Fireball) return true;
        if (a == State.Skill2Circle && b == State.Skill2Circle) return true;

        return false;
    }

    private float GetWait(State s)
    {
        float baseTime, mult;
        switch (s)
        {
            case State.Idle: baseTime = idleFrameTime; mult = idleSpeed; break;

            case State.Move:
            case State.MoveSide:
            case State.MoveFront:
            case State.MoveBack:
                baseTime = moveFrameTime; mult = moveSpeed; break;

            case State.Skill1Fireball: baseTime = skill1FrameTime; mult = skill1Speed; break;
            case State.Skill2Circle: baseTime = skill2FrameTime; mult = skill2Speed; break;

            case State.Skill3DashStart: baseTime = skill3DashStartFrameTime; mult = skill3DashStartSpeed; break;
            case State.Skill3Dash: baseTime = skill3DashFrameTime; mult = skill3DashSpeed; break;
            case State.Skill3Slash: baseTime = skill3SlashFrameTime; mult = skill3SlashSpeed; break;

            default: baseTime = 0.12f; mult = 1f; break;
        }
        float speed = Mathf.Max(0.05f, globalSpeed * Mathf.Max(0.05f, mult));
        return Mathf.Max(0.005f, baseTime / speed);
    }
}
