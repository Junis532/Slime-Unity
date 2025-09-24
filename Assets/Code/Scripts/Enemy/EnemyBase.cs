using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    [Header("이동 속도")]
    public float speed = 3f;
    public float originalSpeed = 3f;

    [Header("스폰 딜레이")]
    public float spawnDelay = 0.4f;

    protected bool canMove = true;

    // 외부에서 제어 가능
    public bool CanMove
    {
        get { return canMove; }
        set { canMove = value; }
    }

    public virtual void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
        originalSpeed = newSpeed;
    }
}
