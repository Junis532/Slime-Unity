using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyAnimation : MonoBehaviour
{
    [System.Serializable]
    public enum State
    {
        // 기본/이동/공격(기존)
        Idle,
        Move,
        AttackStart,
        Attack,
        AttackEnd,
        MoveSide,
        MoveFront,
        MoveBack,
        FrontAttackEnd,

        // 스킬(기존)
        Skill1Fireball,
        Skill2Circle,
        Skill3Dash,

        // 연출/보스 전용 (추가)
        Entry,          // 등장 (원샷 → Idle)
        PatternStart,   // 패턴 시작 (원샷 → Idle)
        PatternLoop,    // 패턴 진행 (마커) → Idle 유지(보스 모드에서만)
        PatternEnd,     // 패턴 종료 (마커) → Idle 유지(보스 모드에서만)
        Death           // 사망 (원샷 → 잠금)
    };

    // ===== 스프라이트 슬롯 =====
    [Header("스프라이트 (기본/측면)")]
    public List<Sprite> idleSprites;
    public List<Sprite> moveSprites;

    [Header("공격(측면/정면)")]
    public List<Sprite> attackStartSprites;   // 비루프
    public List<Sprite> attackSprites;        // 루프
    public List<Sprite> attackEndSprites;     // 비루프
    public List<Sprite> attackStartFrontSprites;
    public List<Sprite> attackEndFrontSprites;

    [Header("방향 이동")]
    public List<Sprite> moveSideSprites;
    public List<Sprite> moveFrontSprites;
    public List<Sprite> moveBackSprites;

    [Header("스킬(기존)")]
    public List<Sprite> skill1FireballSprites;
    public List<Sprite> skill2CircleSprites;
    public List<Sprite> skill3DashSprites;

    [Header("연출/보스 전용")]
    public List<Sprite> entrySprites;         // 원샷
    public List<Sprite> patternStartSprites;  // 원샷
    public List<Sprite> deathSprites;         // 원샷(잠금)

    // ===== 설정 =====
    [Header("속도/설정")]
    public float frameRate = 0.1f;
    [Tooltip("빠른 원샷류(AttackStart/End/Entry/PatternStart/Death)에 적용")]
    public float attackFrameRate = 0.05f;
    [Tooltip("수직/수평 전환 임계값")]
    public float verticalThreshold = 0.5f;

    [Header("중간보스 전용 옵션")]
    [Tooltip("ON: 패턴은 PatternStart만 연출, PatternLoop/End/Skill*은 Idle 유지")]
    public bool bossPatternIdleMode = false;

    [Tooltip("ON: entrySprites가 있으면 Start 시 Entry 한 번 자동 재생")]
    public bool autoPlayEntry = false;

    // ===== 내부 상태 =====
    private SpriteRenderer spriteRenderer;
    private int currentFrame;
    public State currentState;
    private List<Sprite> currentSprites;
    private Coroutine animationRoutine;
    private WaitForSeconds defaultWait;
    private WaitForSeconds fastWait;
    private bool lockedByDeath = false;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        defaultWait = new WaitForSeconds(Mathf.Max(0.0001f, frameRate));
        fastWait = new WaitForSeconds(Mathf.Max(0.0001f, attackFrameRate));

        if (autoPlayEntry && entrySprites != null && entrySprites.Count > 0)
            PlayAnimation(State.Entry);
        else
            PlayAnimation(State.Idle);
    }

    // ===== 코루틴 시작 =====
    private void StartAnimation(List<Sprite> sprites)
    {
        if (animationRoutine != null) StopCoroutine(animationRoutine);
        animationRoutine = StartCoroutine(AnimateRoutine(sprites));
    }

    // ===== 메인 애니 루프 =====
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

        bool isLooping =
            currentState == State.Idle ||
            currentState == State.Move ||
            currentState == State.MoveSide ||
            currentState == State.MoveFront ||
            currentState == State.MoveBack ||
            currentState == State.Attack;

        bool isFast =
            currentState == State.AttackStart ||
            currentState == State.AttackEnd ||
            currentState == State.FrontAttackEnd ||
            currentState == State.Entry ||
            currentState == State.PatternStart ||
            currentState == State.Death ||
            currentState == State.Attack; // 공격 유지 빠르게

        var wait = isFast ? fastWait : defaultWait;

        while (isLooping || currentFrame < currentSprites.Count)
        {
            if (currentSprites == null || currentSprites.Count == 0) break;
            if (currentFrame >= currentSprites.Count) break;

            spriteRenderer.sprite = currentSprites[currentFrame];
            yield return wait;

            currentFrame++;
            if (isLooping) currentFrame %= currentSprites.Count;
        }

        // Death는 잠금 유지
        if (currentState == State.Death)
        {
            animationRoutine = null;
            yield break;
        }

        // ✅ AttackStart가 끝나면 Attack 루프로 자동 진입
        if (currentState == State.AttackStart)
        {
            if (attackSprites != null && attackSprites.Count > 0)
            {
                currentState = State.Attack;
                StartAnimation(attackSprites); // 루프
                animationRoutine = null;
                yield break;
            }
            else
            {
                PlayAnimation(State.Idle);
                animationRoutine = null;
                yield break;
            }
        }

        // 비루프 종료 후 Idle 복귀 (Entry/PatternStart/스킬 종료/공격 End 포함)
        if (currentState == State.AttackEnd || currentState == State.FrontAttackEnd ||
            currentState == State.Skill1Fireball || currentState == State.Skill2Circle || currentState == State.Skill3Dash ||
            currentState == State.Entry || currentState == State.PatternStart)
        {
            PlayAnimation(State.Idle);
        }

        animationRoutine = null;
    }

    // ===== 외부 API =====
    public void PlayAnimation(State newState)
    {
        if (lockedByDeath) return;

        // === 보스 모드일 때만 패턴 상태를 Idle로 우회 ===
        if (bossPatternIdleMode)
        {
            if (newState == State.PatternLoop || newState == State.PatternEnd ||
                newState == State.Skill1Fireball || newState == State.Skill2Circle || newState == State.Skill3Dash)
            {
                if (currentState != State.Idle) { currentState = State.Idle; StartAnimation(idleSprites); }
                return;
            }
            // PatternStart는 원샷 연출 허용(아래 switch에서 처리)
        }

        // ✅ 행동/스킬 중 보호 (AttackStart→Attack 허용)
        bool inAction = (currentState == State.AttackStart || currentState == State.Attack);
        bool inSkill = (currentState == State.Skill1Fireball || currentState == State.Skill2Circle || currentState == State.Skill3Dash);
        if (inAction || inSkill)
        {
            bool allow =
                (currentState == State.AttackStart && (newState == State.Attack || newState == State.AttackEnd || newState == State.FrontAttackEnd)) ||
                (currentState == State.Attack && (newState == State.AttackEnd || newState == State.FrontAttackEnd)) ||
                (inSkill && (newState == State.Idle || newState == State.Move));

            if (!allow) return;
        }

        bool finishing = (currentState == State.AttackEnd || currentState == State.FrontAttackEnd);
        if (finishing && newState != State.Idle && newState != State.Move)
        {
            if (newState != currentState) return;
        }

        if (newState == State.MoveSide || newState == State.MoveFront || newState == State.MoveBack)
        {
            Debug.LogError("방향별 이동은 PlayDirectionalMoveAnimation(Vector2)을 사용하세요.");
            return;
        }

        // ✅ Attack 요청 처리 (Idle/Move에서 오면 AttackStart부터)
        if (newState == State.Attack)
        {
            if (currentState == State.AttackStart)
            {
                // AttackStart 중 Attack 요청 → 허용
            }
            else if (currentState != State.Attack && currentState != State.AttackEnd && currentState != State.FrontAttackEnd)
            {
                newState = State.AttackStart;
            }
            else if (finishing) // End 단계면 재공격 무시
            {
                return;
            }
        }

        if (newState == currentState) return;

        State prev = currentState;
        currentState = newState;

        bool facingVertical = (prev == State.MoveFront || prev == State.MoveBack);
        List<Sprite> target;

        switch (currentState)
        {
            case State.Idle: target = idleSprites; break;
            case State.Move: target = moveSprites; break;

            case State.AttackStart:
                target = (facingVertical && attackStartFrontSprites != null && attackStartFrontSprites.Count > 0)
                    ? attackStartFrontSprites : attackStartSprites;
                break;

            case State.Attack: target = attackSprites; break;
            case State.AttackEnd: target = attackEndSprites; break;
            case State.FrontAttackEnd: target = attackEndFrontSprites; break;

            // 연출/보스 전용
            case State.Entry: target = entrySprites; break;
            case State.PatternStart: target = patternStartSprites; break;
            case State.PatternLoop: target = idleSprites; break;   // 방어적 폴백
            case State.PatternEnd: target = idleSprites; break;   // 방어적 폴백
            case State.Death:
                lockedByDeath = true;
                target = deathSprites; break;

            // 스킬(기존)
            case State.Skill1Fireball: target = skill1FireballSprites; break;
            case State.Skill2Circle: target = skill2CircleSprites; break;
            case State.Skill3Dash: target = skill3DashSprites; break;

            default: target = idleSprites; break;
        }

        if (target != null && target.Count > 0)
        {
            StartAnimation(target);
        }
        else if (currentState != State.Idle)
        {
            Debug.LogWarning($"EnemyAnimation: {currentState} 스프라이트가 없어 Idle로 대체합니다.");
            PlayAnimation(State.Idle);
        }
    }

    /// <summary> 이동 벡터로 MoveSide/Front/Back 전환 (공격/스킬/죽음 중 무시) </summary>
    public void PlayDirectionalMoveAnimation(Vector2 moveDirection)
    {
        if (lockedByDeath) return;

        if (currentState == State.AttackStart || currentState == State.Attack || currentState == State.AttackEnd || currentState == State.FrontAttackEnd ||
            currentState == State.Skill1Fireball || currentState == State.Skill2Circle || currentState == State.Skill3Dash ||
            currentState == State.Death)
            return;

        if (moveDirection.magnitude < 0.01f)
        {
            PlayAnimation(State.Idle);
            return;
        }

        State targetState;
        List<Sprite> targetSprites;

        if (Mathf.Abs(moveDirection.y) > verticalThreshold * Mathf.Abs(moveDirection.x))
        {
            targetState = (moveDirection.y > 0) ? State.MoveBack : State.MoveFront;
            targetSprites = (moveDirection.y > 0) ? moveBackSprites : moveFrontSprites;
        }
        else
        {
            targetState = State.MoveSide;
            targetSprites = (moveSideSprites != null && moveSideSprites.Count > 0) ? moveSideSprites : moveSprites;
        }

        if (targetState == currentState) return;

        currentState = targetState;

        if (targetSprites != null && targetSprites.Count > 0)
            StartAnimation(targetSprites);
        else
        {
            Debug.LogWarning($"EnemyAnimation: {targetState} 스프라이트 없음 → Idle");
            PlayAnimation(State.Idle);
        }
    }

    public State GetCurrentState() => currentState;

    // ===== 보스/연출 헬퍼 =====
    public void PlayPatternStartOnce() => PlayAnimation(State.PatternStart);
    public void PlayEntryOnce() => PlayAnimation(State.Entry);
    public void PlayDeathLock() => PlayAnimation(State.Death);

    /// <summary> 상태 길이(초) 근사값 </summary>
    public float GetEstimatedDuration(State st)
    {
        List<Sprite> list = null;
        float per = frameRate;

        switch (st)
        {
            case State.Entry: list = entrySprites; per = attackFrameRate; break;
            case State.PatternStart: list = patternStartSprites; per = attackFrameRate; break;
            case State.Death: list = deathSprites; per = attackFrameRate; break;

            case State.AttackStart:
                list = (attackStartFrontSprites != null && attackStartFrontSprites.Count > 0)
                    ? attackStartFrontSprites : attackStartSprites;
                per = attackFrameRate; break;

            case State.Attack: list = attackSprites; per = attackFrameRate; break;
            case State.AttackEnd: list = attackEndSprites; per = attackFrameRate; break;
            case State.FrontAttackEnd: list = attackEndFrontSprites; per = attackFrameRate; break;

            case State.Idle: list = idleSprites; per = frameRate; break;
            case State.Move:
            case State.MoveSide: list = (moveSideSprites != null && moveSideSprites.Count > 0) ? moveSideSprites : moveSprites; per = frameRate; break;
            case State.MoveFront: list = (moveFrontSprites != null && moveFrontSprites.Count > 0) ? moveFrontSprites : moveSprites; per = frameRate; break;
            case State.MoveBack: list = (moveBackSprites != null && moveBackSprites.Count > 0) ? moveBackSprites : moveSprites; per = frameRate; break;

            case State.Skill1Fireball: list = skill1FireballSprites; per = frameRate; break;
            case State.Skill2Circle: list = skill2CircleSprites; per = frameRate; break;
            case State.Skill3Dash: list = skill3DashSprites; per = frameRate; break;

            default: list = idleSprites; per = frameRate; break;
        }

        if (list == null || list.Count == 0) return 0f;
        return list.Count * Mathf.Max(0.0001f, per);
    }
}
