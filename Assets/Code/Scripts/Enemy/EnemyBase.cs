using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    [Header("이동 속도")]
    public float speed = 3f;
    public float originalSpeed = 3f;

    [Header("스폰 딜레이")]
    public float spawnDelay = 0.4f;

    protected bool canMove = true;

    public bool CanMove
    {
        get => canMove;
        set => canMove = value;
    }

    public virtual void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
        originalSpeed = newSpeed;
    }

    public virtual void StopMovement()
    {
        canMove = false;
    }

    public virtual void ResumeMovement()
    {
        canMove = true;
    }
}
