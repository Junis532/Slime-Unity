using UnityEngine.EventSystems;
using UnityEngine;

public class JoystickDragEvents : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public System.Action OnDragStart;
    public System.Action OnDragEnd;

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDragStart?.Invoke();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        OnDragEnd?.Invoke();
    }
}