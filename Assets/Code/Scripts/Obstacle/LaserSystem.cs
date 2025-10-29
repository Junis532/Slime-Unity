using UnityEngine;
using System.Collections;


//// 예시: 보스가 레이저 쏠 때
//[SerializeField] private LaserSystem laserPrefab;

//void FireBossLaser()
//{
//    LaserSystem laser = Instantiate(laserPrefab);
//    laser.FireLaser(transform.position, 90f); // 90도 방향으로 발사
//}


/// <summary>
/// 범용 레이저 시스템 (보스 스킬, 트랩, 무기 등 어디서나 사용 가능)
/// Instantiate 후 FireLaser() 호출로 발사.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class LaserSystem : MonoBehaviour
{
    private LineRenderer lineRenderer;

    [Header("Laser Settings")]
    public float laserLength = 20f;
    public float angle = 0f;
    public int laserDamage = 100;
    public LayerMask raycastMask = ~0;

    [Header("Laser Appearance")]
    public Material laserMaterial;
    public float laserWidth = 0.1f;
    public float scrollSpeed = 2f;
    public Color laserColor = Color.red;

    [Header("Timing")]
    public float laserDuration = 2f;
    public float delayBeforeLaser = 0.3f;
    public float laserGrowSpeed = 30f;

    [Header("Start Circle Settings")]
    public GameObject startCirclePrefab;
    public float startCircleSize = 0.3f;
    public float startCircleGrowDuration = 0.3f;
    [Range(0f, 1f)] public float startCircleAlpha = 1f;

    [Header("Wave Effect")]
    public Color waveColor = new Color(1f, 0.3f, 0.3f, 0.7f);
    public float waveMaxRadius = 0.5f;
    public float waveDuration = 0.5f;
    public int waveSegments = 32;
    public float waveInterval = 0.1f;

    [Header("Damage Throttle")]
    public float damageCooldown = 0.15f;
    private float lastDamageTime = -999f;

    private bool isFiring = false;
    private GameObject startCircle;
    private Coroutine laserRoutine;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = laserWidth;
        lineRenderer.endWidth = laserWidth;
        lineRenderer.sortingOrder = 12;
        lineRenderer.enabled = false;

        if (laserMaterial != null)
            lineRenderer.material = laserMaterial;
        else
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

        lineRenderer.startColor = laserColor;
        lineRenderer.endColor = laserColor;
    }

    /// <summary>
    /// 외부에서 호출해 레이저 발사 시작
    /// </summary>
    public void FireLaser(Vector3 origin, float angleZ)
    {
        if (isFiring) return;
        transform.position = origin;
        angle = angleZ;
        isFiring = true;

        laserRoutine = StartCoroutine(LaserSequence());
    }

    private IEnumerator LaserSequence()
    {
        lineRenderer.enabled = true;
        yield return StartCoroutine(AnimateStartCircle());
        yield return StartCoroutine(AnimateLaser());
        yield return new WaitForSeconds(laserDuration);

        StopLaser();
    }

    private IEnumerator AnimateStartCircle()
    {
        if (startCirclePrefab == null) yield break;

        startCircle = Instantiate(startCirclePrefab, transform.position, Quaternion.identity);
        startCircle.transform.localScale = Vector3.zero;
        SpriteRenderer sr = startCircle.GetComponent<SpriteRenderer>();

        float elapsed = 0f;
        while (elapsed < startCircleGrowDuration)
        {
            float t = elapsed / startCircleGrowDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            float bounce = 1f + Mathf.Sin(smoothT * Mathf.PI * 2f) * 0.15f;
            startCircle.transform.localScale = Vector3.one * startCircleSize * smoothT * bounce;

            if (sr != null)
            {
                float alpha = Mathf.Lerp(0f, startCircleAlpha, t);
                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, alpha);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        startCircle.transform.localScale = Vector3.one * startCircleSize;
        if (delayBeforeLaser > 0f)
            yield return new WaitForSeconds(delayBeforeLaser);
    }

    private IEnumerator AnimateLaser()
    {
        Vector2 dir = Quaternion.Euler(0, 0, angle) * Vector2.right;
        float currentLength = 0f;
        lineRenderer.startColor = laserColor;
        lineRenderer.endColor = laserColor;

        StartCoroutine(WaveRoutine());

        while (currentLength < laserLength)
        {
            currentLength += laserGrowSpeed * Time.deltaTime;
            if (currentLength > laserLength) currentLength = laserLength;

            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, transform.position + (Vector3)dir * currentLength);

            yield return null;
        }

        StartCoroutine(LaserLoop());
    }

    private IEnumerator LaserLoop()
    {
        while (isFiring)
        {
            FireRaycast();
            if (lineRenderer.material != null)
                lineRenderer.material.mainTextureOffset = new Vector2(Time.time * scrollSpeed, 0f);

            float pulse = 0.9f + 0.1f * Mathf.Sin(Time.time * 3f);
            Color newColor = laserColor * pulse;
            lineRenderer.startColor = newColor;
            lineRenderer.endColor = newColor;

            yield return null;
        }
    }

    private void FireRaycast()
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

            if (hit.collider.CompareTag("Player") && Time.time - lastDamageTime >= damageCooldown)
            {
                lastDamageTime = Time.time;
                GameManager.Instance.playerDamaged.TakeDamage(laserDamage, transform.position);
            }
        }

        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, transform.position + (Vector3)dir * endDist);
    }

    private IEnumerator WaveRoutine()
    {
        while (isFiring)
        {
            GameObject wave = new GameObject("LaserWave");
            wave.transform.position = transform.position;
            LineRenderer wr = wave.AddComponent<LineRenderer>();

            wr.useWorldSpace = false;
            wr.loop = true;
            wr.positionCount = waveSegments;
            wr.startWidth = 0.05f;
            wr.endWidth = 0.05f;
            wr.material = new Material(Shader.Find("Sprites/Default"));
            wr.startColor = waveColor;
            wr.endColor = waveColor;
            wr.sortingOrder = 14;

            float elapsed = 0f;
            while (elapsed < waveDuration)
            {
                float t = elapsed / waveDuration;
                float radius = Mathf.Lerp(0f, waveMaxRadius, Mathf.SmoothStep(0f, 1f, t));
                float bright = 0.7f + 0.3f * Mathf.Sin(t * Mathf.PI);
                Color c = new Color(waveColor.r * bright, waveColor.g * bright, waveColor.b * bright, Mathf.Lerp(waveColor.a, 0f, t));
                wr.startColor = c; wr.endColor = c;

                for (int i = 0; i < waveSegments; i++)
                {
                    float ang = (i / (float)waveSegments) * Mathf.PI * 2f;
                    float offset = Mathf.Sin(i + elapsed * 5f) * 0.02f;
                    wr.SetPosition(i, new Vector3(Mathf.Cos(ang) * (radius + offset), Mathf.Sin(ang) * (radius + offset), 0));
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(wave);
            yield return new WaitForSeconds(waveInterval);
        }
    }

    public void StopLaser()
    {
        if (!isFiring) return;

        isFiring = false;
        lineRenderer.enabled = false;
        if (startCircle != null) Destroy(startCircle);
        StopAllCoroutines();
        Destroy(gameObject, 0.1f); // 자동 삭제 (원하면 제거)
    }
}
