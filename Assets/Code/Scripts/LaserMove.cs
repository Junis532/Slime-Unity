using DG.Tweening;
using UnityEngine;

public class MoveUpDOTween : MonoBehaviour
{
    void Start()
    {
        transform.DOMoveY(transform.position.y + 12f, 8f);
    }
}