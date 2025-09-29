using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class TurretEnemyAnimation : MonoBehaviour
{
    [System.Serializable]
    // 상태 정의
    public enum State
    {
        Idle,
        Move,
        AttackStart,
        Attack,
        AttackEnd,
        MoveSide,
        MoveFront,
        MoveBack,
        FrontAttackEnd,

        // 🟢 발사 관련 단계 추가 (TurretEnemy_FixedAngle에서 사용)
        ShootPrepare,       // 발사 준비 (측면/일반)
        ShootPost,          // 발사 후 (측면/일반)
        FrontShootPrepare,  // 발사 준비 (정면/90도/270도)
        FrontShootPost,     // 발사 후 (정면/90도/270도)

        // 🟢 보스 스킬 상태
        Skill1Fireball,
        Skill2Circle,
        Skill3Dash
    };

    [Header("스프라이트 리스트 (기본/측면)")]
    public List<Sprite> idleSprites;

    [Header("정면 Idle 스프라이트 (90도/270도 용)")]
    public List<Sprite> idleFrontSprites;
    public List<Sprite> moveSprites;

    [Header("📌 발사 단계 스프라이트 (ShootPrepare/Post)")]
    public List<Sprite> shootPrepareSprites;
    public List<Sprite> shootPostSprites;

    [Header("📌 발사 단계 정면 스프라이트 (FrontShootPrepare/Post)")]
    public List<Sprite> shootPrepareFrontSprites;
    public List<Sprite> shootPostFrontSprites;

    // 기존 Attack 관련 필드
    public List<Sprite> attackStartSprites;
    public List<Sprite> attackSprites;
    public List<Sprite> attackEndSprites;
    [Header("정면 공격 애니메이션 (Front)")]
    public List<Sprite> attackStartFrontSprites;
    public List<Sprite> attackEndFrontSprites;

    // 방향별 이동 애니메이션
    public List<Sprite> moveSideSprites;
    public List<Sprite> moveFrontSprites;
    public List<Sprite> moveBackSprites;

    // 보스 스킬 애니메이션 스프라이트 리스트
    [Header("보스 스킬 애니메이션")]
    public List<Sprite> skill1FireballSprites;
    public List<Sprite> skill2CircleSprites;
    public List<Sprite> skill3DashSprites;

    [Header("설정")]
    public float frameRate = 0.1f;
    [Tooltip("Attack 관련 애니메이션 전용 프레임 간격 (초).")]
    public float attackFrameRate = 0.05f;

    [Header("발사 단계 애니메이션 속도")]
    public float shootPrepareFrameTime = 0.1f;
    public float shootPostFrameTime = 0.1f;
    [Min(0.05f)] public float shootPrepareSpeed = 1.0f;
    [Min(0.05f)] public float shootPostSpeed = 1.0f;

    [Tooltip("수직/수평 애니메이션 전환 임계값 (0에 가까울수록 민감)")]
    public float verticalThreshold = 0.5f;
    [Min(0.05f)] public float globalSpeed = 1.0f;

    private SpriteRenderer spriteRenderer;
    private int currentFrame;
    public State currentState;
    private List<Sprite> currentSprites;

    private Coroutine animationRoutine;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            PlayAnimation(State.Idle);
        }
    }

    void Update() { }

    private void StartAnimation(List<Sprite> sprites)
    {
        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
        }

        animationRoutine = StartCoroutine(AnimateRoutine(sprites));
    }

    private IEnumerator AnimateRoutine(List<Sprite> sprites)
    {
        currentSprites = sprites;
        currentFrame = 0;

        if (currentSprites == null || currentSprites.Count == 0)
        {
            spriteRenderer.sprite = null;
            animationRoutine = null;
            yield break;
        }

        // 루프 애니메이션 상태들
        bool isLooping = (currentState == State.Idle ||
                             currentState == State.Move ||
                             currentState == State.MoveSide ||
                             currentState == State.MoveFront ||
                             currentState == State.MoveBack ||
                             currentState == State.Attack);

        while (isLooping || currentFrame < currentSprites.Count)
        {
            if (currentSprites == null || currentSprites.Count == 0 || currentFrame >= currentSprites.Count) break;

            spriteRenderer.sprite = currentSprites[currentFrame];

            // GetWait() 함수를 사용하여 런타임 속도를 반영
            yield return new WaitForSeconds(GetWait(currentState));

            currentFrame++;

            if (isLooping)
            {
                currentFrame %= currentSprites.Count;
            }
        }

        // 🟢 비반복 애니메이션 (공격, 스킬, 발사 단계) 종료 후, 상태 복귀
        if (currentState == State.AttackEnd || currentState == State.FrontAttackEnd ||
            currentState == State.Skill1Fireball || currentState == State.Skill2Circle || currentState == State.Skill3Dash ||
            currentState == State.ShootPost || currentState == State.FrontShootPost)
        {
            PlayAnimation(State.Idle);
        }

        animationRoutine = null;
    }

    /// <summary>
    /// Idle, Move, Attack, Skill, Shoot 관련 상태로 전환합니다.
    /// </summary>
    public void PlayAnimation(State newState)
    {
        // 1. 액션 상태 중 보호 로직
        if (IsActionState(currentState))
        {
            // 발사 준비 상태일 때 발사 후 상태로 전환하는 것만 허용
            bool isShootTransition = (
                (currentState == State.ShootPrepare && newState == State.ShootPost) ||
                (currentState == State.FrontShootPrepare && newState == State.FrontShootPost)
            );

            // 허용되지 않은 전환이라면 무시
            if (!isShootTransition && newState != currentState)
            {
                return;
            }
        }

        // 2. 방향별 Move 상태로의 직접적인 전환 요청 방지 
        if (newState == State.MoveSide || newState == State.MoveFront || newState == State.MoveBack)
        {
            Debug.LogError("방향별 애니메이션은 PlayDirectionalMoveAnimation(Vector2)을 사용하세요.");
            return;
        }

        // 3. 같은 상태로 다시 전환하려고 하면 무시 
        if (newState == currentState && animationRoutine != null) return;

        // 4. 스프라이트 확인
        List<Sprite> targetSprites = GetSprites(newState);
        if (targetSprites == null || targetSprites.Count == 0)
        {
            if (newState != State.Idle)
            {
                Debug.LogWarning($"Enemy Animation: {newState}에 할당된 스프라이트가 없어 Idle로 전환합니다.");
                PlayAnimation(State.Idle);
            }
            return;
        }

        // 상태 갱신
        currentState = newState;
        StartAnimation(targetSprites);
    }

    /// <summary>
    /// 고정된 각도를 기반으로 Idle 애니메이션의 방향(측면/정면)을 결정합니다.
    /// </summary>
    public void PlayAnimation(State newState, float angle)
    {
        // Idle 상태가 아니거나, 현재 액션 상태(스킬/공격/발사) 중이면 무시
        if (newState != State.Idle || IsActionState(currentState))
        {
            if (newState != State.Idle && newState != currentState)
            {
                PlayAnimation(newState);
            }
            return;
        }

        // 1. 각도 정규화 및 수직 방향 판단.
        angle = (angle % 360 + 360) % 360;
        bool useFrontSprite = false;
        float verticalTolerance = 25f;

        if ((angle >= 90f - verticalTolerance && angle <= 90f + verticalTolerance) ||
            (angle >= 270f - verticalTolerance && angle <= 270f + verticalTolerance))
        {
            useFrontSprite = true;
        }

        List<Sprite> targetSprites;
        if (useFrontSprite && idleFrontSprites != null && idleFrontSprites.Count > 0)
        {
            targetSprites = idleFrontSprites;
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = false;
            }
        }
        else
        {
            targetSprites = idleSprites;

            // 측면(0도/180도)일 경우 좌우 반전 처리
            if (spriteRenderer != null && targetSprites == idleSprites)
            {
                // 180도 부근 (왼쪽)
                if (angle >= 180f - verticalTolerance && angle <= 180f + verticalTolerance)
                {
                    spriteRenderer.flipX = true;
                }
                // 0도/360도 부근 (오른쪽)
                else
                {
                    spriteRenderer.flipX = false;
                }
            }
        }

        // 상태 변경이 없어도, 스프라이트 리스트가 바뀌었다면 애니메이션 재생
        if (currentState != newState || currentSprites != targetSprites)
        {
            currentState = newState;
            if (targetSprites != null && targetSprites.Count > 0)
            {
                StartAnimation(targetSprites);
            }
        }
    }

    /// <summary>
    /// 이동 방향 벡터를 기반으로 MoveSide, MoveFront, MoveBack 중 하나로 전환합니다.
    /// </summary>
    public void PlayDirectionalMoveAnimation(Vector2 moveDirection)
    {
        if (IsActionState(currentState)) return;

        if (moveDirection.magnitude < 0.01f)
        {
            PlayAnimation(State.Idle);
            return;
        }

        State targetState;
        List<Sprite> sprites;

        if (Mathf.Abs(moveDirection.y) > verticalThreshold * Mathf.Abs(moveDirection.x))
        {
            targetState = (moveDirection.y > 0) ? State.MoveBack : State.MoveFront;
            sprites = (moveDirection.y > 0) ? moveBackSprites : moveFrontSprites;
        }
        else
        {
            targetState = State.MoveSide;
            sprites = (moveSideSprites != null && moveSideSprites.Count > 0) ? moveSideSprites : moveSprites;
        }

        if (targetState == currentState) return;

        currentState = targetState;

        if (sprites != null && sprites.Count > 0)
        {
            StartAnimation(sprites);
        }
        else
        {
            Debug.LogWarning($"Enemy Animation: {targetState}에 할당된 스프라이트가 없어 Idle로 대체합니다.");
            PlayAnimation(State.Idle);
        }
    }

    // ================= 헬퍼 함수 =================

    private bool IsActionState(State s)
    {
        return s == State.AttackStart || s == State.Attack || s == State.AttackEnd || s == State.FrontAttackEnd ||
            s == State.Skill1Fireball || s == State.Skill2Circle || s == State.Skill3Dash ||
            s == State.ShootPrepare || s == State.FrontShootPrepare || s == State.ShootPost || s == State.FrontShootPost;
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

            // 기존 Attack 계열
            case State.AttackStart: return attackStartSprites;
            case State.Attack: return attackSprites;
            case State.AttackEnd: return attackEndSprites;
            case State.FrontAttackEnd: return attackEndFrontSprites;

            // 🟢 Shoot 계열
            case State.ShootPrepare: return shootPrepareSprites;
            case State.ShootPost: return shootPostSprites;
            case State.FrontShootPrepare: return shootPrepareFrontSprites;
            case State.FrontShootPost: return shootPostFrontSprites;

            // Skill 계열
            case State.Skill1Fireball: return skill1FireballSprites;
            case State.Skill2Circle: return skill2CircleSprites;
            case State.Skill3Dash: return skill3DashSprites;
        }
        return null;
    }

    private float GetWait(State s)
    {
        float baseTime, mult;

        switch (s)
        {
            case State.Idle:
            case State.Move:
            case State.MoveSide:
            case State.MoveFront:
            case State.MoveBack:
                baseTime = frameRate; mult = 1.0f; break;

            case State.AttackStart:
            case State.Attack:
            case State.AttackEnd:
            case State.FrontAttackEnd:
                baseTime = attackFrameRate; mult = 1.0f; break;

            // 🟢 Shoot 계열 속도
            case State.ShootPrepare:
            case State.FrontShootPrepare:
                baseTime = shootPrepareFrameTime; mult = shootPrepareSpeed; break;
            case State.ShootPost:
            case State.FrontShootPost:
                baseTime = shootPostFrameTime; mult = shootPostSpeed; break;

            // Skill 계열 (임시 기본값)
            case State.Skill1Fireball: baseTime = 0.12f; mult = 1.0f; break;
            case State.Skill2Circle: baseTime = 0.12f; mult = 1.0f; break;
            case State.Skill3Dash: baseTime = 0.08f; mult = 1.0f; break;

            default: baseTime = frameRate; mult = 1f; break;
        }

        float speed = Mathf.Max(0.05f, globalSpeed * Mathf.Max(0.05f, mult));
        return Mathf.Max(0.005f, baseTime / speed);
    }

    /// <summary> 비루프 애니메이션의 총 재생 시간(초)을 반환합니다. </summary>
    public float GetNonLoopDuration(State s)
    {
        var sprites = GetSprites(s);
        if (sprites == null || sprites.Count == 0) return 0f;
        // 총 스프라이트 수 * 프레임당 대기 시간
        return sprites.Count * GetWait(s);
    }

    /// <summary> 현재 재생 중인 애니메이션 상태를 반환합니다. </summary>
    public State GetCurrentState()
    {
        return currentState;
    }
}