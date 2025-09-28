using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LaserObject : MonoBehaviour
{
    private LineRenderer lineRenderer;

    [Header("Laser Settings")]
    public float laserLength = 20f;      // 레이저 최대 길이
    [Range(0f, 360f)]
    public float angle = 0f;             // 레이저 각도
    public int laserDamage = 100;        // 레이저 데미지

    [Header("Laser Material")]
    public Material laserMaterial;       // 적용할 쉐이더 머티리얼
    public float scrollSpeed = 2f;       // 텍스처 흐르는 속도

    [Header("Laser Timer")]
    public bool useTimer = false;        // 타이머 기능 켜기/끄기
    public float activeTime = 2f;        // 레이저 켜진 시간
    public float inactiveTime = 1f;      // 레이저 꺼진 시간

    private float timer = 0f;
    private bool isActive = true;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;

        if (laserMaterial != null)
            lineRenderer.material = laserMaterial;

        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.sortingOrder = 10;
    }

    void Update()
    {
        // 타이머 체크
        if (useTimer)
        {
            timer += Time.deltaTime;
            if (isActive && timer >= activeTime)
            {
                isActive = false;
                timer = 0f;
                lineRenderer.enabled = false; // 레이저 끄기
            }
            else if (!isActive && timer >= inactiveTime)
            {
                isActive = true;
                timer = 0f;
                lineRenderer.enabled = true; // 레이저 켜기
            }
        }
        else
        {
            lineRenderer.enabled = true; // 타이머 사용 안 하면 항상 켬
            isActive = true;
        }

        // 레이저 켜져 있을 때만 동작
        if (isActive)
        {
            FireLaser();

            // 머티리얼 UV 스크롤
            if (lineRenderer.material != null)
            {
                lineRenderer.material.mainTextureOffset = new Vector2(Time.time * scrollSpeed, 0);
            }
        }
    }

    void FireLaser()
    {
        Vector2 direction = Quaternion.Euler(0, 0, angle) * Vector2.right;
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + (Vector3)direction * laserLength;

        // RaycastAll로 모든 충돌 체크
        RaycastHit2D[] hits = Physics2D.RaycastAll(startPos, direction, laserLength);
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null) continue;

            // LaserNot 만나면 레이저 막힘
            if (hit.collider.CompareTag("LaserNot"))
            {
                endPos = hit.point;
                break;
            }

            // Player 충돌 처리
            if (hit.collider.CompareTag("Player"))
            {
                JoystickDirectionIndicator indicator = hit.collider.GetComponent<JoystickDirectionIndicator>();

                // 1. 스킬 사용 중이 아니거나 indicator가 없는 경우에만 데미지 처리
                if (indicator == null || !indicator.IsUsingSkill)
                {
                    // 2. 넉백 방향 계산을 위해 현재 오브젝트(레이저 발사체/적)의 위치를 전달합니다.
                    Vector3 enemyPosition = transform.position;

                    // 3. PlayerDamaged.TakeDamage(데미지, 적 위치) 형식으로 호출
                    //    (기존의 hit.collider와 hit.point 인자는 제거됩니다.)
                    GameManager.Instance.playerDamaged.TakeDamage(laserDamage, enemyPosition);

                    // 여기에 레이저 파괴 로직 등을 추가할 수 있습니다.
                }
            }
        }

        // 라인렌더러 업데이트
        lineRenderer.SetPosition(0, startPos);
        lineRenderer.SetPosition(1, endPos);
    }
}
