using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillButtonHandler : MonoBehaviour, IPointerClickHandler
{
    public JoystickDirectionIndicator directionIndicator;

    public void OnPointerClick(PointerEventData eventData)
    {
        directionIndicator.UseSkillButton();
    }
}
