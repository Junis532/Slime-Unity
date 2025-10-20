using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class EnemiesDie : MonoBehaviour
{
    public enum DeathStyle { Classic, SquishPopCinematic } // ▼ 인스펙터에서 선택
    [Header("Death Style")]
    public DeathStyle deathStyle = DeathStyle.Classic;

    private bool isLive = true;
    private GroupController groupController;
    private SpriteRenderer spriter;

    // 풀/복구/충돌 캐시
    private string originalTag;
    private Vector3 originalScale = Vector3.one;
    private Collider2D[] cachedCols;

    [Header("죽을 때 포션")]
    public GameObject potionPrefab;               // 풀에 등록되어 있으면 Pool 사용
    [Range(0f, 1f)] public float potionDropChance = 0.1f;

    // ---------------- Classic (원래 연출) ----------------
    [Header("Classic Params")]
    public float classicBackwardTime = 0.5f;
    public float classicBackwardDistance = 0.5f;
    public float classicShrinkTime = 0.5f;
    public float classicFadeTime = 0.5f;

    // ------------- SquishPopCinematic (코드 전용) -------------
    [Header("Cinematic - Movement / Scale")]
    public float spBackwardTime = 0.12f;
    public float spBackwardDistance = 0.45f;
    public float spSquishTime = 0.14f;
    public Vector2 spSquishScale = new Vector2(0.62f, 0.42f);
    public float spPrePopHold = 0.05f;
    public float spPopTime = 0.08f;
    public float spPopScale = 1.45f;
    public float spCollapseTime = 0.18f;
    public float spFadeTime = 0.20f;

    [Header("Cinematic - Time / Flash / Camera")]
    public bool spUseTimeSlow = true;
    [Range(0.01f, 1f)] public float spTimeScale = 0.1f;
    public float spTimeSlowDur = 0.08f;
    public bool spUseScreenFlash = true;
    public float spFlashAlpha = 0.9f;
    public float spFlashDur = 0.08f;
    public bool spUseCameraShake = true;
    public bool spUseCameraPump = true;
    public float spCamPumpZoomDelta = -0.6f;
    public float spCamPumpInTime = 0.08f;
    public float spCamPumpOutTime = 0.35f;

    [Header("Cinematic - Shockwave Ring (LineRenderer)")]
    public bool spUseShockwaveRing = true;
    public float spRingDuration = 0.35f;
    public float spRingRadius = 2.0f;
    public int spRingSegments = 48;
    public float spRingWidth = 0.12f;
    public Color spRingColor = new Color(1f, 1f, 1f, 0.85f);

    [Header("Cinematic - Debris (ParticleSystem)")]
    public bool spUseDebris = true;
    public int spDebrisCount = 24;
    public float spDebrisLifetime = 0.5f;
    public float spDebrisSpeed = 6f;
    public float spDebrisGravity = 0f;
    public Color spDebrisStartColor = new Color(1f, 0.85f, 0.4f, 1f);
    public Color spDebrisEndColor = new Color(1f, 0.85f, 0.4f, 0f);
    public float spDebrisStartSize = 0.08f;
    public float spDebrisEndSize = 0.02f;

    void Awake()
    {
        spriter = GetComponent<SpriteRenderer>();
        originalTag = gameObject.tag;
        originalScale = transform.localScale;
        cachedCols = GetComponentsInChildren<Collider2D>(includeInactive: true);
    }

    public void SetGroupController(GroupController group)
    {
        this.groupController = group;
    }

    public void Die()
    {
        if (!isLive) return;
        isLive = false;

        gameObject.tag = "Untagged";

        if (potionPrefab != null && Random.value <= potionDropChance)
        {
            if (PoolManager.Instance != null)
                PoolManager.Instance.SpawnFromPool(potionPrefab.name, transform.position, Quaternion.identity);
            else
                Instantiate(potionPrefab, transform.position, Quaternion.identity);
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        Vector3 backwardDir;
        float backDist = (deathStyle == DeathStyle.Classic) ? classicBackwardDistance : spBackwardDistance;

        if (playerObj != null)
        {
            Vector3 dirToPlayer = (playerObj.transform.position - transform.position).normalized;
            backwardDir = -dirToPlayer * backDist;
        }
        else
        {
            backwardDir = -transform.right * backDist;
        }

        if (cachedCols != null)
            foreach (var c in cachedCols) if (c) c.enabled = false;

        if (deathStyle == DeathStyle.Classic) PlayClassic(backwardDir);
        else PlayCinematic(backwardDir);
    }

    private void PlayClassic(Vector3 backwardDir)
    {
        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOMove(transform.position + backwardDir, classicBackwardTime).SetEase(Ease.OutQuad));
        seq.Join(transform.DOScale(Vector3.zero, classicShrinkTime).SetEase(Ease.InBack));
        if (spriter != null) seq.Join(spriter.DOFade(0f, classicFadeTime));
        seq.OnComplete(CompleteAndDisable);
    }

    private void PlayCinematic(Vector3 backwardDir)
    {
        float prevTimeScale = Time.timeScale;
        if (spUseTimeSlow)
        {
            Time.timeScale = spTimeScale;
            DOVirtual.DelayedCall(spTimeSlowDur, () => { Time.timeScale = prevTimeScale; }, ignoreTimeScale: true);
        }

        // 화면 플래시
        GameObject flash = null;
        if (spUseScreenFlash)
        {
            flash = new GameObject("DeathFlashCanvas");
            var canvas = flash.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            flash.AddComponent<CanvasScaler>();
            flash.AddComponent<GraphicRaycaster>();

            var imgGO = new GameObject("Flash");
            imgGO.transform.SetParent(flash.transform, false);
            var img = imgGO.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, spFlashAlpha);
            var rt = img.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            img.DOFade(0f, spFlashDur).SetUpdate(true).OnComplete(() => { if (flash) Destroy(flash); });
        }

        // 카메라 연출
        Camera cam = Camera.main;
        float camOrigOrtho = 0f;
        bool camIsOrtho = false;
        if (cam != null)
        {
            camIsOrtho = cam.orthographic;
            if (camIsOrtho) camOrigOrtho = cam.orthographicSize;

            if (spUseCameraShake)
            {
                if (GameManager.Instance && GameManager.Instance.cameraShake)
                    GameManager.Instance.cameraShake.GenerateImpulse(); // 인자 제거
            }

            if (spUseCameraPump && camIsOrtho)
            {
                DOTween.Sequence().SetUpdate(true)
                    .Append(DOTween.To(() => cam.orthographicSize, v => cam.orthographicSize = v,
                                       camOrigOrtho + spCamPumpZoomDelta, spCamPumpInTime).SetEase(Ease.OutCubic))
                    .Append(DOTween.To(() => cam.orthographicSize, v => cam.orthographicSize = v,
                                       camOrigOrtho, spCamPumpOutTime).SetEase(Ease.InOutSine))
                    .OnComplete(() => { if (cam) cam.orthographicSize = camOrigOrtho; });
            }
        }

        // 쇼크웨이브 링
        GameObject ringGO = null;
        LineRenderer ringLR = null;
        if (spUseShockwaveRing)
        {
            ringGO = new GameObject("ShockwaveRing");
            ringLR = ringGO.AddComponent<LineRenderer>();
            ringLR.useWorldSpace = true;
            ringLR.positionCount = spRingSegments + 1;
            ringLR.loop = true;
            ringLR.startWidth = ringLR.endWidth = spRingWidth;
            ringLR.material = new Material(Shader.Find("Sprites/Default"));
            ringLR.startColor = ringLR.endColor = spRingColor;
            ringLR.sortingLayerName = "Foreground";
            ringLR.sortingOrder = 100;
            ringGO.transform.position = transform.position;

            SetRingPositions(ringLR, 0.05f);

            var col = spRingColor;
            DOTween.To(() => 0.05f, r => SetRingPositions(ringLR, r), spRingRadius, spRingDuration)
                   .SetEase(Ease.OutCubic).SetUpdate(true);
            DOTween.To(() => col.a, a => { col.a = a; SetRingColorAlpha(ringLR, col); }, 0f, spRingDuration)
                   .SetEase(Ease.Linear).SetUpdate(true)
                   .OnComplete(() => { if (ringGO) Destroy(ringGO); });
        }

        // 파편
        if (spUseDebris)
            CreateAndBurstParticle(transform.position);

        // 본체 시퀀스
        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOMove(transform.position + backwardDir, spBackwardTime).SetEase(Ease.OutQuad));

        Vector3 squish = new Vector3(originalScale.x * spSquishScale.x, originalScale.y * spSquishScale.y, originalScale.z);
        seq.Join(transform.DOScale(squish, spSquishTime).SetEase(Ease.InQuad));

        if (spPrePopHold > 0f) seq.AppendInterval(spPrePopHold);

        Vector3 pop = originalScale * spPopScale;
        seq.Append(transform.DOScale(pop, spPopTime).SetEase(Ease.OutExpo));

        seq.Append(transform.DOScale(Vector3.zero, spCollapseTime).SetEase(Ease.InCubic));
        if (spriter != null && spFadeTime > 0f)
            seq.Join(spriter.DOFade(0f, spFadeTime));

        seq.OnComplete(() =>
        {
            if (spUseTimeSlow) Time.timeScale = 1f;
            CompleteAndDisable();
        });
    }

    private void SetRingPositions(LineRenderer lr, float radius)
    {
        int count = spRingSegments;
        for (int i = 0; i <= count; i++)
        {
            float t = (i / (float)count) * Mathf.PI * 2f;
            Vector3 p = new Vector3(Mathf.Cos(t), Mathf.Sin(t), 0f) * radius;
            lr.SetPosition(i, transform.position + p);
        }
    }

    private void SetRingColorAlpha(LineRenderer lr, Color c)
    {
        lr.startColor = lr.endColor = c;
    }

    private void CreateAndBurstParticle(Vector3 pos)
    {
        var go = new GameObject("DeathDebris_PS");
        var ps = go.AddComponent<ParticleSystem>();
        go.transform.position = pos;

        var main = ps.main;
        main.duration = 0.1f;
        main.loop = false;
        main.startLifetime = spDebrisLifetime;
        main.startSpeed = spDebrisSpeed;
        main.startSize = spDebrisStartSize;
        main.startColor = spDebrisStartColor;
        main.gravityModifier = spDebrisGravity;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.05f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(spDebrisStartColor, 0f),
                new GradientColorKey(spDebrisStartColor, 0.2f),
                new GradientColorKey(spDebrisEndColor,   1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(spDebrisStartColor.a, 0f),
                new GradientAlphaKey(spDebrisStartColor.a, 0.2f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(1f, spDebrisEndSize / Mathf.Max(0.0001f, spDebrisStartSize))
        );
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sCurve);

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingLayerName = "Foreground";
        renderer.sortingOrder = 200;

        ps.Emit(spDebrisCount);
        DOVirtual.DelayedCall(spDebrisLifetime + 0.1f, () => { if (go) Destroy(go); }, ignoreTimeScale: true);
    }

    private void CompleteAndDisable()
    {
        DOTween.Kill(transform);
        if (spriter != null) spriter.color = new Color(1, 1, 1, 1);
        gameObject.SetActive(false);
        if (groupController != null) groupController.OnChildDie();
    }

    void OnEnable()
    {
        isLive = true;
        DOTween.Kill(transform);
        if (spriter != null)
            spriter.color = new Color(1, 1, 1, 1);
        transform.localScale = originalScale;
        gameObject.tag = originalTag;
        if (cachedCols != null)
            foreach (var c in cachedCols) if (c) c.enabled = true;
    }

    void OnDisable()
    {
        DOTween.Kill(transform);
        if (Time.timeScale != 1f) Time.timeScale = 1f;
    }
}
