using UnityEngine;

/// <summary>
/// 2D ������Ʈ�� � �ܺ� �Է�/��/��ũ��Ʈ���� �ұ��ϰ� ���� �������� �ʰ� �����մϴ�.
/// - Rigidbody2D�� ������: bodyType�� Static �Ǵ� Kinematic+FreezeAll�� �����ϰ� �� ������ ��ġ/ȸ���� �缳��.
/// - Rigidbody2D�� ������: Transform�� �� ������ ����ġ�� �缳��.
/// - Lock()/Unlock()���� ��Ÿ�� ��� ����.
/// </summary>
[DisallowMultipleComponent]
public class Lockdown2D : MonoBehaviour
{
    public enum LockPhysicsMode
    {
        AutoBest,      // Rigidbody2D ������ Static, ������ Transform ����
        StaticBody,    // �׻� Rigidbody2D�� Static����
        KinematicFreeze, // Rigidbody2D�� Kinematic + FreezeAll (�浹 �̺�Ʈ�� ������ �����ϰ� ���� ��)
        TransformOnly  // Rigidbody �����ϰ� Ʈ�������� �ǵ���(�����, Ư�� ���̽�)
    }

    [Header("��� ����")]
    public bool lockOnStart = true;
    public LockPhysicsMode mode = LockPhysicsMode.AutoBest;
    [Tooltip("�θ�/�ִϸ��̼�/�ٸ� ��ũ��Ʈ�� ��ġ�� �ٲ㵵 ����ġ�� ���� ����")]
    public bool enforceEveryFrame = true;

    private Rigidbody2D rb;
    private RigidbodyType2D originalBodyType;
    private RigidbodyConstraints2D originalConstraints;
    private bool originalSimulated;

    private Vector3 lockedPosition;
    private Quaternion lockedRotation;
    private Vector3 lockedScale;

    private bool isLocked;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        CacheCurrentTransform();
        if (rb)
        {
            originalBodyType = rb.bodyType;
            originalConstraints = rb.constraints;
            originalSimulated = rb.simulated;
        }
    }

    void Start()
    {
        if (lockOnStart) Lock();
    }

    void CacheCurrentTransform()
    {
        lockedPosition = transform.position;
        lockedRotation = transform.rotation;
        lockedScale = transform.localScale;
    }

    void LateUpdate()
    {
        if (!isLocked || !enforceEveryFrame) return;

        // � ��ũ��Ʈ�� �������� ����ġ�� �ǵ�����
        transform.position = lockedPosition;
        transform.rotation = lockedRotation;
        transform.localScale = lockedScale;

        // Rigidbody2D�� ������ �ӵ�/���ӵ� ����
        if (rb)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    void FixedUpdate()
    {
        if (!isLocked) return;

        // ���� ƽ������ Ȯ���� ����
        if (rb)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;

            if (CurrentMode() == LockPhysicsMode.KinematicFreeze)
            {
                // Ȥ�ö� �ܷ����� �����Ǵ� �� ����
                rb.MovePosition(lockedPosition);
                rb.MoveRotation(lockedRotation.eulerAngles.z);
            }
        }
    }

    public void Lock()
    {
        if (isLocked) return;
        isLocked = true;

        // ���� ��ġ/ȸ���� ��� �������� ���
        CacheCurrentTransform();

        var m = CurrentMode();

        if (rb)
        {
            if (m == LockPhysicsMode.AutoBest || m == LockPhysicsMode.StaticBody)
            {
                rb.bodyType = RigidbodyType2D.Static; // ���� Ȯ���ϰ� �� ������(�浹�ε� �� �и�)
            }
            else if (m == LockPhysicsMode.KinematicFreeze)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.constraints = RigidbodyConstraints2D.FreezeAll;
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
            else // TransformOnly
            {
                // ���� ���� ����. �Ʒ� LateUpdate/FixedUpdate���� �ǵ����� ����
            }
        }
        else
        {
            // Rigidbody�� ��� Ʈ������ �ǵ������� ����
        }
    }

    public void Unlock()
    {
        if (!isLocked) return;
        isLocked = false;

        if (rb)
        {
            // ���� ���� ���� ����
            rb.bodyType = originalBodyType;
            rb.constraints = originalConstraints;
            rb.simulated = originalSimulated;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    LockPhysicsMode CurrentMode()
    {
        if (mode != LockPhysicsMode.AutoBest) return mode;
        // Auto: Rigidbody�� ������ Static, ������ TransformOnly
        return rb ? LockPhysicsMode.StaticBody : LockPhysicsMode.TransformOnly;
    }

#if UNITY_EDITOR
    // �����Ϳ��� �� �ٲٸ� ��� ��� ���ص� �����ϰ� ���� �� ȣ��
    [ContextMenu("Reset Lock Pose To Current")]
    void Editor_ResetLockPose()
    {
        CacheCurrentTransform();
        if (!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
