using UnityEngine;
using System.Collections;
using System;

[RequireComponent(typeof(SpriteRenderer))]
public class TimedSpriteSplit : MonoBehaviour
{
    [Header("분리(파괴) 연출")]
    public float splitForce = 2f;
    public float rotationSpeed = 180f;
    public float fadeDuration = 1f;
    public bool destroyOriginal = true;

    [Header("지연(타이머) 설정")]
    [Tooltip("몇 초 뒤에 부서질지 (유저가 직접 설정 가능)")]
    public float startDelay = 2f; // 🔸 유저가 인스펙터에서 조정
    public bool autoStartOnEnable = false;
    public bool useUnscaledTime = false;

    [Header("경고(깜빡임) 옵션")]
    public bool flashBeforeSplit = true;
    public float flashStartAt = 0.7f;
    public float flashInterval = 0.1f;
    [Range(0f, 1f)] public float minAlphaOnFlash = 0.3f;

    private SpriteRenderer sr;
    private Coroutine countdownRoutine;
    private bool isSplitting;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        if (autoStartOnEnable)
            StartSplitAfter(startDelay);
    }

    /// <summary>
    /// 외부에서 원하는 시간 설정 후 시작 가능
    /// </summary>
    public void StartSplitAfter(float delaySeconds)
    {
        if (isSplitting) return;
        if (countdownRoutine != null) StopCoroutine(countdownRoutine);
        countdownRoutine = StartCoroutine(CountdownThenSplit(delaySeconds));
    }

    public void CancelScheduledSplit()
    {
        if (countdownRoutine != null)
        {
            StopCoroutine(countdownRoutine);
            countdownRoutine = null;
            if (sr)
            {
                var c = sr.color;
                c.a = 1f;
                sr.color = c;
            }
        }
    }

    [ContextMenu("Split Now")]
    public void SplitNow()
    {
        if (!isSplitting)
            StartCoroutine(SplitEffect());
    }

    private IEnumerator CountdownThenSplit(float delay)
    {
        float t = 0f;
        float lastFlash = 0f;
        bool flashLow = false;
        float flashStartTime = Mathf.Max(0f, delay - Mathf.Max(0f, flashStartAt));

        while (t < delay)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            t += dt;

            if (flashBeforeSplit && t >= flashStartTime && sr)
            {
                lastFlash += dt;
                if (lastFlash >= flashInterval)
                {
                    lastFlash = 0f;
                    flashLow = !flashLow;
                    var c = sr.color;
                    c.a = flashLow ? minAlphaOnFlash : 1f;
                    sr.color = c;
                }
            }
            yield return null;
        }

        if (sr)
        {
            var c = sr.color; c.a = 1f; sr.color = c;
        }

        countdownRoutine = null;
        yield return SplitEffect();
    }

    private IEnumerator SplitEffect()
    {
        isSplitting = true;
        if (!sr || sr.sprite == null)
        {
            Destroy(gameObject);
            yield break;
        }

        Sprite original = sr.sprite;
        Texture2D tex = original.texture;
        Rect rect = original.textureRect;

        int halfWidth = Mathf.FloorToInt(rect.width / 2f);
        int height = Mathf.FloorToInt(rect.height);

        Texture2D leftTex = new Texture2D(halfWidth, height, TextureFormat.RGBA32, false);
        Texture2D rightTex = new Texture2D(halfWidth, height, TextureFormat.RGBA32, false);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < halfWidth; x++)
            {
                leftTex.SetPixel(x, y, tex.GetPixel((int)rect.x + x, (int)rect.y + y));
                rightTex.SetPixel(x, y, tex.GetPixel((int)rect.x + x + halfWidth, (int)rect.y + y));
            }
        }
        leftTex.Apply();
        rightTex.Apply();

        Sprite leftSprite = Sprite.Create(leftTex, new Rect(0, 0, halfWidth, height), new Vector2(1f, 0.5f), original.pixelsPerUnit);
        Sprite rightSprite = Sprite.Create(rightTex, new Rect(0, 0, halfWidth, height), new Vector2(0f, 0.5f), original.pixelsPerUnit);

        GameObject leftObj = new GameObject(name + "_LeftHalf");
        GameObject rightObj = new GameObject(name + "_RightHalf");
        leftObj.transform.position = transform.position;
        rightObj.transform.position = transform.position;

        var leftSr = leftObj.AddComponent<SpriteRenderer>();
        var rightSr = rightObj.AddComponent<SpriteRenderer>();
        leftSr.sprite = leftSprite;
        rightSr.sprite = rightSprite;

        leftSr.sortingLayerID = sr.sortingLayerID;
        rightSr.sortingLayerID = sr.sortingLayerID;
        leftSr.sortingOrder = sr.sortingOrder;
        rightSr.sortingOrder = sr.sortingOrder;

        if (destroyOriginal) Destroy(sr);

        Vector3 leftDir = (Vector3.left + Vector3.up * 0.25f).normalized;
        Vector3 rightDir = (Vector3.right + Vector3.up * 0.25f).normalized;

        float elapsed = 0f;
        Color lc = leftSr.color, rc = rightSr.color;

        while (elapsed < fadeDuration)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            elapsed += dt;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);

            leftObj.transform.position += leftDir * splitForce * dt;
            rightObj.transform.position += rightDir * splitForce * dt;
            leftObj.transform.Rotate(Vector3.forward, rotationSpeed * dt);
            rightObj.transform.Rotate(Vector3.forward, -rotationSpeed * dt);

            leftSr.color = new Color(lc.r, lc.g, lc.b, alpha);
            rightSr.color = new Color(rc.r, rc.g, rc.b, alpha);
            yield return null;
        }

        Destroy(leftObj);
        Destroy(rightObj);
        Destroy(gameObject);
    }
}
