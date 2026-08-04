using System;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class HudColorChipInput : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField]
    private int _colorIndex;

    public event Action<int> OnPressed;
    public event Action<int, Vector2> OnDragStarted;
    public event Action<Vector2> OnDragged;
    public event Action<int, Vector2> OnDropped;

    public void Configure(int colorIndex)
    {
        _colorIndex = colorIndex;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnPressed?.Invoke(_colorIndex);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        OnDragStarted?.Invoke(_colorIndex, eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        OnDragged?.Invoke(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        OnDropped?.Invoke(_colorIndex, eventData.position);
    }
}
