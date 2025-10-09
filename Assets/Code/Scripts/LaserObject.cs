using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LaserObject : MonoBehaviour
{
    private LineRenderer lineRenderer;

    [Header("Laser Settings")]
    public float laserLength = 20f;                 // 레이저 최대 길이
    [Range(0f, 360f)] public float angle = 0f;      // 레이저 각도
    public int laserDamage = 100;                   // 레이저 데미지
    public LayerMask raycastMask = ~0;              // 레이캐스트 대상(필요 시 레이어 제한)

    [Header("Laser Material")]
    public Material laserMaterial;                  // 적용할 머티리얼
    public float scrollSpeed = 2f;                  // 텍스처 흐름 속도

    [Header("Laser Timer (자체 온/오프 순환)")]
    public bool useTimer = false;                   // 온/오프 순환 사용
    public float activeTime = 2f;                   // 켜진 시간
    public float inactiveTime = 1f;                 // 꺼진 시간

    [Header("Activation")]
    public bool startActive = false;                // 시작 시 켜둘지
    public bool selfActivateOnTrigger = false;      // 이 오브젝트의 트리거로 직접 활성화할지
    public string triggerTag = "Player";            // 트리거로 인식할 태그
    public float activateDelay = 3.5f;              // 트리거 후 지연

    [Header("Damage Throttle")]
    public float damageCooldown = 0.15f;            // 같은 대상 연타 방지(초)
    private float lastDamageTime = -999f;

    private float timer = 0f;
    private bool isActive = false;                  // 실제 레이저 켜짐 상태
    private bool isManuallyActivated = false;       // 외부/트리거로 한 번이라도 켜진 적이 있는가

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;

        if (laserMaterial != null)
            lineRenderer.material = laserMaterial;

        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.sortingOrder = 10;

        // 시작 상태
        if (startActive)
        {
            Activate();
        }
        else
        {
            isActive = false;
            isManuallyActivated = false;
            lineRenderer.enabled = false;
        }
    }

    void Update()
    {
        // 아직 수동 활성화(트리거나 외부 호출)가 안 됐고 startActive도 아니라면 아무 것도 하지 않음
        if (!isManuallyActivated && !startActive)
        {
            lineRenderer.enabled = false;
            return;
        }

        // 타이머 순환
        if (useTimer)
        {
            timer += Time.deltaTime;
            if (isActive && timer >= activeTime)
            {
                isActive = false;
                timer = 0f;
                lineRenderer.enabled = false;
            }
            else if (!isActive && timer >= inactiveTime)
            {
                isActive = true;
                timer = 0f;
                lineRenderer.enabled = true;
            }
        }
        else
        {
            // 타이머 미사용이면 항상 켜두기
            isActive = true;
            lineRenderer.enabled = true;
        }

        if (!isActive) return;

        FireLaser();

        // UV 스크롤
        if (lineRenderer.material != null)
        {
            lineRenderer.material.mainTextureOffset = new Vector2(Time.time * scrollSpeed, 0f);
        }
    }

    void FireLaser()
    {
        Vector2 dir = Quaternion.Euler(0, 0, angle) * Vector2.right;
        Vector2 origin = transform.position;

        float endDist = laserLength;
        bool blocked = false;

        // 가장 가까운 막힘(LaserNot 등)을 기준으로 끝점 결정
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, dir, laserLength, raycastMask);
        foreach (var hit in hits)
        {
            if (!hit.collider) continue;
            if (hit.collider.gameObject == gameObject) continue; // 자기 자신 무시(필요 시)

            // 막는 오브젝트: LaserNot
            if (hit.collider.CompareTag("LaserNot"))
            {
                endDist = hit.distance;
                blocked = true;
                break; // 더 멀리는 볼 필요 없음
            }

            // 플레이어 피격 처리(끝점은 유지해서 관통형처럼 보임)
            if (hit.collider.CompareTag("Player"))
            {
                var indicator = hit.collider.GetComponent<JoystickDirectionIndicator>();
                if (indicator == null || !indicator.IsUsingSkill)
                {
                    if (Time.time - lastDamageTime >= damageCooldown)
                    {
                        lastDamageTime = Time.time;
                        Vector3 enemyPos = transform.position;
                        GameManager.Instance.playerDamaged.TakeDamage(laserDamage, enemyPos);
                    }
                }
                // 만약 플레이어에서 레이저를 끊고 싶다면 아래 두 줄 활성화:
                // endDist = Mathf.Min(endDist, hit.distance);
                // blocked = true;
            }
        }

        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + (Vector3)dir * endDist;

        lineRenderer.SetPosition(0, startPos);
        lineRenderer.SetPosition(1, endPos);
    }

    // ===== 외부 제어 API =====
    public void Activate()
    {
        isManuallyActivated = true;
        timer = 0f;
        isActive = !useTimer || true;    // 타이머 미사용이면 바로 켜짐
        lineRenderer.enabled = !useTimer || true;
    }

    public void ActivateAfterDelay(float delay)
    {
        StartCoroutine(ActivateRoutine(delay));
    }

    private System.Collections.IEnumerator ActivateRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        Activate();
    }

    public void Deactivate()
    {
        isManuallyActivated = false;
        isActive = false;
        timer = 0f;
        lineRenderer.enabled = false;
    }

    // 이 스크립트가 달린 오브젝트가 직접 트리거를 받을 때 사용
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!selfActivateOnTrigger) return;
        if (!other.CompareTag(triggerTag)) return;

        // 트리거 후 3.5초(activateDelay) 뒤 켜기
        ActivateAfterDelay(activateDelay);
    }
}
