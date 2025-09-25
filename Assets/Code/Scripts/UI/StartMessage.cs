using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class StartMessage : MonoBehaviour
{
    public List<string> mapName;
    public int levelStage = 0;

    [Header("Refs")]
    [SerializeField] private GameObject usedCanvas;
    [SerializeField] private TextMeshProUGUI textMesh; // 텍스트
    [SerializeField] private Image panel;              // 커질 이미지(패널)

    [Header("Height Animation")]
    [SerializeField] private float targetHeight = 220f; // 목표 높이
    [SerializeField] private float growSpeed = 600f;  // 커지는 속도(px/s)
    [SerializeField] private float shrinkSpeed = 600f;  // 줄어드는 속도(px/s)
    [SerializeField] private float waitAtTop = 3f;    // 꼭대기에서 대기(초)

    [Header("Text Fade")]
    [SerializeField] private float fadeInDuration = 0.4f;  // 페이드인 시간(초)
    [SerializeField] private float fadeOutDuration = 0.15f; // 페이드아웃 시간(초, 빠르게)

    [Header("Behavior")]
    [SerializeField] private bool autoPlayOnStart = false;  // Start에서 자동 실행 여부

    private RectTransform PanelRT => panel.rectTransform;
    private Coroutine animHandle;

    private void Start()
    {
        PlayForLevel(0);
        // 캔버스 활성화(있다면)
        if (usedCanvas != null) usedCanvas.SetActive(true);

        // 패널 높이 0 + 텍스트 알파 0으로 초기화
        SetHeight(0f);
        SetTextAlpha(0f);

        // 자동 재생 옵션
        if (autoPlayOnStart)
        {
            PlayForLevel(levelStage);
        }
    }

    // ===== 외부에서 호출할 공개 API =====

    /// <summary>
    /// 스테이지 인덱스로 텍스트를 mapName[stage]로 설정하고 애니메이션 실행.
    /// </summary>
    public void PlayForLevel(int stage)
    {
        if (mapName == null || mapName.Count == 0)
        {
            Debug.LogWarning("[StartMessage] mapName이 비어 있습니다.");
            PlayWithText(string.Empty);
            return;
        }

        levelStage = Mathf.Clamp(stage, 0, mapName.Count - 1);
        string msg = mapName[levelStage];
        PlayWithText(msg);
    }

    /// <summary>
    /// 임의의 메시지로 텍스트를 설정하고 애니메이션 실행.
    /// </summary>
    public void PlayWithText(string message)
    {
        if (usedCanvas != null) usedCanvas.SetActive(true);

        // 중복 실행 시 이전 코루틴 정지
        if (animHandle != null) StopCoroutine(animHandle);

        // 초기 상태 세팅
        SetHeight(0f);
        SetTextAlpha(0f);
        if (textMesh != null) textMesh.text = message;

        animHandle = StartCoroutine(AnimCoroutine());
    }

    // ===== 메인 코루틴 =====
    private IEnumerator AnimCoroutine()
    {
        // 1) 0 -> targetHeight까지 점진 증가
        while (GetHeight() < targetHeight)
        {
            float newH = GetHeight() + growSpeed * Time.deltaTime;
            if (newH > targetHeight) newH = targetHeight;
            SetHeight(newH);
            yield return null;
        }

        // 2) 텍스트 페이드인
        if (textMesh != null)
            yield return StartCoroutine(FadeTextAlpha(1f, fadeInDuration));

        // 3) 꼭대기에서 대기
        yield return new WaitForSeconds(waitAtTop);

        // 4) 줄어들기 직전 텍스트 빠른 페이드아웃
        if (textMesh != null)
            yield return StartCoroutine(FadeTextAlpha(0f, fadeOutDuration));

        // 5) targetHeight -> 0까지 점진 감소
        while (GetHeight() > 0f)
        {
            float newH = GetHeight() - shrinkSpeed * Time.deltaTime;
            if (newH < 0f) newH = 0f;
            SetHeight(newH);
            yield return null;
        }

        animHandle = null;
    }

    // ===== 유틸 =====
    private float GetHeight()
    {
        return PanelRT.sizeDelta.y;
    }

    private void SetHeight(float h)
    {
        var sd = PanelRT.sizeDelta;
        sd.y = h;
        PanelRT.sizeDelta = sd;
        // 필요 시: PanelRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
    }

    private void SetTextAlpha(float a)
    {
        if (textMesh == null) return;
        var c = textMesh.color;
        c.a = a;
        textMesh.color = c;
    }

    private IEnumerator FadeTextAlpha(float targetAlpha, float duration)
    {
        if (textMesh == null)
            yield break;

        float t = 0f;
        Color start = textMesh.color;
        Color end = start; end.a = targetAlpha;

        if (duration <= 0f)
        {
            textMesh.color = end;
            yield break;
        }

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            textMesh.color = Color.Lerp(start, end, k);
            yield return null;
        }
        textMesh.color = end;
    }
}
