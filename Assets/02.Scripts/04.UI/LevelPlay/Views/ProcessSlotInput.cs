using System;
using UnityEngine;

public sealed class ProcessSlotInput : MonoBehaviour
{
    private const float LongPressDuration = 0.45f;

    public event Action<int> OnPressed;
    public event Action<int> OnLongPressed;

    private int _slotId;
    private bool _isPointerDown;
    private bool _isLongPressTriggered;
    private float _pressedAtTime;

    public void Initialize(int slotId)
    {
        _slotId = slotId;
    }

    private void Update()
    {
        if (!_isPointerDown || _isLongPressTriggered || Time.unscaledTime - _pressedAtTime < LongPressDuration)
        {
            return;
        }

        _isLongPressTriggered = true;
        OnLongPressed?.Invoke(_slotId);
    }

    public void BeginPress()
    {
        _isPointerDown = true;
        _isLongPressTriggered = false;
        _pressedAtTime = Time.unscaledTime;
    }

    public void EndPress()
    {
        if (_isPointerDown && !_isLongPressTriggered)
        {
            OnPressed?.Invoke(_slotId);
        }

        ResetPressState();
    }

    public void CancelPress()
    {
        ResetPressState();
    }

    private void OnDisable()
    {
        ResetPressState();
    }

    private void ResetPressState()
    {
        _isPointerDown = false;
        _isLongPressTriggered = false;
    }
}
