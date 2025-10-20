using System.Collections;
using UnityEngine;
using DG.Tweening; // ✅ DOTween 네임스페이스 추가

[RequireComponent(typeof(Collider2D))]
public class Thron : MonoBehaviour
{
    public int damage = 100;

    [Header("가시 패턴 설정")]
    public bool startActive = false;    // ✅ 처음 켜진 상태로 시작할지 여부
    public float activeDuration = 1f;   // 콜라이더 켜지는 시간
    public float inactiveDuration = 2f; // 콜라이더 꺼지는 시간
    public int blinkCount = 2;          // 켜지기 전 깜빡임 횟수
    public float blinkInterval = 0.2f;  // 깜빡임 간격

    [Header("색상 설정")]
    public Color activeColor = Color.red;        // 켜질 때 색
    public Color inactiveColor = Color.white;    // 꺼질 때 색
    public float colorTweenTime = 0.2f;          // 색 전환 시간

    private Collider2D col;
    private SpriteRenderer spriteRenderer;
    private bool isActive; // 현재 상태 추적용

    private void Start()
    {
        col = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            Debug.LogWarning("SpriteRenderer가 없습니다. 색상 변경은 적용되지 않습니다.");
        }

        // ✅ 시작 상태 설정
        isActive = startActive;
        col.enabled = isActive;

        if (spriteRenderer != null)
            spriteRenderer.color = isActive ? activeColor : inactiveColor;

        StartCoroutine(ToggleColliderRoutine());
    }

    private IEnumerator ToggleColliderRoutine()
    {
        while (true)
        {
            if (isActive)
            {
                // 현재 켜져 있다면 → 꺼지는 쪽으로 진행
                yield return new WaitForSeconds(activeDuration);

                // 콜라이더 OFF
                col.enabled = false;
                isActive = false;

                if (spriteRenderer != null)
                    spriteRenderer.DOColor(inactiveColor, colorTweenTime);

                yield return new WaitForSeconds(inactiveDuration);
            }
            else
            {
                // 현재 꺼져 있다면 → 켜지기 전 깜빡임
                for (int i = 0; i < blinkCount; i++)
                {
                    if (spriteRenderer != null)
                        spriteRenderer.DOColor(activeColor, colorTweenTime);
                    yield return new WaitForSeconds(blinkInterval);

                    if (spriteRenderer != null)
                        spriteRenderer.DOColor(inactiveColor, colorTweenTime);
                    yield return new WaitForSeconds(blinkInterval);
                }

                // 콜라이더 ON
                col.enabled = true;
                isActive = true;

                if (spriteRenderer != null)
                    spriteRenderer.DOColor(activeColor, colorTweenTime);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!col.enabled) return;

        if (collision.CompareTag("Player"))
        {
            if (GameManager.Instance.joystickDirectionIndicator == null ||
                GameManager.Instance.joystickDirectionIndicator.IsUsingSkill)
                return;

            GameManager.Instance.playerStats.currentHP -= damage;
        }
    }
}