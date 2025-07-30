using UnityEngine;
using DG.Tweening;

public class PlayerDamaged : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    public void PlayDamageEffect()
    {
        if (spriteRenderer == null) return;

        spriteRenderer.DOKill();  // 이전 애니메이션 정리

        if (GameManager.Instance != null && GameManager.Instance.cameraShake != null)
        {
            Debug.Log("CameraShake 호출됨");
            GameManager.Instance.cameraShake.Shake();
        }

        spriteRenderer.color = Color.red;  // 빨간색으로 변경
        spriteRenderer.DOColor(originalColor, 0.5f);  // 원래 색으로 돌아감

        // 진동 추가 (모바일 기기에서만 작동)
        Handheld.Vibrate();

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            var zacSkill = playerObj.GetComponent<ZacSkill>();
            if (zacSkill != null && zacSkill.enabled)
            {
                zacSkill.SpawnPieces();
            }
        }
    }
}
