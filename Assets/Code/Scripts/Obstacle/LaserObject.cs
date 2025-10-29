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

    [Header("Laser Timer")]
    public bool useTimer = false;
    public float activeTime = 2f;
    public float inactiveTime = 1f;

    [Header("Activation")]
    public bool startActive = false;
    public float startActiveDelay = 0f;
    public bool selfActivateOnTrigger = false;
    public string triggerTag = "Player";
    public float activateDelay = 3.5f;

    [Header("Warning Settings")]
    public bool useWarning = true;
    public GameObject warningPrefab;
    public float warningDuration = 1f;
    public Vector3 warningOffset = Vector3.zero;
    public float warningScale = 1.5f;

    [Header("Damage Throttle")]
    public float damageCooldown = 0.15f;
    private float lastDamageTime = -999f;

    private float timer = 0f;
    private bool isActive = false;
    private bool isManuallyActivated = false;

    [Header("Wave Effect Settings")]
    public Color waveColor = new Color(1f, 0.3f, 0.3f, 0.7f);
    public float waveMaxRadius = 0.5f;
    public float waveDuration = 0.5f;
    public int waveSegments = 32;

    [Header("Start Circle Settings")]
    public GameObject startCirclePrefab;   // 인스펙터에서 원 프리팹 넣기
    public float startCircleSize = 0.3f;   // 원 크기
    [Range(0f, 1f)] public float startCircleAlpha = 1f; // 불투명도

    [Header("Animation Settings")]
    public float startCircleGrowDuration = 0.3f;  // 원 커지는 시간
    public float laserGrowSpeed = 30f;            // 레이저 길이 증가 속도
    public float delayBeforeLaser = 0.5f;         // 🔹 원 커지고 레이저 시작 전 대기 시간 (Inspector에서 수정 가능)


    private GameObject startCircle;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;

        if (laserMaterial != null)
            lineRenderer.material = laserMaterial;

        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.sortingOrder = 12;
        lineRenderer.enabled = false;
    }

    void Start()
    {
        if (startActive)
            Activate();
        else if (startActiveDelay > 0f)
            StartCoroutine(ActivateAfterDelay(startActiveDelay));
    }

    void Update()
    {
        if (useTimer)
        {
            timer += Time.deltaTime;

            if (isActive && timer >= activeTime)
            {
                Deactivate();
            }
            else if (!isActive && timer >= inactiveTime)
            {
                timer = 0f;

                if (useWarning && warningPrefab != null)
                    StartCoroutine(WarningThenActivate());
                else
                    StartLaserImmediately();
            }
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

    public void Activate()
    {
        if (useWarning && warningPrefab != null)
            StartCoroutine(WarningThenActivate());
        else
            StartLaserImmediately();
    }

    private IEnumerator WarningThenActivate()
    {
        isManuallyActivated = true;

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

        StartCoroutine(AnimateStartCircleAndLaser());

        // 🔹 레이저 발사 시 원형 파동도 동시에
        StartCoroutine(DrawWaveCircle());
    }
    private IEnumerator AnimateStartCircleAndLaser()
    {
        // 1. 원 생성
        CreateStartCircle();
        startCircle.transform.localScale = Vector3.zero;

        SpriteRenderer sr = startCircle.GetComponent<SpriteRenderer>();
        Color initialColor = sr != null ? sr.color : Color.white;

        float elapsed = 0f;
        while (elapsed < startCircleGrowDuration)
        {
            float t = elapsed / startCircleGrowDuration;
            // SmoothStep으로 부드럽게 커짐
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            // 살짝 튀는 느낌 추가 (sin 기반 흔들림)
            float bounce = 1f + Mathf.Sin(smoothT * Mathf.PI * 2f) * 0.15f;
            startCircle.transform.localScale = Vector3.one * startCircleSize * smoothT * bounce;

            // 색상 밝기 + 투명도 조절
            if (sr != null)
            {
                float alpha = Mathf.Lerp(0f, startCircleAlpha, t); // 점점 불투명해짐
                float brightness = 0.7f + 0.3f * Mathf.Sin(t * Mathf.PI); // 살짝 밝아졌다가 원래색
                sr.color = new Color(initialColor.r * brightness, initialColor.g * brightness, initialColor.b * brightness, alpha);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 최종 스케일 & 색상 보정
        startCircle.transform.localScale = Vector3.one * startCircleSize;
        if (sr != null) sr.color = initialColor;

        // 🔹 원 커진 후 레이저 시작 전 딜레이
        if (delayBeforeLaser > 0f)
            yield return new WaitForSeconds(delayBeforeLaser);

        // 2. 레이저 길이 점진 증가
        Vector2 dir = Quaternion.Euler(0, 0, angle) * Vector2.right;
        float currentLength = 0f;

        while (currentLength < laserLength)
        {
            currentLength += laserGrowSpeed * Time.deltaTime;
            if (currentLength > laserLength) currentLength = laserLength;

            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, transform.position + (Vector3)dir * currentLength);

            if (startCircle != null)
                startCircle.transform.position = transform.position;

            yield return null;
        }

        // 3. 레이저 지속 업데이트 시작
        StartCoroutine(LaserLoop());
    }

    private IEnumerator LaserLoop()
    {
        Color baseColor = lineRenderer.startColor; // 기본 레이저 색
        while (isActive)
        {
            FireLaser();

            // 텍스처 스크롤
            if (lineRenderer.material != null)
                lineRenderer.material.mainTextureOffset = new Vector2(Time.time * scrollSpeed, 0f);

            // 레이저 밝기 펄스
            float brightness = 0.9f + 0.1f * Mathf.Sin(Time.time * 3f); // 10f는 깜빡임 속도
            Color newColor = new Color(baseColor.r * brightness, baseColor.g * brightness, baseColor.b * brightness, baseColor.a);
            lineRenderer.startColor = newColor;
            lineRenderer.endColor = newColor;

            // 원 위치 갱신
            if (startCircle != null)
                startCircle.transform.position = lineRenderer.GetPosition(0);

            yield return null;
        }
    }

    private void CreateStartCircle()
    {
        if (startCircle != null) Destroy(startCircle);

        if (startCirclePrefab != null)
        {
            startCircle = Instantiate(startCirclePrefab, transform.position, Quaternion.identity);
            startCircle.transform.localScale = Vector3.one * startCircleSize;

            var sr = startCircle.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Color c = sr.color;
                c.a = startCircleAlpha;
                sr.color = c;
                sr.sortingOrder = lineRenderer.sortingOrder + 1;
            }
        }
    }

    public void Deactivate()
    {
        isManuallyActivated = false;
        isActive = false;
        timer = 0f;
        lineRenderer.enabled = false;

        if (startCircle != null)
        {
            Destroy(startCircle);
            startCircle = null;
        }
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
    private IEnumerator DrawWaveCircle()
    {
        while (isActive)
        {
            // 새 파동 생성
            GameObject wave = new GameObject("LaserWave");
            wave.transform.position = startCircle != null ? startCircle.transform.position : transform.position;
            LineRenderer waveRenderer = wave.AddComponent<LineRenderer>();

            waveRenderer.useWorldSpace = false;
            waveRenderer.loop = true;
            waveRenderer.positionCount = waveSegments;
            waveRenderer.startWidth = 0.05f;
            waveRenderer.endWidth = 0.05f;
            waveRenderer.material = new Material(Shader.Find("Sprites/Default"));
            waveRenderer.startColor = waveColor;
            waveRenderer.endColor = waveColor;
            waveRenderer.sortingOrder = 14;

            float elapsed = 0f;
            float alpha = waveColor.a;

            while (elapsed < waveDuration)
            {
                float t = elapsed / waveDuration;

                float radius = Mathf.Lerp(0f, waveMaxRadius, Mathf.SmoothStep(0f, 1f, t));

                float brightness = 0.7f + 0.3f * Mathf.Sin(t * Mathf.PI);
                Color c = new Color(waveColor.r * brightness, waveColor.g * brightness, waveColor.b * brightness, Mathf.Lerp(alpha, 0f, t));
                waveRenderer.startColor = c;
                waveRenderer.endColor = c;

                for (int i = 0; i < waveSegments; i++)
                {
                    float ang = (i / (float)waveSegments) * Mathf.PI * 2f;
                    float offset = Mathf.Sin(i + elapsed * 5f) * 0.02f;
                    waveRenderer.SetPosition(i, new Vector3(Mathf.Cos(ang) * (radius + offset), Mathf.Sin(ang) * (radius + offset), 0));
                }

                // 파동 위치를 항상 원 중심에 맞춤
                if (startCircle != null)
                    wave.transform.position = startCircle.transform.position;

                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(wave);

            // 🔹 다음 파동까지 간격
            yield return new WaitForSeconds(0.1f); // 파동 간격 조정 가능
        }
    }

}
