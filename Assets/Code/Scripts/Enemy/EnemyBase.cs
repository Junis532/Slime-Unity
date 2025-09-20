using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    public float speed;
    public float originalSpeed;

    // 기본 SetSpeed는 변수만 바꿈. 실제 속도 변경은 서브클래스에서 구현
    public virtual void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
        // 여기서는 실제 이동속도 갱신 없음(서브클래스에서 처리)
    }
}
