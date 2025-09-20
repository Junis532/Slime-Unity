//using System.Collections;
//using UnityEngine;
//using DG.Tweening; // DOTween을 사용한다면 추가

//public class SpecialDashProjectile : MonoBehaviour
//{
//    private Vector2 initialVelocity;
//    private Vector2 targetPosition;

//    // 이제 DamageArea 대신 PotionEnemyDamage 프리팹을 받습니다.
//    private GameObject potionDamagePrefab;
//    private float potionLifetime; // 포션(독 장판)의 지속 시간

//    private float startTime;

//    void OnEnable()
//    {
//        // 풀에서 재활용될 때 초기화되므로 여기서 startTime을 초기화할 필요는 없습니다.
//    }

//    /// <summary>
//    /// DashProjectile을 초기화합니다.
//    /// </summary>
//    /// <param name="velocity">투사체의 초기 속도 (방향 * 속도).</param>
//    /// <param name="targetPos">투사체가 멈추고 독 장판을 생성할 최종 목표 위치.</param>
//    /// <param name="potionPrefab">생성할 PotionEnemyDamage 프리팹.</param>
//    /// <param name="lifetime">생성될 독 장판의 지속 시간.</param>
//    public void Init(Vector2 velocity, Vector2 targetPos, GameObject potionPrefab, float lifetime)
//    {
//        initialVelocity = velocity;
//        targetPosition = targetPos;
//        potionDamagePrefab = potionPrefab;
//        potionLifetime = lifetime; // 독 장판 지속 시간 설정

//        startTime = Time.time; // 투사체 이동 타이머 시작
//    }

//    void Update()
//    {
//        float totalMoveDistance = Vector2.Distance(transform.position, targetPosition);
//        float totalMoveTime = (initialVelocity.magnitude > 0) ? totalMoveDistance / initialVelocity.magnitude : 0f;

//        float timeElapsed = Time.time - startTime;
//        float fractionOfJourney = (totalMoveTime > 0) ? timeElapsed / totalMoveTime : 1f;

//        if (fractionOfJourney < 1.0f)
//        {
//            transform.position = Vector2.Lerp(transform.position, targetPosition, fractionOfJourney);
//        }
//        else // 목표 지점에 도달했거나 지나쳤을 때
//        {
//            transform.position = targetPosition; // 정확히 목표 지점에 위치
//            SpawnPotionDamageAndDeactivate(); // PotionEnemyDamage 스폰 함수 호출
//        }
//    }

//    private void SpawnPotionDamageAndDeactivate()
//    {
//        if (potionDamagePrefab != null)
//        {
//            // 풀에서 PotionEnemyDamage 오브젝트 스폰
//            GameObject poisonArea = GameManager.Instance.poolManager.SpawnFromPool(
//                potionDamagePrefab.name, targetPosition, Quaternion.identity);

//            if (poisonArea != null)
//            {
//                // PotionBehavior 스크립트를 찾아 Init 메소드를 호출
//                PotionBehavior potionBehavior = poisonArea.GetComponent<PotionBehavior>();
//                if (potionBehavior != null)
//                {
//                    potionBehavior.StartLifetime(potionLifetime); // 독 장판 지속 시간 설정
//                }
//                else
//                {
//                    Debug.LogWarning("스폰된 PotionDamage 오브젝트에 PotionBehavior 스크립트를 찾을 수 없습니다! 독 장판이 사라지지 않을 수 있습니다.");
//                }

//                // PotionEnemyDamage 스크립트 초기화 (데미지 계산을 위함)
//                PotionEnemyDamage potionDamage = poisonArea.GetComponent<PotionEnemyDamage>();
//                if (potionDamage != null)
//                {
//                    potionDamage.Init(); // PotionEnemyDamage의 Init 호출 (playerStats 기반 데미지 계산)
//                }
//                else
//                {
//                    Debug.LogWarning("스폰된 PotionDamage 오브젝트에 PotionEnemyDamage 스크립트를 찾을 수 없습니다! 독 데미지가 제대로 작동하지 않을 수 있습니다.");
//                    GameManager.Instance.poolManager.ReturnToPool(poisonArea); // 스크립트 없으면 바로 풀로 반환
//                }
//            }
//            else
//            {
//                Debug.LogError($"'{potionDamagePrefab.name}' 프리팹을 풀에서 스폰하지 못했습니다. PoolManager에 등록되었는지, 이름이 정확한지 확인하세요.");
//            }
//        }
//        else
//        {
//            Debug.LogWarning("Potion Damage Prefab이 설정되지 않아 독 데미지 영역을 생성할 수 없습니다.");
//        }

//        // 자신을 풀로 반환
//        if (GameManager.Instance != null && GameManager.Instance.poolManager != null)
//        {
//            GameManager.Instance.poolManager.ReturnToPool(this.gameObject);
//        }
//        else
//        {
//            Destroy(this.gameObject);
//        }
//    }
//}