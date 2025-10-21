using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// MiddleBoss
/// - 기존 패턴/경고/레이저/몹 스폰 로직 유지
/// - ★ EnemyAnimation과 연동:
///     · 시작 시 Entry(등장) 재생
///     · 각 패턴: PatternStart → PatternLoop(진행 중) → PatternEnd
///     · SetDead(): Death 재생 후 파괴(옵션)
/// </summary>
public class MiddleBoss : MonoBehaviour
{
    // 내부 마커(소환 몹 추적)
    private class BossSpawnedMob : MonoBehaviour
    {
        public MiddleBoss owner;
        public bool IsOwnerAlive() => owner != null;
    }

    private bool isLive = true;
    private SpriteRenderer spriter;
    private EnemyAnimation enemyAnimation;

    // 패턴/타이밍
    [Header("패턴 타이밍")]
    public float skillInterval = 4f;
    private float skillTimer = 0f;
    private bool isSkillPlaying = false;

    // 인텐시티
    [Range(0f, 3f)] public float intensity = 0f;
    public float intensityPerSkill = 0.15f;
    public float intensityMax = 2.0f;

    // 리듬
    [Header("연출 리듬(BPM)")]
    public float tempoBPM = 120f;
    private float BeatPeriod => 60f / Mathf.Max(1f, tempoBPM);

    // 온/오프
    [Header("패턴 온/오프")]
    public bool enableBulletCircle = true;
    public bool enableLaserPattern = true;
    public bool enableSwordPattern = true;
    public bool enableJumpPattern = false;

    private enum PatternType { None, BulletCircle, Laser, Sword, Jump }
    private PatternType _lastPattern = PatternType.None;

    // 경고 플래그
    [Header("경고 표시")]
    public bool warnBulletCircle = false;
    public bool warnLaserPattern = true;
    public bool warnSwordPattern = true;
    public bool warnJumpPattern = false;

    // 탄막
    [Header("탄막")]
    public GameObject bulletPrefab;
    public int bulletsPerWave = 24;
    public float bulletSpeed = 6f;
    public float bulletFireInterval = 0.35f;
    public float bulletPatternDuration = 6.0f;
    public bool useGaps = false;
    [Range(0f, 1f)] public float bulletSpeedScale = 1.0f;
    [Range(0f, 1f)] public float spinAdd = 0.15f;
    [Range(0, 6)] public int gapCount = 2;
    [Range(0f, 1f)] public float gapWidthRatio = 0.15f;

    // 레이저
    [Header("레이저")]
    public Collider2D mapCollider;
    public int laserDamage = 100;
    public Material laserMaterial;

    [Header("레이저 시작점")]
    public float leftLaserOffsetX = -2f;
    public float rightLaserOffsetX = 2f;

    public enum SweepWaveform { Sine, Triangle }
    [Header("스윕")]
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

    [Header("폭 펄스")]
    public bool laserWidthPulse = true;
    public float laserWidthBase = 0.18f;
    public float laserWidthPulseAmt = 0.12f;

    [Header("레이저 컬러/알파 사이클(3색)")]
    public bool useLaserColorCycle = true;
    public Color[] cycleColors = new Color[3] {
        new Color(1f, 0.35f, 0.55f, 1f),
        new Color(1f, 0.95f, 0.45f, 1f),
        new Color(0.9f, 0.95f, 1f, 1f)
    };
    public float[] alphaLevels = new float[3] { 0.35f, 0.7f, 1.0f };
    public float alphaCycleSeconds = 1.2f;
    public float[] colorStops = new float[3] { 0.0f, 0.5f, 1.0f };

    [Header("레이저 아웃라인(2겹)")]
    public bool laserOutline = true;
    public float outlineWidthAdd = 0.08f;
    public Color outlineColor = new Color(1f, 0.6f, 1f, 0.35f);

    // 검
    [Header("검")]
    public float swordRotateSpeed = 360f;
    public float swordStartAngle = 180f;
    public float swordWarningDuration = 1f;
    public int afterimageCount = 3;
    public float endShockwaveBullets = 12f;
    [Range(0f, 1f)] public float endShockwaveCountScale = 0.6f;
    [Range(0f, 0.08f)] public float endShockwaveStagger = 0.03f;

    // 점프
    [Header("점프 패턴")]
    public float jumpHeight = 5f;
    public float jumpDuration = 0.5f;
    public int jumpBulletCount = 8;
    public float jumpBulletSpeed = 6f;

    // 경고 라인
    [Header("경고 라인 공통")]
    public float preWarnDuration = 1.0f;
    public float warnLineWidth = 0.28f;
    public Color warnLineColor = new Color(0.68f, 0.45f, 1f, 0.95f);
    public Material warnLineMaterial;
    public bool keepWarnDuringTransition = false;
    public float warnTransitionTime = 0.15f;

    // 전환 FX
    [Header("전환 FX(경고→본패턴) - 무프리팹")]
    public bool fxScreenFlash = true;
    public Color screenFlashColor = new Color(1f, 1f, 1f, 0.35f);
    [Range(0.05f, 0.6f)] public float screenFlashIn = 0.06f;
    [Range(0.05f, 0.6f)] public float screenFlashOut = 0.18f;

    public bool fxShockwaveRing = true;
    public float shockwaveRadiusFrom = 0.4f;
    public float shockwaveRadiusTo = 2.4f;
    public float shockwaveSeconds = 0.22f;
    public Color shockwaveColor = new Color(1f, 0.9f, 1f, 0.85f);
    public int shockwaveSegments = 48;

    public bool fxLaserAfterimageBurst = true;
    [Range(1, 4)] public int laserAfterimageCount = 2;
    [Range(0.02f, 0.5f)] public float laserAfterimageFade = 0.18f;

    // 감각 강화
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

    [Header("카메라 줌")]
    public bool zoomOnClimax = true;
    public float zoomSize = 4.8f;
    public float zoomInDur = 0.12f;
    public float zoomHold = 0.10f;
    public float zoomOutDur = 0.15f;

    // 레이저 중 몹 스폰
    [Header("레이저 중 몹 스폰")]
    public bool spawnMobsDuringLaser = true;
    public bool replaceBulletsWithMobs = false;
    public List<GameObject> mobPrefabs = new List<GameObject>();
    public float mobSpawnInterval = 2.0f;
    public Vector2Int mobSpawnCountRange = new Vector2Int(1, 2);
    public float mobSpawnMargin = 0.6f;
    public float mobMinDistanceFromPlayer = 2.5f;
    public int mobMaxAlive = 6;
    public Transform mobParent;
    public LayerMask mobBlockMask;
    private readonly List<GameObject> _aliveMobs = new();

    // 컬러 사이클 캐시
    private Gradient _cycleGradientLaser;
    private Gradient _cycleGradientOutline;
    private GradientColorKey[] _laserColorKeys = new GradientColorKey[3];
    private GradientAlphaKey[] _alphaKeysShared = new GradientAlphaKey[3];
    private GradientColorKey[] _outlineColorKeys = new GradientColorKey[3];

    // 디버그
    [Header("디버그")]
    public bool debugForceBulletCircle = false;
    public bool debugForceLaserPatternNow = false;
    public bool debugSpawnOneOnStart = false;
    public bool verboseSpawnLog = true;
    public bool forceSpawnIfBlocked = true;

    private readonly List<GameObject> activeSkillObjects = new();
    private readonly List<LineRenderer> _warnLines = new();

    // 내부 캐시
    private Transform camT;
    private Vector3 camOrigin;
    private Camera cam;

    // ★ 추가: 죽음 연출 후 파괴 지연
    [Header("죽음 파괴 지연(초) - 0이면 자동 계산")]
    public float deathDestroyDelay = 0f;

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

        InitCycleGradients();

        if (!mobParent)
        {
            var g = new GameObject($"MobParent_{name}");
            mobParent = g.transform;
        }

        // ★ 시작 시 등장 애니메이션 (EnemyAnimation에서 Entry가 설정되어 있으면 자동 Idle 복귀)
        if (enemyAnimation != null && enemyAnimation.GetEstimatedDuration(EnemyAnimation.State.Entry) > 0f)
        {
            enemyAnimation.PlayAnimation(EnemyAnimation.State.Entry);
        }

        if (debugSpawnOneOnStart)
        {
            debugSpawnOneOnStart = false;
            if (mapCollider && mobPrefabs != null && mobPrefabs.Count > 0 && mobPrefabs[0] != null)
            {
                var b = mapCollider.bounds;
                if (TryGetSpawnPoint(b, out var p, 30))
                {
                    var m = SpawnMobAt(p);
                    Debug.Log(m ? $"[MiddleBoss] 테스트 스폰 OK @ {p}" : "[MiddleBoss] 테스트 스폰 실패");
                }
            }
        }
    }

    void Update()
    {
        if (debugForceLaserPatternNow && !isSkillPlaying)
        {
            debugForceLaserPatternNow = false;
            isSkillPlaying = true;
            StartSafe(SkillLaserPattern());
            return;
        }

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
            UseRandomSkill_NoRepeat();
        }
    }

    // ====== 동일 패턴 연속 방지 ======
    private void UseRandomSkill_NoRepeat()
    {
        isSkillPlaying = true;

        var options = new List<PatternType>();
        if (enableBulletCircle) options.Add(PatternType.BulletCircle);
        if (enableLaserPattern) options.Add(PatternType.Laser);
        if (enableSwordPattern) options.Add(PatternType.Sword);
        if (enableJumpPattern) options.Add(PatternType.Jump);

        if (options.Count == 0)
        {
            Debug.LogWarning("활성화된 보스 패턴이 없습니다.");
            isSkillPlaying = false;
            return;
        }

        var filtered = options.FindAll(p => p != _lastPattern);
        if (filtered.Count == 0) filtered = options;

        var choice = filtered[UnityEngine.Random.Range(0, filtered.Count)];
        _lastPattern = choice;

        switch (choice)
        {
            case PatternType.BulletCircle: StartSafe(SkillBulletCircle()); break;
            case PatternType.Laser: StartSafe(SkillLaserPattern()); break;
            case PatternType.Sword: StartSafe(SkillSwordPattern()); break;
            case PatternType.Jump: StartSafe(SkillJumpAndShoot()); break;
        }
    }

    private void StartSafe(IEnumerator routine) => StartCoroutine(CoSafe(routine));
    private IEnumerator CoSafe(IEnumerator routine)
    {
        bool finished = false;
        while (true)
        {
            object current;
            try
            {
                if (!routine.MoveNext()) { finished = true; break; }
                current = routine.Current;
            }
            catch (Exception e)
            {
                Debug.LogError($"[MiddleBoss] 스킬 실행 중 예외: {e}");
                break;
            }
            yield return current;
        }
        if (!finished)
        {
            ClearAllSkillObjects();
            KillAllSpawnedMobs();
            isSkillPlaying = false;
        }
    }

    // ===================== 사이클/머티리얼 =====================
    private void InitCycleGradients()
    {
        for (int i = 0; i < 3; i++)
        {
            var colL = (cycleColors != null && cycleColors.Length > i) ? cycleColors[i] : Color.white;
            float stop = (colorStops != null && colorStops.Length > i) ? Mathf.Clamp01(colorStops[i]) : (i == 0 ? 0f : (i == 1 ? 0.5f : 1f));
            _laserColorKeys[i] = new GradientColorKey(new Color(colL.r, colL.g, colL.b, 1f), stop);

            Color outBase = outlineColor;
            float t = (i == 0) ? 0.9f : (i == 1 ? 1.0f : 0.8f);
            Color outC = new Color(
                Mathf.Lerp(outBase.r, 1f, 0.15f * t),
                Mathf.Lerp(outBase.g, 0.8f, 0.1f * t),
                Mathf.Lerp(outBase.b, 1f, 0.2f * t),
                1f
            );
            _outlineColorKeys[i] = new GradientColorKey(outC, stop);
        }
        for (int i = 0; i < 3; i++)
            _alphaKeysShared[i] = new GradientAlphaKey(1f,
                (colorStops != null && colorStops.Length > i) ? Mathf.Clamp01(colorStops[i]) : (i == 0 ? 0f : (i == 1 ? 0.5f : 1f)));

        _cycleGradientLaser = new Gradient();
        _cycleGradientOutline = new Gradient();
        UpdateCycleGradientAlpha(0f);
    }

    private float EvalCyclingAlpha(float t)
    {
        if (!useLaserColorCycle || alphaCycleSeconds <= 0.01f) return 1f;
        float cycle = Mathf.Repeat(t / Mathf.Max(0.01f, alphaCycleSeconds), 1f) * 3f;
        int i0 = Mathf.FloorToInt(cycle);
        int i1 = (i0 + 1) % 3;
        float f = cycle - i0;

        float a0 = (alphaLevels != null && alphaLevels.Length > i0) ? Mathf.Clamp01(alphaLevels[i0]) : 1f;
        float a1 = (alphaLevels != null && alphaLevels.Length > i1) ? Mathf.Clamp01(alphaLevels[i1]) : 1f;

        return Mathf.Lerp(a0, a1, f);
    }

    private void UpdateCycleGradientAlpha(float t)
    {
        float a = EvalCyclingAlpha(t);
        for (int i = 0; i < 3; i++) _alphaKeysShared[i].alpha = a;
        _cycleGradientLaser.SetKeys(_laserColorKeys, _alphaKeysShared);
        _cycleGradientOutline.SetKeys(_outlineColorKeys, _alphaKeysShared);
    }

    private void SetMaterialAlpha(LineRenderer lr, float a)
    {
        var m = lr.material; if (m == null) return;
        if (m.HasProperty("_Color")) { var c = m.color; c.a = a; m.color = c; }
        if (m.HasProperty("_BaseColor")) { var c2 = m.GetColor("_BaseColor"); c2.a = a; m.SetColor("_BaseColor", c2); }
        if (m.HasProperty("_TintColor")) { var c3 = m.GetColor("_TintColor"); c3.a = a; m.SetColor("_TintColor", c3); }
    }

    private void ApplyCycleGradientTo(LineRenderer lr, bool isOutline, float t)
    {
        if (!useLaserColorCycle || lr == null) return;
        UpdateCycleGradientAlpha(t);
        lr.colorGradient = isOutline ? _cycleGradientOutline : _cycleGradientLaser;
        float a = EvalCyclingAlpha(t);
        var sc = lr.startColor; sc.a = a; lr.startColor = sc;
        var ec = lr.endColor; ec.a = a; lr.endColor = ec;
        SetMaterialAlpha(lr, a);
    }

    // ===================== 경고/라인 유틸 =====================
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
        lr.material = warnLineMaterial != null ? new Material(warnLineMaterial) : new Material(Shader.Find("Sprites/Default"));
        lr.useWorldSpace = true;
        lr.sortingLayerName = "Foreground";
        lr.sortingOrder = order;
        ApplyWarnColor(lr, warnLineColor);
        _warnLines.Add(lr);
        activeSkillObjects.Add(go);
        return lr;
    }
    private LineRenderer CreateRingLR(string name, Vector3 center, float radius, int segments, float width, Color color, int order = 100)
    {
        segments = Mathf.Max(8, segments);
        var go = new GameObject(name);
        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = segments + 1;
        lr.startWidth = lr.endWidth = Mathf.Max(0.001f, width);
        lr.material = new Material(Shader.Find("Sprites/Default"));
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
        ApplyWarnColor(lr, color);
        activeSkillObjects.Add(go);
        return lr;
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

    // ===================== 탄환/스폰 유틸 =====================
    private GameObject SpawnBullet(Vector2 pos, Vector2 vel)
    {
        if (!bulletPrefab)
        {
            Debug.LogWarning("[MiddleBoss] bulletPrefab 없음 → 탄 스킵");
            return null;
        }
        var go = Instantiate(bulletPrefab, pos, Quaternion.identity);
        var rb = go.GetComponent<Rigidbody2D>();
        if (!rb) rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.freezeRotation = true;
        rb.linearVelocity = vel;
        activeSkillObjects.Add(go);
        return go;
    }

    // ===================== 전환 FX =====================
    private Canvas _fxCanvas; private Image _fxFlashImg;
    private void EnsureFXCanvas()
    {
        if (_fxCanvas != null && _fxFlashImg != null) return;
        var canvasGO = new GameObject("FXCanvas");
        _fxCanvas = canvasGO.AddComponent<Canvas>();
        _fxCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(canvasGO);

        var imgGO = new GameObject("Flash");
        imgGO.transform.SetParent(canvasGO.transform, false);
        _fxFlashImg = imgGO.AddComponent<Image>();
        _fxFlashImg.color = new Color(0, 0, 0, 0);
        _fxFlashImg.rectTransform.anchorMin = Vector2.zero;
        _fxFlashImg.rectTransform.anchorMax = Vector2.one;
        _fxFlashImg.rectTransform.offsetMin = Vector2.zero;
        _fxFlashImg.rectTransform.offsetMax = Vector2.zero;
    }
    private void DoScreenFlash()
    {
        if (!fxScreenFlash) return;
        EnsureFXCanvas();
        _fxFlashImg.DOKill();
        _fxFlashImg.color = new Color(screenFlashColor.r, screenFlashColor.g, screenFlashColor.b, 0f);
        _fxFlashImg.DOFade(screenFlashColor.a, screenFlashIn).SetEase(Ease.OutQuad)
            .OnComplete(() => _fxFlashImg.DOFade(0f, screenFlashOut).SetEase(Ease.InQuad));
    }
    private void SpawnShockwaveRingLR(Vector3 worldPos)
    {
        if (!fxShockwaveRing) return;
        var ring = CreateRingLR("ShockwaveRingLR", worldPos, shockwaveRadiusFrom, shockwaveSegments, 0.18f, shockwaveColor, 120);
        var c = shockwaveColor; ring.startColor = ring.endColor = c;

        float t = 0f;
        DOTween.To(() => t, v =>
        {
            t = v;
            float r = Mathf.Lerp(shockwaveRadiusFrom, shockwaveRadiusTo, t);
            for (int i = 0; i <= shockwaveSegments; i++)
            {
                float u = (float)i / shockwaveSegments;
                float ang = u * Mathf.PI * 2f;
                Vector3 p = worldPos + new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * r;
                ring.SetPosition(i, p);
            }
            Color cc = ring.startColor; cc.a = Mathf.Lerp(shockwaveColor.a, 0f, t);
            ring.startColor = ring.endColor = cc;
            SetMaterialAlpha(ring, cc.a);
        }, 1f, shockwaveSeconds).SetEase(Ease.OutCubic)
          .OnComplete(() => { if (ring) Destroy(ring.gameObject); });
    }
    private void BurstLaserAfterimages(LineRenderer src)
    {
        if (!fxLaserAfterimageBurst || src == null) return;
        for (int i = 0; i < laserAfterimageCount; i++)
        {
            var g = new GameObject($"LaserGhost_{i}");
            var lr = g.AddComponent<LineRenderer>();
            lr.positionCount = src.positionCount;
            lr.SetPosition(0, src.GetPosition(0));
            lr.SetPosition(1, src.GetPosition(1));
            lr.startWidth = lr.endWidth = src.startWidth * Mathf.Lerp(0.85f, 0.65f, (float)i / Mathf.Max(1, laserAfterimageCount - 1));
            lr.material = new Material(src.material);
            lr.colorGradient = src.colorGradient;
            lr.useWorldSpace = true;
            lr.sortingLayerName = src.sortingLayerName;
            lr.sortingOrder = src.sortingOrder - 1;

            float a0 = 0.75f - 0.2f * i;
            Color sc = src.startColor; sc.a = a0;
            Color ec = src.endColor; ec.a = a0;
            lr.startColor = sc; lr.endColor = ec;
            SetMaterialAlpha(lr, a0);

            DOTween.To(() => a0, a =>
            {
                var cc0 = lr.startColor; cc0.a = a; lr.startColor = cc0;
                var cc1 = lr.endColor; cc1.a = a; lr.endColor = cc1;
                SetMaterialAlpha(lr, a);
            }, 0f, laserAfterimageFade).SetEase(Ease.InQuad)
              .OnComplete(() => Destroy(g));
        }
    }
    private void TriggerPatternClimaxFX(Vector3 pos, params LineRenderer[] mainLines)
    {
        DoScreenFlash();
        DoHitStop();
        ShakeCamera();
        SpawnShockwaveRingLR(pos);
        if (mainLines != null) foreach (var lr in mainLines) BurstLaserAfterimages(lr);
    }

    // ===================== 감각 유틸 =====================
    private void DoHitStop()
    {
        if (!useHitStop) return;
        DOTween.Kill("HitStopTS");
        DOTween.To(() => Time.timeScale, v => Time.timeScale = v, 0.0001f + hitStopScale, 0.0f).SetId("HitStopTS").SetUpdate(true);
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
        camT.DOShakePosition(shakeDuration, shakeAmplitude, 12, 90f, false, true)
            .OnComplete(() => camT.position = camOrigin);
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
    private void FlashSprite()
    {
        if (!flashSpriteOnFire || spriter == null) return;
        Color baseC = spriter.color;
        spriter.DOKill();
        spriter.DOColor(flashColor, flashDuration).OnComplete(() =>
            spriter.DOColor(baseC, flashDuration));
    }

    // ===================== 스킬 1: 회전 탄막 =====================
    private IEnumerator SkillBulletCircle()
    {
        // ★ 패턴 시작 애니
        enemyAnimation?.PlayAnimation(EnemyAnimation.State.PatternStart);

        int count = Mathf.Max(1, bulletsPerWave);
        float step = 360f / count;

        float duration = bulletPatternDuration + intensity * 0.6f;
        float fireIntervalLocal = Mathf.Clamp(bulletFireInterval - 0.05f * intensity, 0.18f, 0.6f);

        float spinDir = Mathf.Sign(UnityEngine.Random.Range(-1f, 1f) + 0.01f);
        float tElapsed = 0f;

        // 경고
        float warnOffset = 0f;
        if (warnBulletCircle)
        {
            float warnTime = 0f;
            float warnRadius = 3.0f;
            for (int i = 0; i < count; i++) CreateWarnLine($"Warn_BulletDir_{i}", 1.35f, 9);

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
                warnOffset += (30f * beat) * Time.deltaTime * spinDir;
                warnTime += Time.deltaTime;
                yield return null;
            }
            if (!keepWarnDuringTransition) KillAllWarnLines();
            else StartCoroutine(_CoDelayedKillWarnLines(Mathf.Max(0f, warnTransitionTime)));
        }

        // 본 패턴 돌입 FX + ★ 패턴 루프 애니
        TriggerPatternClimaxFX(transform.position);
        enemyAnimation?.PlayAnimation(EnemyAnimation.State.PatternLoop);

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

            spinOffset += (32f * spinMul) * spinDir;
            gapPhase += step * 0.22f * spinMul;

            tElapsed += fireIntervalLocal;
            yield return new WaitForSeconds(fireIntervalLocal);
        }

        // ★ 패턴 종료 애니
        enemyAnimation?.PlayAnimation(EnemyAnimation.State.PatternEnd);

        yield return StartCoroutine(SkillFinished());
    }

    private IEnumerator _CoDelayedKillWarnLines(float delay)
    {
        yield return new WaitForSeconds(delay);
        KillAllWarnLines();
    }

    // ===================== 스킬 2: 교차 스윕 레이저 =====================
    private IEnumerator SkillLaserPattern()
    {
        // ★ 패턴 시작 애니
        enemyAnimation?.PlayAnimation(EnemyAnimation.State.PatternStart);

        if (mapCollider == null)
        {
            Debug.LogWarning("mapCollider 미지정!");
            // 종료 애니로 안전하게 마무리
            enemyAnimation?.PlayAnimation(EnemyAnimation.State.PatternEnd);
            yield return StartCoroutine(SkillFinished());
            yield break;
        }

        Bounds b = mapCollider.bounds;

        Vector3 leftBase = transform.position + new Vector3(leftLaserOffsetX, 0);
        Vector3 rightBase = transform.position + new Vector3(rightLaserOffsetX, 0);

        float over = Mathf.Max(0f, laserOverrun);
        float halfY = b.extents.y + over;
        float centerX = (leftBase.x + rightBase.x) * 0.5f;
        float halfSep0 = Mathf.Abs(rightBase.x - leftBase.x) * 0.5f;

        float amp = Mathf.Max(0f, crossingAmplitudeUnits) * (1f + 0.15f * intensity);
        float hz = Mathf.Max(0.1f, crossingHz * (1f + 0.12f * intensity));
        float period = 1f / hz;

        // 경고
        float tWarn = 0f;
        if (warnLaserPattern)
        {
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

                SetLineVertical(warnL, curL, halfY);
                SetLineVertical(warnR, curR, halfY);

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

        // 본 레이저 + FX + ★ 패턴 루프 애니
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

        ApplyCycleGradientTo(leftLR, false, 0f);
        ApplyCycleGradientTo(rightLR, false, 0f);
        if (laserOutline)
        {
            ApplyCycleGradientTo(leftOutline, true, 0f);
            ApplyCycleGradientTo(rightOutline, true, 0f);
        }
        TriggerPatternClimaxFX(transform.position, leftLR, rightLR);
        StartCoroutine(CamZoomPunch());

        // ★ 루프 진입
        enemyAnimation?.PlayAnimation(EnemyAnimation.State.PatternLoop);

        float phase = 0f;
        float elapsed = 0f;
        float bulletTimer = 0f;
        float mobTimer = 0f;

        int patIdx = 0;
        string[] patSeq = { "X", "Y", "X", "Y" };

        while (elapsed < laserActiveDuration)
        {
            float dt = Time.deltaTime;
            elapsed += dt;
            bulletTimer += dt;
            mobTimer += dt;

            float wv = Waveform01(phase += dt / period, waveform);
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

            SetLineVertical(leftLR, curL, b.extents.y + Mathf.Max(0f, laserOverrun));
            SetLineVertical(rightLR, curR, b.extents.y + Mathf.Max(0f, laserOverrun));
            if (laserOutline)
            {
                SetLineVertical(leftOutline, curL, b.extents.y + Mathf.Max(0f, laserOverrun));
                SetLineVertical(rightOutline, curR, b.extents.y + Mathf.Max(0f, laserOverrun));
            }

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

            if (laserMaterial != null)
            {
                var mat = leftLR.material;
                if (mat && mat.HasProperty("_MainTex"))
                {
                    Vector2 off = mat.mainTextureOffset;
                    off.x += dt * 0.6f;
                    mat.mainTextureOffset = off;
                    rightLR.material.mainTextureOffset = off;
                    if (laserOutline)
                    {
                        leftOutline.material.mainTextureOffset = off;
                        rightOutline.material.mainTextureOffset = off;
                    }
                }
            }

            if (useLaserColorCycle)
            {
                ApplyCycleGradientTo(leftLR, false, elapsed);
                ApplyCycleGradientTo(rightLR, false, elapsed);
                if (laserOutline)
                {
                    ApplyCycleGradientTo(leftOutline, true, elapsed);
                    ApplyCycleGradientTo(rightOutline, true, elapsed);
                }
            }

            CheckLaserHit(leftLR);
            CheckLaserHit(rightLR);

            if (!replaceBulletsWithMobs)
            {
                float bulletGate = fireInterval * Mathf.Max(0.6f, 1f - 0.2f * intensity);
                if (bulletTimer >= bulletGate)
                {
                    bulletTimer = 0f;
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
                }
            }

            if (spawnMobsDuringLaser && mobTimer >= mobSpawnInterval)
            {
                mobTimer = 0f;
                TrimDeadMobs();

                if (_aliveMobs.Count >= mobMaxAlive)
                {
                    if (verboseSpawnLog) Debug.Log($"[MiddleBoss] 스폰 보류: MaxAlive({_aliveMobs.Count}/{mobMaxAlive})");
                }
                else if (mobPrefabs == null || mobPrefabs.Count == 0)
                {
                    if (verboseSpawnLog) Debug.LogWarning("[MiddleBoss] 스폰 보류: mobPrefabs 비어있음");
                }
                else
                {
                    int minC = Mathf.Max(0, mobSpawnCountRange.x);
                    int maxC = Mathf.Max(minC, mobSpawnCountRange.y);
                    int spawnCount = UnityEngine.Random.Range(minC, maxC + 1);

                    if (verboseSpawnLog) Debug.Log($"[MiddleBoss] 스폰 시도: {spawnCount}개");

                    for (int i = 0; i < spawnCount; i++)
                    {
                        if (_aliveMobs.Count >= mobMaxAlive) break;

                        if (TryGetSpawnPoint(b, out var pos))
                        {
                            var mob = SpawnMobAt(pos);
                            if (mob != null)
                            {
                                var t = mob.transform;
                                t.localScale = Vector3.one * 0.8f;
                                t.DOScale(1f, 0.18f).SetEase(Ease.OutBack, overshoot: 1.6f);
                            }
                        }
                    }

                    FlashSprite();
                    ShakeCamera();
                }
            }

            yield return null;
        }

        Destroy(leftLaser); Destroy(rightLaser);
        if (laserOutline)
        {
            if (leftOutline) Destroy(leftOutline.gameObject);
            if (rightOutline) Destroy(rightOutline.gameObject);
        }
        KillAllWarnLines();

        // ★ 패턴 종료 애니
        enemyAnimation?.PlayAnimation(EnemyAnimation.State.PatternEnd);

        yield return StartCoroutine(SkillFinished());
    }

    // ===================== 스킬 3: 검 회전 =====================
    private IEnumerator SkillSwordPattern()
    {
        // ★ 패턴 시작 애니
        enemyAnimation?.PlayAnimation(EnemyAnimation.State.PatternStart);

        if (mapCollider == null)
        {
            Debug.LogWarning("mapCollider 미지정!");
            enemyAnimation?.PlayAnimation(EnemyAnimation.State.PatternEnd);
            yield return StartCoroutine(SkillFinished());
            yield break;
        }

        float r = Mathf.Max(mapCollider.bounds.size.x, mapCollider.bounds.size.y) / 2f;
        Vector3 c = transform.position;
        float ang = swordStartAngle;

        if (warnSwordPattern)
        {
            var warnA = CreateWarnLine("Warn_Sword_A", 1.6f, 9);
            var warnB = CreateWarnLine("Warn_Sword_B", 1.6f, 9);

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

            if (!keepWarnDuringTransition) KillAllWarnLines();
            else StartCoroutine(_CoDelayedKillWarnLines(Mathf.Max(0f, warnTransitionTime)));
        }

        // 본 패턴 + FX + ★ 루프 진입
        var la = new GameObject("RotLaserA").AddComponent<LineRenderer>();
        var lb = new GameObject("RotLaserB").AddComponent<LineRenderer>();
        SetupLaser(la, Color.red); SetupLaser(lb, Color.red);
        la.sortingLayerName = lb.sortingLayerName = "Foreground";
        la.sortingOrder = lb.sortingOrder = 10;

        ApplyCycleGradientTo(la, false, 0f);
        ApplyCycleGradientTo(lb, false, 0f);
        TriggerPatternClimaxFX(transform.position, la, lb);
        StartCoroutine(CamZoomPunch());

        enemyAnimation?.PlayAnimation(EnemyAnimation.State.PatternLoop);

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

            if (useLaserColorCycle)
            {
                ApplyCycleGradientTo(la, false, time);
                ApplyCycleGradientTo(lb, false, time);
            }

            time += Time.deltaTime;
            yield return null;
        }

        Destroy(la.gameObject); Destroy(lb.gameObject);

        int small = Mathf.RoundToInt(Mathf.Max(0f, endShockwaveBullets) * Mathf.Clamp01(endShockwaveCountScale));
        if (small > 0 && bulletPrefab)
        {
            float stepDeg = 360f / small;
            for (int i = 0; i < small; i++)
            {
                float rad2 = (stepDeg * i) * Mathf.Deg2Rad;
                Vector2 d = new(Mathf.Cos(rad2), Mathf.Sin(rad2));
                SpawnBullet(c, d * (bulletSpeed * 0.85f + 0.25f * intensity));
                if (endShockwaveStagger > 0f) yield return new WaitForSeconds(endShockwaveStagger);
            }
            FlashSprite(); DoHitStop(); ShakeCamera();
        }

        // ★ 패턴 종료 애니
        enemyAnimation?.PlayAnimation(EnemyAnimation.State.PatternEnd);

        yield return StartCoroutine(SkillFinished());
    }

    // ===================== 스킬 4: 점프 후 원형탄 =====================
    private IEnumerator SkillJumpAndShoot()
    {
        // ★ 패턴 시작 애니
        enemyAnimation?.PlayAnimation(EnemyAnimation.State.PatternStart);

        if (!enableJumpPattern)
        {
            enemyAnimation?.PlayAnimation(EnemyAnimation.State.PatternEnd);
            yield return StartCoroutine(SkillFinished());
            yield break;
        }

        Vector3 s = transform.position;
        Vector3 p = s + Vector3.up * jumpHeight;

        int count = Mathf.Max(1, jumpBulletCount);
        float step = 360f / count;

        if (warnJumpPattern)
        {
            for (int i = 0; i < count; i++) CreateWarnLine($"Warn_JumpBullet_{i}", 1.25f, 9);

            float warnElapsed = 0f;
            while (warnElapsed < preWarnDuration)
            {
                int idx = 0;
                foreach (var wl in _warnLines)
                {
                    float a = step * idx;
                    float rad = a * Mathf.Deg2Rad;
                    Vector3 dir = new(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
                    SetLineByDir(wl, s, dir, 2f);
                    idx++;
                }
                warnElapsed += Time.deltaTime;
                yield return null;
            }

            if (!keepWarnDuringTransition) KillAllWarnLines();
            else StartCoroutine(_CoDelayedKillWarnLines(Mathf.Max(0f, warnTransitionTime)));
        }

        // 본 패턴 진입 FX + ★ 루프 애니
        TriggerPatternClimaxFX(transform.position);
        enemyAnimation?.PlayAnimation(EnemyAnimation.State.PatternLoop);

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

        // ★ 패턴 종료 애니
        enemyAnimation?.PlayAnimation(EnemyAnimation.State.PatternEnd);

        yield return StartCoroutine(SkillFinished());
    }

    // ===================== 파형/히트/레이저 셋업 =====================
    private float Waveform01(float t, SweepWaveform form)
    {
        float u = t - Mathf.Floor(t);
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
        return mid * mid * (3f - 2f * mid);
    }
    private void EnsureTransparentSpriteMaterial(LineRenderer lr)
    {
        if (lr.material == null || lr.material.shader == null)
        {
            lr.material = new Material(Shader.Find("Sprites/Default")); return;
        }
        string sn = lr.material.shader.name;
        if (!sn.Contains("Sprites/Default") &&
            !sn.Contains("Legacy Shaders/Particles/Alpha Blended") &&
            !sn.Contains("Unlit") && !sn.Contains("Particles"))
        {
            lr.material = new Material(Shader.Find("Sprites/Default"));
        }
    }
    private void SetupLaser(LineRenderer lr, Color c)
    {
        lr.positionCount = 2;
        lr.startWidth = lr.endWidth = laserWidthBase;
        lr.material = laserMaterial != null ? new Material(laserMaterial) : new Material(Shader.Find("Sprites/Default"));
        lr.useWorldSpace = true;
        lr.startColor = lr.endColor = c;
        EnsureTransparentSpriteMaterial(lr);
        if (useLaserColorCycle) lr.colorGradient = _cycleGradientLaser;
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

    // ===================== 몹 스폰/정리 =====================
    private void TrimDeadMobs()
    {
        for (int i = _aliveMobs.Count - 1; i >= 0; i--)
            if (_aliveMobs[i] == null) _aliveMobs.RemoveAt(i);
    }
    private bool TryGetSpawnPoint(Bounds b, out Vector3 pos, int maxTries = 10)
    {
        Transform playerT = null;
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player) playerT = player.transform;

        float minX = b.min.x + mobSpawnMargin;
        float maxX = b.max.x - mobSpawnMargin;
        float minY = b.min.y + mobSpawnMargin;
        float maxY = b.max.y - mobSpawnMargin;

        for (int t = 0; t < maxTries; t++)
        {
            int edge = UnityEngine.Random.Range(0, 4);
            float x = 0f, y = 0f;
            switch (edge)
            {
                case 0: x = UnityEngine.Random.Range(minX, maxX); y = maxY; break;
                case 1: x = UnityEngine.Random.Range(minX, maxX); y = minY; break;
                case 2: x = minX; y = UnityEngine.Random.Range(minY, maxY); break;
                default: x = maxX; y = UnityEngine.Random.Range(minY, maxY); break;
            }

            Vector3 candidate = new Vector3(x, y, 0f);

            const float checkRadius = 0.35f;
            if (mobBlockMask.value != 0 && Physics2D.OverlapCircle(candidate, checkRadius, mobBlockMask))
            {
                if (verboseSpawnLog) Debug.Log($"[MiddleBoss] 스폰거부: 벽/지형 충돌 @ {candidate}");
                continue;
            }

            if (playerT && Vector2.Distance(candidate, playerT.position) < mobMinDistanceFromPlayer)
            {
                if (verboseSpawnLog) Debug.Log($"[MiddleBoss] 스폰거부: 플레이어 근접 @ {candidate}");
                continue;
            }

            pos = candidate;
            return true;
        }

        Vector3 fallback = new Vector3(
            Mathf.Clamp(b.center.x, b.min.x + mobSpawnMargin, b.max.x - mobSpawnMargin),
            Mathf.Clamp(b.center.y, b.min.y + mobSpawnMargin, b.max.y - mobSpawnMargin),
            0f);

        if (forceSpawnIfBlocked)
        {
            if (verboseSpawnLog) Debug.LogWarning("[MiddleBoss] 모든 체크 실패 → 강제 스폰");
            pos = fallback; return true;
        }

        if (verboseSpawnLog) Debug.LogWarning("[MiddleBoss] 스폰 실패");
        pos = Vector3.zero; return false;
    }
    private GameObject SpawnMobAt(Vector3 pos)
    {
        if (mobPrefabs == null || mobPrefabs.Count == 0)
        {
            if (verboseSpawnLog) Debug.LogWarning("[MiddleBoss] SpawnMobAt 실패: mobPrefabs 비어있음");
            return null;
        }
        var prefab = mobPrefabs[UnityEngine.Random.Range(0, mobPrefabs.Count)];
        if (!prefab)
        {
            if (verboseSpawnLog) Debug.LogWarning("[MiddleBoss] SpawnMobAt 실패: mobPrefabs에 null 포함");
            return null;
        }

        var go = Instantiate(prefab, pos, Quaternion.identity, mobParent ? mobParent : null);
        if (!go) return null;
        if (!go.activeSelf) go.SetActive(true);

        var marker = go.GetComponent<BossSpawnedMob>();
        if (!marker) marker = go.AddComponent<BossSpawnedMob>();
        marker.owner = this;

        go.name = $"{prefab.name}_Spawned_{_aliveMobs.Count + 1}";
        _aliveMobs.Add(go);

        if (verboseSpawnLog) Debug.Log($"[MiddleBoss] 몹 스폰: {go.name} @ {pos} (총 {_aliveMobs.Count})");
        return go;
    }
    private void KillAllSpawnedMobs()
    {
        for (int i = _aliveMobs.Count - 1; i >= 0; i--)
        {
            var go = _aliveMobs[i];
            if (!go) { _aliveMobs.RemoveAt(i); continue; }

            var marker = go.GetComponent<BossSpawnedMob>();
            if (marker != null && marker.owner == this)
            {
                Destroy(go);
            }
            _aliveMobs.RemoveAt(i);
        }
    }

    // ===================== 종료/정리/인텐시티 =====================
    private IEnumerator SkillFinished()
    {
        KillAllWarnLines();
        KillAllSpawnedMobs();
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
        if (!isLive) return;
        isLive = false;

        // 스킬/경고 정리
        ClearAllSkillObjects();
        KillAllSpawnedMobs();

        // ★ 죽음 애니메이션
        if (enemyAnimation != null && enemyAnimation.GetEstimatedDuration(EnemyAnimation.State.Death) > 0f)
        {
            StartCoroutine(CoDieWithAnim());
        }
        else
        {
            // 죽음 스프라이트 없으면 즉시 파괴
            if (mobParent) Destroy(mobParent.gameObject);
            Destroy(gameObject);
        }
    }

    private IEnumerator CoDieWithAnim()
    {
        enemyAnimation.PlayAnimation(EnemyAnimation.State.Death);

        float wait = deathDestroyDelay;
        if (wait <= 0f)
        {
            // 애니 길이 + 여유 0.1초
            wait = enemyAnimation.GetEstimatedDuration(EnemyAnimation.State.Death) + 0.1f;
        }
        yield return new WaitForSeconds(wait);

        if (mobParent) Destroy(mobParent.gameObject);
        Destroy(gameObject);
    }

    void OnDisable()
    {
        KillAllWarnLines();
        if (camT) camT.position = camOrigin;
    }
}
