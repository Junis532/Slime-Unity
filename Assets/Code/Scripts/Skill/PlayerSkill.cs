//using UnityEngine;

//public class PlayerSkill : MonoBehaviour
//{
//    public GameObject fireballPrefab;    // 화염구 프리팹
//    public Transform firePoint;          // 플레이어 앞쪽 발사 지점
//    public FloatingJoystick joystick;    // 너가 쓰는 조이스틱 컴포넌트 연결

//    private void ShootFireball()
//    {
//        if (fireballPrefab == null || firePoint == null)
//        {
//            Debug.LogWarning("Fireball prefab or firePoint not assigned.");
//            return;
//        }

//        GameObject fireballObj = Instantiate(fireballPrefab, firePoint.position, Quaternion.identity);

//        Vector2 shootDir = lastInputDirection;
//        float angle = Mathf.Atan2(shootDir.y, shootDir.x) * Mathf.Rad2Deg;
//        fireballObj.transform.rotation = Quaternion.Euler(0f, 0f, angle);

//        FireballProjectile fireball = fireballObj.GetComponent<FireballProjectile>();
//        if (fireball != null)
//        {
//            fireball.Init(shootDir);
//        }
//    }

//    private void TeleportPlayer(Vector3 targetPos)
//    {
//        transform.position = targetPos;
//        Debug.Log($"플레이어 텔레포트: {targetPos}");
//    }
//}
