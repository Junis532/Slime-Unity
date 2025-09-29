using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

[RequireComponent(typeof(SpriteRenderer))]
public class TurretEnemyAnimation : MonoBehaviour
{
    public enum State
    {
        Idle,
        MoveSide, MoveFront, MoveBack,
        AttackPrepare,
        AttackShoot
    }

    [Header("🎯 SpriteRenderer (자식 SR 사용 시 지정)")]
    [SerializeField] private SpriteRenderer rendererOverride;

    [Header("📌 기본/이동 스프라이트")]
    public List<Sprite> idleSprites;      // 기본 Idle (앞/뒤 구분 없음)
    public List<Sprite> idleFrontSprites; // 각도 270~90일 때
    public List<Sprite> idleBackSprites;  // 각도 90~270일 때
    public List<Sprite> moveSideSprites;
    public List<Sprite> moveFrontSprites;
    public List<Sprite> moveBackSprites;

    [Header("📌 공격 애니메이션")]
    public List<Sprite> attackPrepareSprites;
    public List<Sprite> attackShootSprites;

    [Header("🎚️ 프레임 시간")]
    public float idleFrameTime = 0.14f;
    public float moveFrameTime = 0.10f;
    public float attackPrepareFrameTime = 0.12f;
    public float attackShootFrameTime = 0.12f;

    [Header("⚡ 배속")]
    [Min(0.05f)] public float globalSpeed = 1.0f;
    [Min(0.05f)] public float idleSpeed = 1.0f;
    [Min(0.05f)] public float moveSpeed = 1.0f;
    [Min(0.05f)] public float attackPrepareSpeed = 1.0f;
    [Min(0.05f)] public float attackShootSpeed = 1.0f;

    [Header("🧭 이동 방향 판정")]
    public float verticalThreshold = 0.5f;

    [Header("🛠 옵션")]
    public bool autoPlayIdle = true;
    public bool logDebug = false;

    private SpriteRenderer spr;
    private Coroutine animCo;
    private List<Sprite> currentSprites;
    private int frameIndex;
    private State currentState = State.Idle;
    private bool isSkillPlaying = false;

    private void Awake()
    {
        spr = rendererOverride != null ? rendererOverride : GetComponent<SpriteRenderer>();
        if (!spr) Debug.LogError("[TurretEnemyAnimation] SpriteRenderer를 지정하세요.");
    }

    private void Start()
    {
        if (autoPlayIdle) PlayAnimation(State.Idle);
    }

    // ================= 공개 API =================

    public void PlayAnimation(State newState, float angle = -1f)
    {
        if (logDebug) Debug.Log($"[TurretEnemyAnimation] 요청: {newState} (현재:{currentState}, skill:{isSkillPlaying})");

        if (isSkillPlaying && newState != State.Idle && !IsAttackState(newState))
            return;

        if (newState == currentState && animCo != null) return;

        // Idle일 때 각도 기반 Front/Back 자동 적용
        List<Sprite> sprites;
        if (newState == State.Idle && angle >= 0f)
        {
            sprites = GetIdleSpritesByAngle(angle);
        }
        else
        {
            sprites = GetSprites(newState);
        }

        if (sprites == null || sprites.Count == 0) return;

        currentState = newState;
        bool isLoop = (newState == State.Idle || newState == State.MoveSide || newState == State.MoveFront || newState == State.MoveBack);
        bool returnToIdleOnEnd = (newState == State.AttackShoot);

        isSkillPlaying = IsAttackState(newState);

        if (animCo != null) StopCoroutine(animCo);
        animCo = StartCoroutine(PlayRoutine(sprites, GetWait(newState), isLoop, returnToIdleOnEnd));
    }

    public void PlayDirectionalMoveAnimation(Vector2 moveDir)
    {
        if (isSkillPlaying) return;

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
            sprites = moveSideSprites;
        }

        if (sprites == null || sprites.Count == 0) return;
        if (targetState == currentState && animCo != null) return;

        currentState = targetState;
        isSkillPlaying = false;
        if (animCo != null) StopCoroutine(animCo);
        animCo = StartCoroutine(PlayRoutine(sprites, GetWait(State.MoveSide), true, false));
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
            yield return new WaitForSeconds(GetWait(currentState));

            frameIndex++;
            if (isLoop) frameIndex %= currentSprites.Count;
            else if (frameIndex >= currentSprites.Count) break;
        }

        if (!isLoop && returnToIdleOnEnd)
        {
            isSkillPlaying = false;
            PlayAnimation(State.Idle);
        }
    }

    private List<Sprite> GetSprites(State s)
    {
        switch (s)
        {
            case State.Idle: return idleSprites;
            case State.MoveSide: return moveSideSprites;
            case State.MoveFront: return moveFrontSprites;
            case State.MoveBack: return moveBackSprites;
            case State.AttackPrepare: return attackPrepareSprites;
            case State.AttackShoot: return attackShootSprites;
        }
        return null;
    }

    private bool IsAttackState(State s) => s == State.AttackPrepare || s == State.AttackShoot;

    private float GetWait(State s)
    {
        float baseTime, mult;
        switch (s)
        {
            case State.Idle: baseTime = idleFrameTime; mult = idleSpeed; break;
            case State.MoveSide:
            case State.MoveFront:
            case State.MoveBack: baseTime = moveFrameTime; mult = moveSpeed; break;
            case State.AttackPrepare: baseTime = attackPrepareFrameTime; mult = attackPrepareSpeed; break;
            case State.AttackShoot: baseTime = attackShootFrameTime; mult = attackShootSpeed; break;
            default: baseTime = 0.12f; mult = 1f; break;
        }
        float speed = Mathf.Max(0.05f, globalSpeed * Mathf.Max(0.05f, mult));
        return Mathf.Max(0.005f, baseTime / speed);
    }

    // ================= 각도 기반 Idle 선택 =================
    private List<Sprite> GetIdleSpritesByAngle(float angle)
    {
        float normalizedAngle = angle % 360f;
        if (normalizedAngle < 0f) normalizedAngle += 360f;

        // 270~90도 → IdleFront, 나머지 → IdleBack
        if (normalizedAngle >= 270f || normalizedAngle <= 90f)
            return idleFrontSprites != null && idleFrontSprites.Count > 0 ? idleFrontSprites : idleSprites;
        else
            return idleBackSprites != null && idleBackSprites.Count > 0 ? idleBackSprites : idleSprites;
    }
}
