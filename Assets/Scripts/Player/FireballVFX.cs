//using UnityEngine;

//public class FireVFX : MonoBehaviour
//{
//    public ParticleSystem fireVFX;
//    private Vector3 lastPosition;

//    void Start()
//    {
//        lastPosition = transform.position;
//    }

//    void LateUpdate()
//    {
//        // 부모 이동 방향 계산
//        Vector3 moveDir = transform.position - lastPosition;
//        lastPosition = transform.position;

//        if (moveDir.sqrMagnitude > 0.001f)
//        {
//            // 불길을 이동 방향의 반대로 회전
//            fireVFX.transform.rotation = Quaternion.LookRotation(-moveDir.normalized, Vector3.up);

//            // Particle 위치를 부모 위치에 맞춤
//            fireVFX.transform.position = transform.position;

//            // 재생
//            if (!fireVFX.isPlaying)
//                fireVFX.Play();
//        }
//    }
//}
