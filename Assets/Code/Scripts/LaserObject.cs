using UnityEngine;
using System.Collections;

[RequireComponent(typeof(LineRenderer))]
public class LaserObject : MonoBehaviour
{
    private LineRenderer lineRenderer;

    [Header("Laser Settings")]
    public float laserLength = 20f;
    [Range(0f, 360f)] public float angle = 0f;
    public int laserDamage = 100;
    public LayerMask raycastMask = ~0;

    [Header("Laser Material")]
    public Material laserMaterial;
    public float scrollSpeed = 2f;

    [Header("Laser Timer (자체 온/오프 순환)")]
    public bool useTimer = false;
    public float activeTime = 2f;
    public float inactiveTime = 1f;

    [Header("Activation")]
    public bool startActive = false;
    public bool selfActivateOnTrigger = false;
    public string triggerTag = "Player";
    public float activateDelay = 3.5f;

    [Header("Warning Settings")]
    public bool useWarning = true;                  // 🔹 경고 기능 켜기/끄기
    public GameObject warningPrefab;                // 🔹 경고 프리팹
    public float warningDuration = 1f;              // 🔹 경고 유지 시간
    public Vector3 warningOffset = Vector3.zero;    // 🔹 경고 위치 오프셋
    public float warningScale = 1.5f;               // 🔹 경고 크기

    [Header("Damage Throttle")]
    public float damageCooldown = 0.15f;
    private float lastDamageTime = -999f;

    private float timer = 0f;
    private bool isActive = false;
    private bool isManuallyActivated = false;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;

        if (laserMaterial != null)
            lineRenderer.material = laserMaterial;

        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.sortingOrder = 10;

        if (startActive)
        {
            Activate();
        }
        else
        {
            isActive = false;
            lineRenderer.enabled = false;
        }
    }

    void Update()
    {
        if (!isManuallyActivated && !startActive)
        {
            lineRenderer.enabled = false;
            return;
        }

        if (useTimer)
        {
            timer += Time.deltaTime;

            if (isActive && timer >= activeTime)
            {
                // 활성 상태가 끝남 → 꺼짐
                isActive = false;
                timer = 0f;
                lineRenderer.enabled = false;
            }
            else if (!isActive && timer >= inactiveTime)
            {
                // 🔹 여기서 바로 켜지지 말고, 경고부터 보여주기
                timer = 0f;

                if (useWarning && warningPrefab != null)
                {
                    StartCoroutine(WarningThenActivate());
                }
                else
                {
                    StartLaserImmediately();
                }
            }
        }
        else
        {
            // useTimer 안 쓸 때는 고정 활성
            if (isActive)
            {
                lineRenderer.enabled = true;
            }
        }

        if (!isActive) return;

        FireLaser();

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

        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, dir, laserLength, raycastMask);
        foreach (var hit in hits)
        {
            if (!hit.collider || hit.collider.gameObject == gameObject) continue;

            if (hit.collider.CompareTag("LaserNot"))
            {
                endDist = hit.distance;
                break;
            }

            if (hit.collider.CompareTag("Player"))
            {
                if (Time.time - lastDamageTime >= damageCooldown)
                {
                    lastDamageTime = Time.time;
                    GameManager.Instance.playerDamaged.TakeDamage(laserDamage, transform.position);
                }
            }
        }

        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, transform.position + (Vector3)dir * endDist);
    }

    // ===== 외부 제어 API =====
    public void Activate()
    {
        if (useWarning && warningPrefab != null)
        {
            StartCoroutine(WarningThenActivate());
        }
        else
        {
            StartLaserImmediately();
        }
    }

    private IEnumerator WarningThenActivate()
    {
        isManuallyActivated = true;

        // 경고 생성
        GameObject warning = Instantiate(
            warningPrefab,
            transform.position + warningOffset,
            Quaternion.Euler(0, 0, angle)
        );
        warning.transform.localScale *= warningScale;

        yield return new WaitForSeconds(warningDuration);

        if (warning) Destroy(warning);

        StartLaserImmediately();
    }

    private void StartLaserImmediately()
    {
        isManuallyActivated = true;
        isActive = true;
        timer = 0f;
        lineRenderer.enabled = true;
    }

    public void Deactivate()
    {
        isManuallyActivated = false;
        isActive = false;
        timer = 0f;
        lineRenderer.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!selfActivateOnTrigger) return;
        if (!other.CompareTag(triggerTag)) return;

        StartCoroutine(ActivateAfterDelay(activateDelay));
    }

    private IEnumerator ActivateAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Activate();
    }
}
