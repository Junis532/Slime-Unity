using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class MiddleBoss : MonoBehaviour
{
    private bool isLive = true;
    private SpriteRenderer spriter;
    private EnemyAnimation enemyAnimation;

    // ────────── 스킬/타이밍 ──────────
    [Header("패턴 타이밍")]
    public float skillInterval = 4f;
    private float skillTimer = 0f;
    private bool isSkillPlaying = false;

    // 인텐시티(스킬을 쓸수록 상승 → 난이도 가중치)
    [Range(0f, 3f)] public float intensity = 0f;
    public float intensityPerSkill = 0.15f;
    public float intensityMax = 2.0f;

    // 리듬(BPM) 펄스
    [Header("연출 리듬(BPM)")]
    public float tempoBPM = 120f; // 120BPM = 0.5초 주기
    private float BeatPeriod => 60f / Mathf.Max(1f, tempoBPM);

    // ────────── 패턴 온/오프 ──────────
    [Header("패턴 온/오프")]
    public bool enableBulletCircle = true;
    public bool enableLaserPattern = true;
    public bool enableSwordPattern = true;
    public bool enableJumpPattern = false;

    // ────────── 패턴별 경고 표시 ──────────
    [Header("경고 표시 토글")]
    public bool warnBulletCircle = false;
    public bool warnLaserPattern = true;
    public bool warnSwordPattern = true;
    public bool warnJumpPattern = false;

    // ────────── 패턴 1: 탄막 ──────────
    [Header("탄막 패턴")]
    public GameObject bulletPrefab;
    public int bulletsPerWave = 24; // 밀도 ↑ 추천값
    public int bulletAngle = 0;
    public float bulletSpeed = 6f;

    // ────────── 탄막 밀도/템포 간단 제어 ──────────
    [Header("탄막 패턴(밀도/템포)")]
    [Tooltip("기본 발사 간격(낮을수록 빠르게)")]
    public float bulletFireInterval = 0.35f;
    [Tooltip("탄막 총 지속시간")]
    public float bulletPatternDuration = 6.0f;
    [Tooltip("경고 단계/본 패턴에서 회피 갭 사용 여부")]
    public bool useGaps = false; // 기본 꺼서 밀도 보존
    [Range(0f, 1f)] public float bulletSpeedScale = 1.0f;
    [Range(0f, 1f)] public float spinAdd = 0.15f;

    // (이전 세부 옵션도 유지)
    [Header("탄막 퀄업 옵션(상세)")]
    [Range(0, 6)] public int gapCount = 2;
    [Range(0f, 1f)] public float gapWidthRatio = 0.15f;
    public AnimationCurve spinCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float spinUpSeconds = 1.2f;
    public float spinDownSeconds = 0.8f;

    // ────────── 패턴 2: 레이저 ──────────
    [Header("레이저 패턴")]
    public Collider2D mapCollider;
    public int laserDamage = 100;
    public Material laserMaterial;

    [Header("레이저 시작점 조정")]
    public float leftLaserOffsetX = -2f;
    public float rightLaserOffsetX = 2f;
    public Vector2 laserExtraStartOffset = Vector2.zero;
    public bool useStartAnchors = false;
    public Transform leftLaserAnchor;
    public Transform rightLaserAnchor;

    public enum SweepWaveform { Sine, Triangle }

    [Header("규칙적 교차 스윕")]
    public bool useRegularCrossing = true;
    public float crossingHz = 0.9f;
    public float crossingAmplitudeUnits = 6f;
    public SweepWaveform waveform = SweepWaveform.Triangle;
    [Range(0f, 0.45f)] public float edgeHoldRatio = 0.2f;

    [Header("경계/길이/보조탄")]
    public bool clampToBounds = true;
    public float clampMargin = 0.5f;
    public float laserActiveDuration = 8f;
    public float laserOverrun = 5f;
    public float fireInterval = 0.5f;

    // 레이저 연출 업
    [Header("레이저 퀄업 옵션")]
    public bool laserWidthPulse = true;          // BPM 연동 폭 펄스
    public float laserWidthBase = 0.18f;
    public float laserWidthPulseAmt = 0.12f;
    public bool showMagicCircleWarn = true;      // 예고 단계에 마법진

    // ────────── 패턴 3: 검 ──────────
    [Header("검 휘두르기")]
    public float swordRotateSpeed = 360f;
    public float swordStartAngle = 180f;
    public float swordWarningDuration = 1f;

    [Header("검 퀄업 옵션")]
    public int afterimageCount = 3;
    public float afterimageFadeSeconds = 0.35f;
    public float endShockwaveBullets = 12f;
    [Range(0f, 1f)] public float endShockwaveCountScale = 0.6f;  // 가불감 완화
    [Range(0f, 0.08f)] public float endShockwaveStagger = 0.03f; // 순차 발사

    // ────────── 패턴 4: 점프 후 원형탄 ──────────
    [Header("점프 후 원형탄")]
    public float jumpHeight = 5f;
    public float jumpDuration = 0.5f;
    public int jumpBulletCount = 8;
    public float jumpBulletSpeed = 6f;

    // ────────── 경고 프리팹(선택) ──────────
    [Header("경고 프리팹(선택)")]
    public GameObject warningPrefab;
    public float warningLengthScale = 2f;
    public float warningThicknessScale = 0.5f;
    public float warningOffsetDistance = 1.5f;

    // ────────── 공통 경고 옵션 ──────────
    [Header("공통 경고 옵션")]
    public float preWarnDuration = 1.0f;
    public float warnLineWidth = 0.28f;
    public Color warnLineColor = new Color(0.68f, 0.45f, 1f, 0.95f); // 보라
    public Material warnLineMaterial;
    public bool keepWarnDuringTransition = false;
    public float warnTransitionTime = 0.15f;

    // ────────── FX: 스프라이트 마법진(프리팹 없이) ──────────
    [Header("FX: 마법진(스프라이트 전용)")]
    public Sprite magicCircleSprite;                          // 드래그 or
    public Sprite[] extraMagicRings;                          // 선택
    public string magicCircleResourcesPath = "";              // Resources 경로
    public Color magicCircleColor = new Color(0.85f, 0.7f, 1f, 0.9f);
    public float magicCircleBaseScale = 3.0f;                 // 크게
    public float magicCircleSpinSpeed = 220f;                 // 빠르게
    public float magicCirclePulseScale = 0.12f;
    public float magicCircleFadeOut = 0.15f;

    [Header("FX: 여러 링 옵션")]
    public bool ringsCounterRotate = true;
    [Range(0.75f, 1.5f)] public float ringScaleStep = 1.12f;
    [Range(-3f, 3f)] public float ringSpinMul = -0.6f;
    public int ringSortingOrderBase = 50;

    // ────────── FX: 마법진 휙(Whip) 업 ──────────
    [Header("FX: 마법진 휙(Whip) 업")]
    public bool magicWhipOnStart = true;
    public float magicWhipScaleMul = 1.35f;
    public float magicWhipSpinSpeed = 900f;
    public float magicWhipDuration = 0.22f;
    public AnimationCurve magicWhipEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // ────────── FX: 레이저 아웃라인(2겹) ──────────
    [Header("FX: 레이저 아웃라인(2겹)")]
    public bool laserOutline = true;
    public float outlineWidthAdd = 0.08f;
    public Color outlineColor = new Color(1f, 0.6f, 1f, 0.35f);

    // ────────── FX: 탄 트레일 ──────────
    public enum TrailPreset { Off, CleanThin, CleanShort, Ghost, Heavy }
    [Header("트레일 프리셋")]
    public bool addBulletTrail = true;
    public TrailPreset trailPreset = TrailPreset.CleanThin;
    public Gradient trailGradient; // 비우면 보라→투명 기본값 생성

    // ────────── FX: 마법진 광원/글로우 ──────────
    [Header("FX: 마법진 광원/글로우")]
    public bool addCircleLight = true;       // URP 2D Light2D 우선
    public float lightRadius = 4.0f;
    public float lightIntensity = 1.6f;
    public Color lightColor = new Color(0.95f, 0.7f, 1f, 1f);
    public bool additiveGlowFallback = true; // URP 없으면 Additive 스프라이트
    public float glowSpriteScaleMul = 1.25f;
    [Range(0f, 1f)] public float glowSpriteAlpha = 0.55f;

    // ────────── 감각 강화 ──────────
    [Header("감각 강화")]
    public bool useHitStop = true;
    [Range(0.01f, 1f)] public float hitStopScale = 0.15f;
    [Range(0.02f, 0.25f)] public float hitStopDuration = 0.06f;
    public bool useCameraShake = true;
    public float shakeAmplitude = 0.2f;
    public float shakeDuration = 0.2f;
    public bool flashSpriteOnFire = true;
    public Color flashColor = new Color(1f, 0.8f, 0.2f, 1f);
    public float flashDuration = 0.05f;

    [Header("카메라 줌(선택)")]
    public bool zoomOnClimax = true;
    public float zoomSize = 4.8f;
    public float zoomInDur = 0.12f;
    public float zoomHold = 0.10f;
    public float zoomOutDur = 0.15f;

    // ────────── 디버그 ──────────
    [Header("디버그")]
    public bool debugForceBulletCircle = false;

    private readonly List<GameObject> activeSkillObjects = new();
    private readonly List<LineRenderer> _warnLines = new();

    // 내부 캐시
    private Transform camT;
    private Vector3 camOrigin;
    private Camera cam;

    // ===========================================================
    //                         Unity
    // ===========================================================
    void Start()
    {
        spriter = GetComponent<SpriteRenderer>();
        enemyAnimation = GetComponent<EnemyAnimation>();

        if (mapCollider == null)
        {
            GameObject roomObj = GameObject.Find("RC 00");
            if (roomObj != null)
            {
                mapCollider = roomObj.GetComponent<BoxCollider2D>();
                if (mapCollider == null)
                    Debug.LogWarning("RC 00 안에 BoxCollider2D가 없습니다!");
            }
            else Debug.LogWarning("RC 00 오브젝트를 찾을 수 없습니다!");
        }

        if (Camera.main)
        {
            cam = Camera.main;
            camT = cam.transform;
            camOrigin = camT.position;
        }

        // 트레일 그라데이션 기본값(보라 → 투명)
        if (trailGradient == null || trailGradient.colorKeys == null || trailGradient.colorKeys.Length == 0)
        {
            trailGradient = new Gradient();
            trailGradient.SetKeys(
                new[] {
                    new GradientColorKey(new Color(0.8f, 0.6f, 1f, 1f), 0f),
                    new GradientColorKey(new Color(1f, 1f, 1f, 1f), 1f)
                },
                new[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
        }

        if (!bulletPrefab)
            Debug.LogWarning("[MiddleBoss] bulletPrefab이 비어있습니다. 탄막/보조탄이 생성되지 않습니다.");
    }

    void Update()
    {
        if (debugForceBulletCircle && !isSkillPlaying)
        {
            debugForceBulletCircle = false;
            isSkillPlaying = true;
            StartSafe(SkillBulletCircle());
            return;
        }

        if (!isLive || isSkillPlaying) return;

        skillTimer += Time.deltaTime;
        if (skillTimer >= skillInterval)
        {
            skillTimer = 0f;
            UseRandomSkill();
        }
    }

    private void UseRandomSkill()
    {
        isSkillPlaying = true;

        var skills = new List<Func<IEnumerator>>();
        if (enableBulletCircle) skills.Add(SkillBulletCircle);
        if (enableLaserPattern) skills.Add(SkillLaserPattern);
        if (enableSwordPattern) skills.Add(SkillSwordPattern);
        if (enableJumpPattern) skills.Add(SkillJumpAndShoot);

        if (skills.Count == 0)
        {
            Debug.LogWarning("활성화된 보스 패턴이 없습니다.");
            isSkillPlaying = false;
            return;
        }

        int idx = UnityEngine.Random.Range(0, skills.Count);
        StartSafe(skills[idx]());
    }

    // ────────────────────────
    // 세이프 실행 래퍼 (예외 대비) — CS1626 해결 버전
    // ────────────────────────
    private void StartSafe(IEnumerator routine) => StartCoroutine(CoSafe(routine));

    private IEnumerator CoSafe(IEnumerator routine)
    {
        bool finished = false;

        while (true)
        {
            object current;
            try
            {
                // MoveNext 중 예외를 잡는다. (yield는 try 밖에서)
                if (!routine.MoveNext())
                {
                    finished = true;
                    break;
                }
                current = routine.Current;
            }
            catch (Exception e)
            {
                Debug.LogError($"[MiddleBoss] 스킬 실행 중 예외: {e}");
                break;
            }

            // 실제 대기/프레임 양보는 try 밖에서 수행 -> CS1626 회피
            yield return current;
        }

        if (!finished)
        {
            // 예외 등으로 중단된 경우 정리하고 다음 패턴 가능
            ClearAllSkillObjects();
            isSkillPlaying = false;
        }
    }

    // ===========================================================
    //                     경고/마법진 유틸
    // ===========================================================
    private static Gradient MakeSolidGradient(Color c)
    {
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
            new[] { new GradientAlphaKey(c.a, 0f), new GradientAlphaKey(c.a, 1f) }
        );
        return g;
    }

    private void ApplyWarnColor(LineRenderer lr, Color c)
    {
        if (lr.material != null)
        {
            if (lr.material.HasProperty("_Color")) lr.material.color = Color.white;
            if (lr.material.HasProperty("_TintColor")) lr.material.SetColor("_TintColor", Color.white);
        }
        lr.colorGradient = MakeSolidGradient(c);
        lr.startColor = lr.endColor = c;
    }

    private LineRenderer CreateWarnLine(string name, float widthMul = 1f, int order = 9)
    {
        var go = new GameObject(name);
        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.startWidth = lr.endWidth = Mathf.Max(0.001f, warnLineWidth * widthMul);
        lr.material = warnLineMaterial != null ? warnLineMaterial : new Material(Shader.Find("Sprites/Default"));
        lr.useWorldSpace = true;
        lr.sortingLayerName = "Foreground";
        lr.sortingOrder = order;
        ApplyWarnColor(lr, warnLineColor);
        _warnLines.Add(lr);
        activeSkillObjects.Add(go);
        return lr;
    }

    private LineRenderer CreateWarnCircleLine(string name, Vector3 center, float radius, int segments, int order = 8, float alpha = 0.75f)
    {
        segments = Mathf.Max(8, segments);
        var go = new GameObject(name);
        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = segments + 1;
        lr.startWidth = lr.endWidth = Mathf.Max(0.001f, warnLineWidth * 0.8f);
        lr.material = warnLineMaterial != null ? warnLineMaterial : new Material(Shader.Find("Sprites/Default"));
        lr.useWorldSpace = true;
        lr.sortingLayerName = "Foreground";
        lr.sortingOrder = order;

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float ang = t * Mathf.PI * 2f;
            Vector3 p = center + new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * radius;
            lr.SetPosition(i, p);
        }

        var c = warnLineColor; c.a = alpha;
        ApplyWarnColor(lr, c);
        _warnLines.Add(lr);
        activeSkillObjects.Add(go);
        return lr;
    }

    // 스프라이트 결정(직접 참조 > Resources 경로)
    private Sprite ResolveMagicSprite(Sprite direct, string resourcesPath)
    {
        if (direct) return direct;
        if (!string.IsNullOrEmpty(resourcesPath))
        {
            var s = Resources.Load<Sprite>(resourcesPath);
            if (!s) Debug.LogWarning($"[MiddleBoss] Resources에서 스프라이트를 못 찾음: {resourcesPath}");
            return s;
        }
        return null;
    }

    // URP 2D Light2D 시도(있으면 true). 없거나 실패 시 false
    private bool TryAddURP2DLight(GameObject parent, float radius, float intensity, Color color)
    {
        try
        {
            var asm = AppDomain.CurrentDomain.Load("Unity.RenderPipelines.Universal.Runtime");
            if (asm == null) return false;

            var lightType = asm.GetType("UnityEngine.Rendering.Universal.Light2D");
            if (lightType == null) return false;

            var go = new GameObject("MagicCircleLight");
            go.transform.SetParent(parent.transform, worldPositionStays: true);
            go.transform.localPosition = Vector3.zero;

            var comp = go.AddComponent(lightType);

            var prop_lightType = lightType.GetProperty("lightType");
            var enumLightType = asm.GetType("UnityEngine.Rendering.Universal.Light2D+LightType");
            var pointEnum = Enum.Parse(enumLightType, "Point");
            prop_lightType?.SetValue(comp, pointEnum);

            lightType.GetProperty("color")?.SetValue(comp, color);
            lightType.GetProperty("intensity")?.SetValue(comp, intensity);
            lightType.GetProperty("pointLightOuterRadius")?.SetValue(comp, radius);
            lightType.GetProperty("pointLightInnerRadius")?.SetValue(comp, Mathf.Max(0.1f, radius * 0.4f));
            return true;
        }
        catch { return false; }
    }

    // Additive 글로우 스프라이트(URP 없을 때 대체) — 셰이더 폴백 포함
    private void AddGlowSprite(Sprite src, Transform parent, float scale, float alpha, int orderOffset)
    {
        var go = new GameObject("MagicGlow");
        go.transform.SetParent(parent, worldPositionStays: true);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = Vector3.one * scale;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = src;
        sr.color = new Color(1f, 0.8f, 1f, alpha);
        sr.sortingLayerName = "Foreground";
        sr.sortingOrder = ringSortingOrderBase + orderOffset;

        Shader sh = Shader.Find("Particles/Additive");
        if (sh == null) sh = Shader.Find("Sprites/Default"); // 폴백
        sr.material = new Material(sh);

        go.transform.DOScale(scale * 1.06f, BeatPeriod * 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
    }

    // 스프라이트 마법진(프리팹 없이)
    private GameObject SpawnMagicCircleSprite(
        Vector3 pos,
        float baseScale,
        float lifeSeconds,
        float spinDegPerSec,
        Color col,
        float pulseScale = 0.1f)
    {
        var main = ResolveMagicSprite(magicCircleSprite, magicCircleResourcesPath);
        if (!main) return null;

        var root = new GameObject("MagicCircleRoot");
        root.transform.position = pos;
        activeSkillObjects.Add(root);

        GameObject MakeRing(Sprite spr, float scale, float baseSpinMul, int sortingOrder)
        {
            var go = new GameObject($"Ring_{spr.name}");
            go.transform.SetParent(root.transform, worldPositionStays: true);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * scale;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = spr;
            sr.color = col;
            sr.sortingLayerName = "Foreground";
            sr.sortingOrder = sortingOrder;

            // 기본 지속 회전(루프)
            go.transform.DORotate(
                new Vector3(0, 0, 360f * baseSpinMul),
                360f / Mathf.Max(1f, spinDegPerSec * Mathf.Abs(baseSpinMul)),
                RotateMode.FastBeyond360
            ).SetLoops(-1, LoopType.Incremental).SetEase(Ease.Linear);

            // 시작 순간 ‘휙’ 느낌: 추가 회전 + 팝업 스케일
            if (magicWhipOnStart)
            {
                go.transform.DORotate(
                    new Vector3(0, 0, magicWhipSpinSpeed * magicWhipDuration * Mathf.Sign(baseSpinMul)),
                    magicWhipDuration, RotateMode.LocalAxisAdd
                ).SetEase(magicWhipEase);

                float from = scale * magicWhipScaleMul;
                go.transform.localScale = Vector3.one * from;
                go.transform.DOScale(scale, magicWhipDuration).SetEase(magicWhipEase);
            }

            // 박동(펄스)
            if (pulseScale > 0f)
            {
                var to = scale * (1f + pulseScale);
                go.transform.DOScale(to, BeatPeriod * 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
            }
            return go;
        }

        // 메인 링
        MakeRing(main, baseScale, 1f, ringSortingOrderBase);

        // 추가 링(있다면 반대 회전으로 얹음)
        if (extraMagicRings != null && extraMagicRings.Length > 0)
        {
            float curScale = baseScale * ringScaleStep;
            float curSpinMul = (ringsCounterRotate ? -1f : 1f);
            int order = ringSortingOrderBase - 1;

            foreach (var spr in extraMagicRings)
            {
                if (!spr) continue;
                MakeRing(spr, curScale, curSpinMul, order--);
                curScale *= ringScaleStep;
                curSpinMul *= (ringsCounterRotate ? -1f : 1f);
            }
        }

        // 광원/글로우
        if (addCircleLight)
        {
            bool lightAdded = TryAddURP2DLight(root, lightRadius, lightIntensity, lightColor);
            if (!lightAdded && additiveGlowFallback)
            {
                AddGlowSprite(main, root.transform, baseScale * glowSpriteScaleMul, glowSpriteAlpha, +1);
                AddGlowSprite(main, root.transform, baseScale * (glowSpriteScaleMul * 1.3f), glowSpriteAlpha * 0.6f, -1);
            }
        }

        // 수명 후 페이드아웃 & 제거
        DOVirtual.DelayedCall(Mathf.Max(0.05f, lifeSeconds), () =>
        {
            var srs = root.GetComponentsInChildren<SpriteRenderer>();
            foreach (var s in srs) s.DOFade(0f, magicCircleFadeOut);
            DOVirtual.DelayedCall(magicCircleFadeOut, () => { if (root) Destroy(root); });
        });

        return root;
    }

    private IEnumerator _CoDelayedKillWarnLines(float delay)
    {
        yield return new WaitForSeconds(delay);
        KillAllWarnLines();
    }

    private void KillAllWarnLines()
    {
        for (int i = _warnLines.Count - 1; i >= 0; i--)
            if (_warnLines[i]) Destroy(_warnLines[i].gameObject);
        _warnLines.Clear();
    }

    private void SetLineVertical(LineRenderer lr, Vector3 center, float halfLen)
    {
        lr.SetPosition(0, center + Vector3.up * halfLen);
        lr.SetPosition(1, center + Vector3.down * halfLen);
    }

    private void SetLineByDir(LineRenderer lr, Vector3 start, Vector3 dir, float len)
    {
        lr.SetPosition(0, start);
        lr.SetPosition(1, start + dir.normalized * len);
    }

    // ===========================================================
    //                       공중 탄환 유틸
    // ===========================================================
    private Rigidbody2D EnsureRB2D(GameObject go)
    {
        var rb = go.GetComponent<Rigidbody2D>();
        if (!rb) rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.freezeRotation = true;
        return rb;
    }

    // 트레일 프리셋 적용 (깔끔하게)
    private void EnsureTrail(GameObject go)
    {
        if (!addBulletTrail) return;
        if (trailPreset == TrailPreset.Off) return;

        var tr = go.GetComponent<TrailRenderer>();
        if (!tr) tr = go.AddComponent<TrailRenderer>();

        // 공통
        tr.minVertexDistance = 0.02f;
        tr.numCapVertices = 2;
        tr.numCornerVertices = 2;
        tr.sortingLayerName = "Foreground";
        tr.sortingOrder = 11;

        // 머티리얼(안전 폴백)
        var sh = Shader.Find("Particles/Additive");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        if (!tr.material) tr.material = new Material(sh);
        tr.material.color = Color.white;

        // 색상 그라데이션(보라 → 투명) 기본값
        if (trailGradient == null || trailGradient.colorKeys == null || trailGradient.colorKeys.Length == 0)
        {
            trailGradient = new Gradient();
            trailGradient.SetKeys(
                new[] {
                    new GradientColorKey(new Color(0.85f, 0.75f, 1f, 1f), 0f),
                    new GradientColorKey(new Color(1f, 1f, 1f, 1f), 1f)
                },
                new[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
        }
        tr.colorGradient = trailGradient;

        // 프리셋
        switch (trailPreset)
        {
            case TrailPreset.CleanThin:
                tr.time = 0.28f;
                tr.startWidth = 0.08f;
                tr.endWidth = 0.0f;
                break;
            case TrailPreset.CleanShort:
                tr.time = 0.18f;
                tr.startWidth = 0.10f;
                tr.endWidth = 0.0f;
                break;
            case TrailPreset.Ghost:
                tr.time = 0.45f;
                tr.startWidth = 0.06f;
                tr.endWidth = 0.0f;
                break;
            case TrailPreset.Heavy:
                tr.time = 0.50f;
                tr.startWidth = 0.13f;
                tr.endWidth = 0.02f;
                break;
            default:
                tr.time = 0.30f;
                tr.startWidth = 0.08f;
                tr.endWidth = 0.0f;
                break;
        }
    }

    private GameObject SpawnBullet(Vector2 pos, Vector2 vel)
    {
        if (!bulletPrefab)
        {
            Debug.LogWarning("[MiddleBoss] bulletPrefab 없음 → 탄 발사 스킵");
            return null;
        }

        var go = Instantiate(bulletPrefab, pos, Quaternion.identity);
        var rb = EnsureRB2D(go);
        rb.linearVelocity = vel;
        EnsureTrail(go);
        activeSkillObjects.Add(go);
        return go;
    }

    // ===========================================================
    //                 감각 유틸(히트/셰이크/플래시/줌)
    // ===========================================================
    private void DoHitStop()
    {
        if (!useHitStop) return;
        DOTween.Kill("HitStopTS");
        DOTween.To(() => Time.timeScale, v => Time.timeScale = v, 0.0001f + hitStopScale, 0.0f)
               .SetId("HitStopTS").SetUpdate(true);
        DOVirtual.DelayedCall(hitStopDuration, () =>
        {
            DOTween.To(() => Time.timeScale, v => Time.timeScale = v, 1f, 0.02f).SetUpdate(true);
        }).SetUpdate(true);
    }

    private void ShakeCamera()
    {
        if (!useCameraShake || camT == null) return;
        camT.DOKill();
        camT.position = camOrigin;
        camT.DOShakePosition(shakeDuration, shakeAmplitude, vibrato: 12, randomness: 90f, snapping: false, fadeOut: true)
            .OnComplete(() => camT.position = camOrigin);
    }

    private void FlashSprite()
    {
        if (!flashSpriteOnFire || spriter == null) return;
        Color baseC = spriter.color;
        spriter.DOKill();
        spriter.DOColor(flashColor, flashDuration).OnComplete(() =>
            spriter.DOColor(baseC, flashDuration));
    }

    private IEnumerator CamZoomPunch()
    {
        if (!zoomOnClimax || cam == null) yield break;
        float orig = cam.orthographicSize;
        yield return DOTween.To(() => cam.orthographicSize, v => cam.orthographicSize = v, zoomSize, zoomInDur)
                            .SetEase(Ease.OutQuad).WaitForCompletion();
        yield return new WaitForSeconds(zoomHold);
        yield return DOTween.To(() => cam.orthographicSize, v => cam.orthographicSize = v, orig, zoomOutDur)
                            .SetEase(Ease.InQuad).WaitForCompletion();
    }

    // ===========================================================
    //                 스킬 1: 회전 탄막
    // ===========================================================
    private IEnumerator SkillBulletCircle()
    {
        int count = Mathf.Max(1, bulletsPerWave);
        float step = 360f / count;

        // 밀도/템포를 새 옵션으로 계산
        float duration = bulletPatternDuration + intensity * 0.6f;
        float fireIntervalLocal = Mathf.Clamp(bulletFireInterval - 0.05f * intensity, 0.18f, 0.6f);

        float spinDir = Mathf.Sign(UnityEngine.Random.Range(-1f, 1f) + 0.01f);
        float tElapsed = 0f;

        // 경고(보라) + 스프라이트 마법진
        float warnOffset = 0f;
        GameObject mcWarn = null;
        if (warnBulletCircle)
        {
            float warnTime = 0f;
            float warnRadius = 3.0f;
            for (int i = 0; i < count; i++) CreateWarnLine($"Warn_BulletDir_{i}", 1.35f, 9);

            mcWarn = SpawnMagicCircleSprite(
                transform.position,
                magicCircleBaseScale,
                preWarnDuration + 0.05f,
                magicCircleSpinSpeed,
                magicCircleColor,
                magicCirclePulseScale
            );

            while (warnTime < preWarnDuration)
            {
                Vector3 origin = transform.position;
                int idx = 0;
                foreach (var wl in _warnLines)
                {
                    float a = (step * idx + warnOffset) * Mathf.Deg2Rad;
                    Vector3 dir = new(Mathf.Cos(a), Mathf.Sin(a), 0f);
                    SetLineByDir(wl, origin, dir, warnRadius);
                    idx++;
                }
                float beat = Mathf.PingPong(warnTime, BeatPeriod) / BeatPeriod;
                warnOffset += (bulletAngle + 30f * beat) * Time.deltaTime * spinDir;
                warnTime += Time.deltaTime;
                yield return null;
            }
            KillAllWarnLines();
        }

        if (mcWarn) mcWarn.transform.DOScale(magicCircleBaseScale * 1.05f, 0.12f).SetLoops(2, LoopType.Yoyo);

        // 본 패턴
        float spinOffset = warnOffset;
        float gapPhase = UnityEngine.Random.Range(0f, 360f);

        while (tElapsed < duration)
        {
            float spinMul = 1f + 0.5f * intensity + spinAdd;
            Vector3 origin = transform.position;

            if (!bulletPrefab)
            {
                Debug.LogWarning("[MiddleBoss] bulletPrefab 없음 → BulletCircle 스킵");
                break;
            }

            bool useGapNow = useGaps && gapCount > 0 && gapWidthRatio > 0.01f;
            float gapWidth = Mathf.Clamp01(gapWidthRatio) * step;

            for (int i = 0; i < count; i++)
            {
                float ang = (step * i + spinOffset) % 360f;

                if (useGapNow)
                {
                    bool inGap = false;
                    for (int g = 0; g < gapCount; g++)
                    {
                        float center = (gapPhase + g * (360f / Mathf.Max(1, gapCount))) % 360f;
                        float delta = Mathf.DeltaAngle(ang, center);
                        if (Mathf.Abs(delta) <= gapWidth * 0.5f) { inGap = true; break; }
                    }
                    if (inGap) continue;
                }

                float rad = ang * Mathf.Deg2Rad;
                Vector2 dir = new(Mathf.Cos(rad), Mathf.Sin(rad));
                float spd = bulletSpeed * (1f + 0.25f * intensity) * Mathf.Max(0.2f, bulletSpeedScale);
                SpawnBullet(origin, dir * spd);
            }

            FlashSprite(); DoHitStop(); ShakeCamera();

            // 회전과 갭 진행
            spinOffset += (bulletAngle + 32f * spinMul) * spinDir;
            gapPhase += step * 0.22f * spinMul;

            tElapsed += fireIntervalLocal;
            yield return new WaitForSeconds(fireIntervalLocal);
        }

        yield return StartCoroutine(SkillFinished());
    }

    // ===========================================================
    //             스킬 2: 규칙적 교차 스윕 레이저
    // ===========================================================
    private IEnumerator SkillLaserPattern()
    {
        if (mapCollider == null)
        {
            Debug.LogWarning("mapCollider 미지정!");
            yield return StartCoroutine(SkillFinished());
            yield break;
        }

        Bounds b = mapCollider.bounds;

        Vector3 leftBase = (useStartAnchors && leftLaserAnchor) ? leftLaserAnchor.position : transform.position + new Vector3(leftLaserOffsetX, 0);
        Vector3 rightBase = (useStartAnchors && rightLaserAnchor) ? rightLaserAnchor.position : transform.position + new Vector3(rightLaserOffsetX, 0);
        leftBase += (Vector3)laserExtraStartOffset;
        rightBase += (Vector3)laserExtraStartOffset;

        float over = Mathf.Max(0f, laserOverrun);
        float topY = b.extents.y + over;
        float centerX = (leftBase.x + rightBase.x) * 0.5f;
        float halfSep0 = Mathf.Abs(rightBase.x - leftBase.x) * 0.5f;

        float amp = Mathf.Max(0f, crossingAmplitudeUnits) * (1f + 0.15f * intensity);
        float hz = Mathf.Max(0.1f, crossingHz * (1f + 0.12f * intensity));
        float period = 1f / hz;

        // 예고(보라 라인 + 스프라이트 마법진)
        float tWarn = 0f;
        if (warnLaserPattern)
        {
            SpawnMagicCircleSprite(
                transform.position,
                magicCircleBaseScale,
                preWarnDuration + 0.05f,
                magicCircleSpinSpeed,
                magicCircleColor,
                magicCirclePulseScale
            );

            var warnL = CreateWarnLine("Warn_Left", 1.8f, 9);
            var warnR = CreateWarnLine("Warn_Right", 1.8f, 9);

            while (tWarn < preWarnDuration)
            {
                float wv = Waveform01(tWarn / period, waveform);
                if (edgeHoldRatio > 0f) wv = ApplyEdgeHold(wv, edgeHoldRatio);
                float inward = Mathf.Lerp(-amp, amp, wv);

                float lx = centerX - halfSep0 + inward;
                float rx = centerX + halfSep0 - inward;

                if (clampToBounds)
                {
                    float minX = b.min.x + clampMargin;
                    float maxX = b.max.x - clampMargin;
                    lx = Mathf.Clamp(lx, minX, maxX);
                    rx = Mathf.Clamp(rx, minX, maxX);
                }

                Vector3 curL = new(lx, transform.position.y, 0f);
                Vector3 curR = new(rx, transform.position.y, 0f);

                SetLineVertical(warnL, curL, topY);
                SetLineVertical(warnR, curR, topY);

                float beat = Mathf.PingPong(tWarn, BeatPeriod) / BeatPeriod;
                float alpha = Mathf.Lerp(0.35f, 0.95f, beat);
                Color c = warnLineColor; c.a = alpha;
                warnL.colorGradient = MakeSolidGradient(c);
                warnR.colorGradient = MakeSolidGradient(c);
                warnL.startColor = warnL.endColor = c;
                warnR.startColor = warnR.endColor = c;

                tWarn += Time.deltaTime;
                yield return null;
            }

            if (!keepWarnDuringTransition) KillAllWarnLines();
            else StartCoroutine(_CoDelayedKillWarnLines(Mathf.Max(0f, warnTransitionTime)));
        }

        // 본 레이저(메인 + 아웃라인)
        var leftLaser = new GameObject("LeftLaser");
        var rightLaser = new GameObject("RightLaser");
        var leftLR = leftLaser.AddComponent<LineRenderer>();
        var rightLR = rightLaser.AddComponent<LineRenderer>();
        SetupLaser(leftLR, Color.red);
        SetupLaser(rightLR, Color.red);
        leftLR.sortingLayerName = rightLR.sortingLayerName = "Foreground";
        leftLR.sortingOrder = rightLR.sortingOrder = 10;
        activeSkillObjects.Add(leftLaser);
        activeSkillObjects.Add(rightLaser);

        LineRenderer leftOutline = null, rightOutline = null;
        if (laserOutline)
        {
            leftOutline = new GameObject("LeftLaserOutline").AddComponent<LineRenderer>();
            rightOutline = new GameObject("RightLaserOutline").AddComponent<LineRenderer>();
            SetupLaser(leftOutline, outlineColor);
            SetupLaser(rightOutline, outlineColor);
            leftOutline.startWidth = leftOutline.endWidth = laserWidthBase + outlineWidthAdd;
            rightOutline.startWidth = rightOutline.endWidth = laserWidthBase + outlineWidthAdd;
            leftOutline.sortingLayerName = rightOutline.sortingLayerName = "Foreground";
            leftOutline.sortingOrder = rightOutline.sortingOrder = 9;
            activeSkillObjects.Add(leftOutline.gameObject);
            activeSkillObjects.Add(rightOutline.gameObject);
        }

        // 클라이맥스 줌
        StartCoroutine(CamZoomPunch());

        float phase = 0f;
        float elapsed = 0f, timer = 0f;
        int patIdx = 0;
        string[] patSeq = { "X", "Y", "X", "Y" };

        while (elapsed < laserActiveDuration)
        {
            elapsed += Time.deltaTime;
            timer += Time.deltaTime;

            phase += Time.deltaTime / period;
            float wv = Waveform01(phase, waveform);
            if (edgeHoldRatio > 0f) wv = ApplyEdgeHold(wv, edgeHoldRatio);

            float inward = Mathf.Lerp(-amp, amp, wv);
            float lx = centerX - halfSep0 + inward;
            float rx = centerX + halfSep0 - inward;

            if (clampToBounds)
            {
                float minX = b.min.x + clampMargin;
                float maxX = b.max.x - clampMargin;
                lx = Mathf.Clamp(lx, minX, maxX);
                rx = Mathf.Clamp(rx, minX, maxX);
            }

            Vector3 curL = new(lx, transform.position.y, 0f);
            Vector3 curR = new(rx, transform.position.y, 0f);

            SetLineVertical(leftLR, curL, topY);
            SetLineVertical(rightLR, curR, topY);
            if (laserOutline)
            {
                SetLineVertical(leftOutline, curL, topY);
                SetLineVertical(rightOutline, curR, topY);
            }

            // 폭 펄스
            if (laserWidthPulse)
            {
                float beat = Mathf.PingPong(elapsed, BeatPeriod) / BeatPeriod;
                float width = laserWidthBase + Mathf.Lerp(0f, laserWidthPulseAmt, beat);
                leftLR.startWidth = leftLR.endWidth = width;
                rightLR.startWidth = rightLR.endWidth = width;
                if (laserOutline)
                {
                    leftOutline.startWidth = leftOutline.endWidth = width + outlineWidthAdd;
                    rightOutline.startWidth = rightOutline.endWidth = width + outlineWidthAdd;
                }
            }

            // 텍스처 스크롤(머티리얼 있을 때만)
            if (laserMaterial != null)
            {
                var mat = leftLR.material;
                if (mat && mat.HasProperty("_MainTex"))
                {
                    Vector2 off = mat.mainTextureOffset;
                    off.x += Time.deltaTime * 0.6f;
                    mat.mainTextureOffset = off;
                    rightLR.material.mainTextureOffset = off;
                    if (laserOutline)
                    {
                        leftOutline.material.mainTextureOffset = off;
                        rightOutline.material.mainTextureOffset = off;
                    }
                }
            }

            CheckLaserHit(leftLR);
            CheckLaserHit(rightLR);

            // 보조 탄막
            if (timer >= fireInterval * Mathf.Max(0.6f, 1f - 0.2f * intensity))
            {
                if (bulletPrefab)
                {
                    string p = patSeq[patIdx % patSeq.Length];
                    Vector2[] dirs = p == "X"
                        ? new[] { new Vector2(1, 1), new Vector2(-1, 1), new Vector2(1, -1), new Vector2(-1, -1) }
                        : new[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

                    foreach (var d in dirs)
                    {
                        float spd = bulletSpeed * (0.8f + 0.3f * intensity);
                        SpawnBullet(transform.position, d.normalized * spd);
                    }
                    patIdx++;
                    FlashSprite(); DoHitStop(); ShakeCamera();
                }
                timer = 0f;
            }

            yield return null;
        }

        Destroy(leftLaser); Destroy(rightLaser);
        if (leftOutline) Destroy(leftOutline.gameObject);
        if (rightOutline) Destroy(rightOutline.gameObject);
        KillAllWarnLines();
        yield return StartCoroutine(SkillFinished());
    }

    // ===========================================================
    //           스킬 3: 검(양방향 회전) — 잔광 + 완화 방사탄
    // ===========================================================
    private IEnumerator SkillSwordPattern()
    {
        if (mapCollider == null)
        {
            Debug.LogWarning("mapCollider 미지정!");
            yield return StartCoroutine(SkillFinished());
            yield break;
        }

        float r = Mathf.Max(mapCollider.bounds.size.x, mapCollider.bounds.size.y) / 2f;
        Vector3 c = transform.position;
        float ang = swordStartAngle;

        // 예고(보라 라인 + 스프라이트 마법진)
        if (warnSwordPattern)
        {
            var warnA = CreateWarnLine("Warn_Sword_A", 1.6f, 9);
            var warnB = CreateWarnLine("Warn_Sword_B", 1.6f, 9);
            SpawnMagicCircleSprite(
                transform.position,
                magicCircleBaseScale * 0.9f,
                swordWarningDuration + 0.05f,
                magicCircleSpinSpeed * 1.2f,
                magicCircleColor,
                magicCirclePulseScale
            );

            float wtime = 0f;
            while (wtime < swordWarningDuration)
            {
                float accel = Mathf.SmoothStep(0.2f, 1.0f, wtime / swordWarningDuration);
                ang += swordRotateSpeed * 0.5f * accel * Time.deltaTime;

                float rad = ang * Mathf.Deg2Rad;
                Vector3 dir = new(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
                SetLineByDir(warnA, c, dir, r);
                SetLineByDir(warnB, c, -dir, r);

                wtime += Time.deltaTime;
                yield return null;
            }
            KillAllWarnLines();
        }

        var la = new GameObject("RotLaserA").AddComponent<LineRenderer>();
        var lb = new GameObject("RotLaserB").AddComponent<LineRenderer>();
        SetupLaser(la, Color.red); SetupLaser(lb, Color.red);
        la.sortingLayerName = lb.sortingLayerName = "Foreground";
        la.sortingOrder = lb.sortingOrder = 10;

        // 잔광
        var afters = new List<LineRenderer>();
        for (int i = 0; i < afterimageCount; i++)
        {
            var lr = new GameObject($"SwordAfter_{i}").AddComponent<LineRenderer>();
            SetupLaser(lr, new Color(1f, 0.3f, 0.9f, 0.5f));
            lr.startWidth = lr.endWidth = Mathf.Max(0.06f, laserWidthBase * 0.6f);
            lr.sortingLayerName = "Foreground";
            lr.sortingOrder = 9;
            afters.Add(lr);
            activeSkillObjects.Add(lr.gameObject);
        }

        // 클라이맥스 줌
        StartCoroutine(CamZoomPunch());

        float time = 0f;
        float swingDur = 360f / Mathf.Max(1f, swordRotateSpeed * (1f + 0.2f * intensity));
        while (time < swingDur)
        {
            ang += swordRotateSpeed * (1f + 0.2f * intensity) * Time.deltaTime;
            float rad = ang * Mathf.Deg2Rad;
            Vector3 da = new(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
            SetLineByDir(la, c, da, r);
            SetLineByDir(lb, c, -da, r);
            CheckLaserDamage(c, da, r);
            CheckLaserDamage(c, -da, r);

            // 잔광 업데이트
            for (int i = afters.Count - 1; i >= 0; i--)
            {
                float back = (i + 1) * 0.06f * r;
                SetLineByDir(afters[i], c, da, Mathf.Max(0f, r - back));
                Color col = afters[i].startColor;
                float a = Mathf.Clamp01(1f - (i + time / swingDur)) * 0.6f;
                col.a = a;
                afters[i].startColor = afters[i].endColor = col;
            }

            time += Time.deltaTime;
            yield return null;
        }

        Destroy(la.gameObject); Destroy(lb.gameObject);

        // 마무리 충격파(소형 방사탄) — 개수 축소 + 순차 발사(가불감 완화)
        int small = Mathf.RoundToInt(Mathf.Max(0f, endShockwaveBullets) * Mathf.Clamp01(endShockwaveCountScale));
        if (small > 0 && bulletPrefab)
        {
            float stepDeg = 360f / small;
            for (int i = 0; i < small; i++)
            {
                float rad = (stepDeg * i) * Mathf.Deg2Rad;
                Vector2 d = new(Mathf.Cos(rad), Mathf.Sin(rad));
                SpawnBullet(c, d * (bulletSpeed * 0.85f + 0.25f * intensity));
                if (endShockwaveStagger > 0f) yield return new WaitForSeconds(endShockwaveStagger);
            }
            FlashSprite(); DoHitStop(); ShakeCamera();
        }

        yield return StartCoroutine(SkillFinished());
    }

    // ===========================================================
    //                 스킬 4: 점프 후 원형탄
    // ===========================================================
    private IEnumerator SkillJumpAndShoot()
    {
        if (!enableJumpPattern) { yield return StartCoroutine(SkillFinished()); yield break; }

        Vector3 s = transform.position;
        Vector3 p = s + Vector3.up * jumpHeight;

        int count = Mathf.Max(1, jumpBulletCount);
        float step = 360f / count;

        if (warnJumpPattern)
        {
            for (int i = 0; i < count; i++) CreateWarnLine($"Warn_JumpBullet_{i}", 1.25f, 9);

            SpawnMagicCircleSprite(
                transform.position,
                magicCircleBaseScale * 0.8f,
                preWarnDuration + 0.05f,
                magicCircleSpinSpeed * 1.1f,
                magicCircleColor,
                magicCirclePulseScale
            );

            float warnElapsed = 0f;
            while (warnElapsed < preWarnDuration)
            {
                int idx = 0;
                foreach (var wl in _warnLines)
                {
                    float a = step * idx;
                    float rad = a * Mathf.Deg2Rad;
                    Vector3 dir = new(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
                    SetLineByDir(wl, s, dir, warningLengthScale);
                    idx++;
                }
                warnElapsed += Time.deltaTime;
                yield return null;
            }
        }

        yield return transform.DOMove(p, jumpDuration).SetEase(Ease.OutQuad).WaitForCompletion();
        yield return new WaitForSeconds(0.05f);
        yield return transform.DOMove(s, jumpDuration).SetEase(Ease.InQuad).WaitForCompletion();

        KillAllWarnLines();

        if (bulletPrefab)
        {
            for (int i = 0; i < count; i++)
            {
                float a = step * i * Mathf.Deg2Rad;
                Vector2 d = new(Mathf.Cos(a), Mathf.Sin(a));
                float spd = jumpBulletSpeed * (1f + 0.15f * intensity);
                SpawnBullet(s, d * spd);
            }
            FlashSprite(); DoHitStop(); ShakeCamera();
        }
        else
        {
            Debug.LogWarning("[MiddleBoss] bulletPrefab 없음 → 점프 후 탄 발사 스킵");
        }

        yield return StartCoroutine(SkillFinished());
    }

    // ===========================================================
    //               공통: 파형/레이저 대미지 체크
    // ===========================================================
    private float Waveform01(float t, SweepWaveform form)
    {
        float u = t - Mathf.Floor(t); // 0..1
        return (form == SweepWaveform.Sine)
            ? 0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * u)
            : 1f - Mathf.Abs(1f - 2f * u);
    }

    private float ApplyEdgeHold(float wv, float edge)
    {
        float hold = Mathf.Clamp(edge, 0f, 0.45f);
        if (hold <= 0f) return wv;
        if (wv > 1f - hold) return 1f;
        if (wv < hold) return 0f;
        float range = 1f - 2f * hold;
        float mid = (wv - hold) / range;
        return mid * mid * (3f - 2f * mid); // S-curve
    }

    private void SetupLaser(LineRenderer lr, Color c)
    {
        lr.positionCount = 2;
        lr.startWidth = lr.endWidth = laserWidthBase;
        lr.material = laserMaterial != null ? laserMaterial : new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = c;
        lr.useWorldSpace = true;
    }

    private void CheckLaserHit(LineRenderer lr)
    {
        var hits = Physics2D.LinecastAll(lr.GetPosition(0), lr.GetPosition(1), LayerMask.GetMask("Player"));
        foreach (var h in hits)
            if (h.collider && h.collider.CompareTag("Player"))
                GameManager.Instance?.playerDamaged?.TakeDamage(laserDamage, transform.position);
    }

    private void CheckLaserDamage(Vector3 start, Vector3 dir, float dist)
    {
        var h = Physics2D.Raycast(start, dir, dist, LayerMask.GetMask("Player"));
        if (h.collider && h.collider.CompareTag("Player"))
            GameManager.Instance?.playerDamaged?.TakeDamage(laserDamage, transform.position);
    }

    // ===========================================================
    //                   종료/정리 + 인텐시티
    // ===========================================================
    private IEnumerator SkillFinished()
    {
        KillAllWarnLines();
        intensity = Mathf.Min(intensityMax, intensity + intensityPerSkill);
        yield return new WaitForSeconds(1f);
        isSkillPlaying = false;
    }

    public void ClearAllSkillObjects()
    {
        foreach (var o in activeSkillObjects)
            if (o) Destroy(o);
        activeSkillObjects.Clear();
        KillAllWarnLines();
    }

    public void SetDead()
    {
        isLive = false;
        ClearAllSkillObjects();
    }

    void OnDisable()
    {
        KillAllWarnLines();
        if (camT) camT.position = camOrigin;
    }
}
