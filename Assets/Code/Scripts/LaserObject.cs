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

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;

        if (laserMaterial != null)
            lineRenderer.material = laserMaterial;

        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
    }

    void Update()
    {
        FireLaser();

        // 머티리얼 UV 스크롤
        if (lineRenderer.material != null)
        {
            lineRenderer.material.mainTextureOffset = new Vector2(Time.time * scrollSpeed, 0);
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
                if (indicator == null || !indicator.IsUsingSkill)
                {
                    GameManager.Instance.playerDamaged.TakeDamage(laserDamage);
                }
            }
        }

        // 라인렌더러 업데이트
        lineRenderer.SetPosition(0, startPos);
        lineRenderer.SetPosition(1, endPos);
    }
}
