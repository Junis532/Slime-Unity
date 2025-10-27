using UnityEngine;

public class PlayerReflection : MonoBehaviour
{
    [Header("기본 설정")]
    public SpriteRenderer playerRenderer;
    public Transform waterSurface; // 물의 Transform (Y 높이)
    public float visibleRange = 0.5f; // 반사가 보이는 거리 범위

    [Header("반사 효과 설정")]
    public float yOffset = -0.1f;
    public float alpha = 0.4f;
    public float waveStrength = 0.05f;
    public float waveSpeed = 2f;

    [Header("크기 변화 설정")]
    public float maxScale = 1f; // 물 가까이 있을 때 반사 크기
    public float minScale = 0.6f; // 멀리 있을 때 반사 크기

    [Header("물 영역")]
    public Vector2 waterSize = new Vector2(2f, 1f); // 물 넓이
    public Vector2 waterCenter = Vector2.zero;      // 물 중심 위치
    public BoxCollider2D waterCollider; // 물 영역 콜라이더

    private SpriteRenderer reflectionRenderer;
    private Color baseColor;

    void Start()
    {
        // 반사 오브젝트 생성
        GameObject reflectionObj = new GameObject("PlayerReflection");
        reflectionRenderer = reflectionObj.AddComponent<SpriteRenderer>();
        reflectionRenderer.sprite = playerRenderer.sprite;
        reflectionRenderer.sortingLayerName = playerRenderer.sortingLayerName;
        reflectionRenderer.sortingOrder = playerRenderer.sortingOrder - 1;
        baseColor = new Color(1, 1, 1, alpha);
    }

    void Update()
    {
        if (playerRenderer.sprite != null)
            reflectionRenderer.sprite = playerRenderer.sprite;

        Vector3 playerPos = transform.position;

        // ── 물 영역 체크 (Collider 기준) ──
        bool inWater = false;
        if (waterCollider != null)
        {
            Bounds bounds = waterCollider.bounds;
            inWater = bounds.Contains(playerPos);
        }

        // 위치 계산 (물 기준 대칭)
        Vector3 reflectionPos = playerPos;
        reflectionPos.y = waterSurface.position.y - (playerPos.y - waterSurface.position.y) + yOffset;
        reflectionRenderer.transform.position = reflectionPos;

        // 좌우 반전 + 상하 반전
        reflectionRenderer.flipX = playerRenderer.flipX;

        // 크기 변화
        float distanceToWater = Mathf.Abs(playerPos.y - waterSurface.position.y);
        float t = Mathf.InverseLerp(visibleRange, 0f, distanceToWater);
        float currentScale = Mathf.Lerp(minScale, maxScale, t);
        reflectionRenderer.transform.localScale = new Vector3(currentScale, -currentScale, 1);

        // 물결 흔들림
        float wave = Mathf.Sin(Time.time * waveSpeed + playerPos.x * 2f) * waveStrength;
        reflectionRenderer.transform.position += new Vector3(0, wave, 0);
        // ───── 반사 표시 조건 ─────
        // inWater이면 바로 alpha, 아니면 0
        float targetAlpha = inWater ? alpha : 0f;
        Color c = baseColor;
        c.a = targetAlpha;  // Lerp 제거
        reflectionRenderer.color = c;

    }
}
