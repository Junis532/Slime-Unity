using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillButtonHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public JoystickDirectionIndicator directionIndicator;
    public CanvasGroup joystickCanvasGroup; // 조이스틱 알파 조절용
    private Image skillImage;

    private void Start()
    {
        skillImage = GetComponent<Image>();

        if (joystickCanvasGroup != null)
        {
            joystickCanvasGroup.alpha = 0f;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        directionIndicator.OnSkillButtonPressed();

        if (skillImage != null)
            skillImage.enabled = false;

        if (joystickCanvasGroup != null)
            joystickCanvasGroup.alpha = 1f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        directionIndicator.OnSkillButtonReleased();

        if (skillImage != null)
            skillImage.enabled = true;

        if (joystickCanvasGroup != null)
            joystickCanvasGroup.alpha = 0f;
        Debug.Log("스킬 버튼 릴리즈됨!");
    }
}
