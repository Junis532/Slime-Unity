using UnityEngine;
using System.Collections.Generic;
using System.Collections;

// EnemyAnimation을 TurretEnemyAnimation으로 가정하고 사용합니다.
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
        // 🟢 NEW: 보스 스킬 상태 추가
        Skill1Fireball,
        Skill2Circle,
        Skill3Dash
    };

    [Header("스프라이트 리스트 (기본/측면)")]
    public List<Sprite> idleSprites;

    // 🟢 NEW: 정면(아래쪽)을 바라보는 Idle 스프라이트 추가
    public List<Sprite> idleFrontSprites;
    public List<Sprite> moveSprites;       // 일반 이동 애니메이션 (MoveSide 대체용)

    // 기본 공격 3단계 애니메이션 스프라이트 리스트 (측면을 기준으로 사용)
    public List<Sprite> attackStartSprites; // 공격 준비 (비반복)
    public List<Sprite> attackSprites;      // 공격 유지/반복 (반복)
    public List<Sprite> attackEndSprites;   // 공격 마무리 (비반복)

    // NEW: 정면 공격 3단계 애니메이션 스프라이트 리스트
    [Header("정면 공격 애니메이션 (Front)")]
    [Tooltip("정면(아래쪽)을 바라보는 공격 준비 애니메이션")]
    public List<Sprite> attackStartFrontSprites;
    [Tooltip("정면(아래쪽)을 바라보는 공격 마무리 애니메이션")]
    public List<Sprite> attackEndFrontSprites;

    // 방향별 이동 애니메이션
    public List<Sprite> moveSideSprites;    // 측면 이동 (좌/우)
    public List<Sprite> moveFrontSprites;   // 앞쪽 이동 (아래)
    public List<Sprite> moveBackSprites;    // 뒤쪽 이동 (위)

    // 🟢 NEW: 스킬 애니메이션 스프라이트 리스트
    [Header("보스 스킬 애니메이션")]
    public List<Sprite> skill1FireballSprites;
    public List<Sprite> skill2CircleSprites;
    public List<Sprite> skill3DashSprites;

    [Header("설정")]
    public float frameRate = 0.1f;
    [Tooltip("Attack 관련 애니메이션 전용 프레임 간격 (초). Start/Attack/End 모두 적용됩니다.")]
    public float attackFrameRate = 0.05f;
    [Tooltip("수직/수평 애니메이션 전환 임계값 (0에 가까울수록 민감)")]
    public float verticalThreshold = 0.5f;

    private SpriteRenderer spriteRenderer;
    private int currentFrame;
    public State currentState;
    private List<Sprite> currentSprites;

    private Coroutine animationRoutine;

    // 💡 최적화: WaitForSeconds 객체를 미리 생성하여 GC를 줄입니다.
    private WaitForSeconds defaultWait;
    private WaitForSeconds attackWait;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        // 💡 최적화: WaitForSeconds 객체를 Start 시에 한 번만 생성합니다.
        defaultWait = new WaitForSeconds(frameRate);
        attackWait = new WaitForSeconds(attackFrameRate);

        if (spriteRenderer != null)
        {
            PlayAnimation(State.Idle);
        }
    }

    void Update() { }

    private void StartAnimation(List<Sprite> sprites)
    {
        // 💡 중첩 방지 핵심: 새로운 애니메이션 시작 시 기존 코루틴을 중단합니다.
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

        // 🟢 NEW: 스킬 1, 2, 3은 루프하지 않습니다.
        bool isLooping = (currentState == State.Idle ||
                             currentState == State.Move ||
                             currentState == State.MoveSide ||
                             currentState == State.MoveFront ||
                             currentState == State.MoveBack ||
                             currentState == State.Attack);

        // 💡 최적화: 사용할 WaitForSeconds 객체 선택
        // 🟢 NEW: 스킬 1, 2, 3은 빠른 애니메이션으로 취급하지 않습니다. (frameRate 적용)
        bool isFastAnimation = (currentState == State.AttackStart ||
                                 currentState == State.Attack ||
                                 currentState == State.AttackEnd ||
                                 currentState == State.FrontAttackEnd);

        WaitForSeconds wait = isFastAnimation ? attackWait : defaultWait;

        while (isLooping || currentFrame < currentSprites.Count)
        {
            // 스프라이트가 null일 경우, 프레임 계산 중 SpriteRenderer의 설정이 변경된 것이므로 종료합니다.
            if (currentSprites == null || currentSprites.Count == 0 || currentFrame >= currentSprites.Count) break;

            spriteRenderer.sprite = currentSprites[currentFrame];

            yield return wait;

            currentFrame++;

            if (isLooping)
            {
                currentFrame %= currentSprites.Count;
            }
        }

        // 🟢 NEW: 비반복 애니메이션 (공격, 스킬) 종료 후, 상태 복귀
        if (currentState == State.AttackEnd || currentState == State.FrontAttackEnd ||
            currentState == State.Skill1Fireball || currentState == State.Skill2Circle || currentState == State.Skill3Dash)
        {
            PlayAnimation(State.Idle);
        }

        animationRoutine = null;
    }

    /// <summary>
    /// Idle, Move, Attack 관련 상태로 전환합니다.
    /// 외부에서 Attack을 호출하면 AttackStart부터 시작합니다.
    /// </summary>
    /// <param name="newState">요청된 상태. Attack이 요청되면 AttackStart로 시작합니다.</param>
    public void PlayAnimation(State newState)
    {
        // 1. 공격 중 보호 로직 강화
        bool isCurrentlyInAction = (currentState == State.AttackStart || currentState == State.Attack);

        // 🟢 NEW: 스킬 상태일 때 다른 전환을 막습니다. (스킬 코루틴 내부에서만 상태 전환을 허용)
        bool isCurrentlyInSkill = (currentState == State.Skill1Fireball || currentState == State.Skill2Circle || currentState == State.Skill3Dash);

        if (isCurrentlyInAction || isCurrentlyInSkill)
        {
            bool isTransitionAllowed = (
                // 공격 3단계 진행 허용 (Start -> AttackEnd/FrontAttackEnd)
                (currentState == State.AttackStart && (newState == State.AttackEnd || newState == State.FrontAttackEnd)) ||
                // 스킬 종료 시 Idle/Move로의 복귀 허용
                (isCurrentlyInSkill && (newState == State.Idle || newState == State.Move))
            );

            // 허용되지 않은 전환이라면 무시
            if (!isTransitionAllowed)
            {
                // 현재 상태가 스킬이라면, 해당 스킬 상태로의 요청도 무시
                if (newState == currentState) return;

                return;
            }
        }

        // AttackEnd/FrontAttackEnd 상태일 때 Idle/Move 복귀는 허용해야 합니다.
        bool isFinishingAction = (currentState == State.AttackEnd || currentState == State.FrontAttackEnd);
        if (isFinishingAction && newState != State.Idle && newState != State.Move)
        {
            // 종료 단계일 때 다른 공격 명령이나 이동 명령을 무시합니다.
            if (newState != currentState) return;
        }

        // 2. 방향별 Move 상태로의 직접적인 전환 요청 방지 
        if (newState == State.MoveSide || newState == State.MoveFront || newState == State.MoveBack)
        {
            Debug.LogError("방향별 애니메이션은 PlayDirectionalMoveAnimation(Vector2)을 사용하세요.");
            return;
        }

        // 3. 외부에서 State.Attack을 호출하면 항상 State.AttackStart부터 시작합니다.
        if (newState == State.Attack)
        {
            if (currentState != State.AttackEnd && currentState != State.FrontAttackEnd)
            {
                newState = State.AttackStart;
            }
            else if (isFinishingAction && newState == State.Attack)
            {
                // End 상태인데 다시 Attack을 요청하면, Idle로 돌아간 후 다시 공격해야 하므로 요청 무시
                return;
            }
        }

        // 4. 같은 상태로 다시 전환하려고 하면 무시 
        if (newState == currentState) return;

        // 상태 갱신
        State previousState = currentState;
        currentState = newState;

        List<Sprite> targetSprites;

        // AttackStart/AttackEnd에서 방향을 확인할 때 사용. 
        bool isFacingVertical = (previousState == State.MoveFront || previousState == State.MoveBack);

        switch (currentState)
        {
            case State.Idle:
                targetSprites = idleSprites;
                break;
            case State.Move:
                targetSprites = moveSprites;
                break;

            case State.AttackStart:
                targetSprites = (isFacingVertical && attackStartFrontSprites != null && attackStartFrontSprites.Count > 0)
                                    ? attackStartFrontSprites
                                    : attackStartSprites;
                break;

            case State.Attack: // Loop Attack
                targetSprites = attackSprites;
                break;

            case State.AttackEnd:
                targetSprites = attackEndSprites;
                break;

            case State.FrontAttackEnd:
                targetSprites = attackEndFrontSprites;
                break;

            // 🟢 NEW: 스킬 상태 처리
            case State.Skill1Fireball:
                targetSprites = skill1FireballSprites;
                break;
            case State.Skill2Circle:
                targetSprites = skill2CircleSprites;
                break;
            case State.Skill3Dash:
                targetSprites = skill3DashSprites;
                break;

            default:
                targetSprites = idleSprites;
                break;
        }

        if (targetSprites != null && targetSprites.Count > 0)
        {
            StartAnimation(targetSprites);
        }
        else if (currentState != State.Idle)
        {
            Debug.LogWarning($"Enemy Animation: {currentState}에 할당된 스프라이트가 없습니다. Idle로 전환을 시도합니다.");

            // 스킬 애니메이션이 없는 경우, 즉시 Idle로 전환
            PlayAnimation(State.Idle);
        }
    }

    /// <summary>
    /// 고정된 각도를 기반으로 Idle 애니메이션의 방향(측면/정면)을 결정합니다.
    /// TurretEnemy_FixedAngle에서 사용됩니다.
    /// </summary>
    /// <param name="newState">요청된 상태 (Idle이어야 함)</param>
    /// <param name="angle">포탑의 고정 발사 각도 (도 단위)</param>
    public void PlayAnimation(State newState, float angle)
    {
        // Idle 상태가 아니거나, 현재 공격/스킬 중이면 무시
        bool isCurrentlyInAction = (currentState == State.AttackStart || currentState == State.Attack ||
                                    currentState == State.AttackEnd || currentState == State.FrontAttackEnd ||
                                    currentState == State.Skill1Fireball || currentState == State.Skill2Circle ||
                                    currentState == State.Skill3Dash);

        if (newState != State.Idle || isCurrentlyInAction)
        {
            if (newState != State.Idle && newState != currentState)
            {
                PlayAnimation(newState); // 다른 상태라면 기본 로직으로 처리
            }
            return;
        }

        // 1. 각도 정규화 (0도 ~ 360도)
        angle = (angle % 360 + 360) % 360;

        // 2. 수직 방향(90도/270도) 판단.
        bool useFrontSprite = false;
        float verticalTolerance = 25f; // 수직으로 판단하는 각도 허용 오차

        // 90도(위) 또는 270도(아래) 부근일 때 useFrontSprite = true
        if (angle >= 90f - verticalTolerance && angle <= 90f + verticalTolerance)
        {
            useFrontSprite = true;
        }
        else if (angle >= 270f - verticalTolerance && angle <= 270f + verticalTolerance)
        {
            useFrontSprite = true;
        }

        List<Sprite> targetSprites;
        if (useFrontSprite && idleFrontSprites != null && idleFrontSprites.Count > 0)
        {
            targetSprites = idleFrontSprites;
            // 정면 스프라이트를 사용할 때는 좌우 반전(flipX)을 하지 않습니다.
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = false;
            }
        }
        else
        {
            targetSprites = idleSprites;

            // 3. 측면(0도/180도)일 경우 좌우 반전 처리
            if (spriteRenderer != null && targetSprites == idleSprites)
            {
                // 180도 부근 (왼쪽): 반전 O
                // Note: 0도/360도 부근은 수평 방향이므로 flipX=false (오른쪽을 바라봄)
                if (angle >= 180f - verticalTolerance && angle <= 180f + verticalTolerance)
                {
                    spriteRenderer.flipX = true;
                }
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
            else
            {
                // Idle 스프라이트가 없는 경우
                if (currentSprites != idleSprites)
                {
                    StartAnimation(idleSprites);
                }
            }
        }
    }


    /// <summary>
    /// 이동 방향 벡터를 기반으로 MoveSide, MoveFront, MoveBack 중 하나로 전환합니다.
    /// </summary>
    /// <param name="moveDirection">몬스터의 이동 방향 벡터</param>
    public void PlayDirectionalMoveAnimation(Vector2 moveDirection)
    {
        // 💡 핵심: 공격 및 스킬 관련 상태일 때는 이동 애니메이션 요청을 무시하여 중첩 방지
        if (currentState == State.AttackStart || currentState == State.Attack || currentState == State.AttackEnd || currentState == State.FrontAttackEnd ||
            currentState == State.Skill1Fireball || currentState == State.Skill2Circle || currentState == State.Skill3Dash)
            return;

        if (moveDirection.magnitude < 0.01f)
        {
            PlayAnimation(State.Idle);
            return;
        }

        List<Sprite> targetSprites;
        State targetState;

        if (Mathf.Abs(moveDirection.y) > verticalThreshold * Mathf.Abs(moveDirection.x))
        {
            targetState = (moveDirection.y > 0) ? State.MoveBack : State.MoveFront;
            targetSprites = (moveDirection.y > 0) ? moveBackSprites : moveFrontSprites;
        }
        else
        {
            targetState = State.MoveSide;
            // moveSideSprites가 없으면 일반 Move Sprites를 사용하도록 폴백
            targetSprites = (moveSideSprites != null && moveSideSprites.Count > 0) ? moveSideSprites : moveSprites;
        }

        // 💡 핵심: 상태 변경이 없으면 StartAnimation 호출을 막아 이동 애니메이션이 끊기거나 빨라지는 것을 방지합니다.
        if (targetState == currentState) return;

        currentState = targetState;

        if (targetSprites != null && targetSprites.Count > 0)
        {
            StartAnimation(targetSprites);
        }
        else
        {
            Debug.LogWarning($"Enemy Animation: {targetState}에 할당된 스프라이트가 없어 Idle로 대체합니다.");
            PlayAnimation(State.Idle);
        }
    }

    /// <summary>
    /// 현재 재생 중인 애니메이션 상태를 반환합니다.
    /// </summary>
    public State GetCurrentState()
    {
        return currentState;
    }
}