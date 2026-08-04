using UnityEngine;
using UnityEngine.EventSystems;

public sealed class BoardInputController : MonoBehaviour
{
    [SerializeField]
    private Camera _targetCamera;

    [SerializeField]
    private LayerMask _inputLayerMask = Physics2D.DefaultRaycastLayers;

    private Collider2D _pressedCollider;
    private ProcessView _pressedProcessView;
    private ResourceView _pressedResourceView;
    private int _activeTouchFingerId = -1;

    private void Update()
    {
        if (_activeTouchFingerId >= 0 || Input.touchCount > 0)
        {
            RefreshTouchInput();
            return;
        }

        RefreshMouseInput();
    }

    private void OnDisable()
    {
        CancelPress();
    }

    private void RefreshMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            BeginPress(Input.mousePosition, -1);
        }

        if (_pressedCollider == null)
        {
            return;
        }

        if (Input.GetMouseButton(0))
        {
            RefreshPress(Input.mousePosition);
        }

        if (Input.GetMouseButtonUp(0))
        {
            EndPress(Input.mousePosition);
        }
    }

    private void RefreshTouchInput()
    {
        Touch activeTouch = default;
        bool hasActiveTouch = false;

        for (int index = 0; index < Input.touchCount; index++)
        {
            Touch touch = Input.GetTouch(index);

            if (_activeTouchFingerId < 0)
            {
                if (touch.phase != TouchPhase.Began)
                {
                    continue;
                }

                _activeTouchFingerId = touch.fingerId;
            }

            if (touch.fingerId == _activeTouchFingerId)
            {
                activeTouch = touch;
                hasActiveTouch = true;
                break;
            }
        }

        if (!hasActiveTouch)
        {
            if (_activeTouchFingerId >= 0)
            {
                CancelPress();
            }

            return;
        }

        switch (activeTouch.phase)
        {
            case TouchPhase.Began:
                BeginPress(activeTouch.position, activeTouch.fingerId);
                break;

            case TouchPhase.Moved:
            case TouchPhase.Stationary:
                RefreshPress(activeTouch.position);
                break;

            case TouchPhase.Ended:
                EndPress(activeTouch.position);
                break;

            case TouchPhase.Canceled:
                CancelPress();
                break;
        }
    }

    private void BeginPress(Vector2 screenPosition, int pointerId)
    {
        CancelPress();
        _activeTouchFingerId = pointerId;

        if (_targetCamera == null || IsPointerOverUi(pointerId))
        {
            return;
        }

        Collider2D hitCollider = FindCollider(screenPosition);

        if (hitCollider == null)
        {
            return;
        }

        ProcessView processView = hitCollider.GetComponentInParent<ProcessView>();

        if (processView != null)
        {
            _pressedCollider = hitCollider;
            _pressedProcessView = processView;
            return;
        }

        ResourceView resourceView = hitCollider.GetComponent<ResourceView>();

        if (resourceView != null)
        {
            _pressedCollider = hitCollider;
            _pressedResourceView = resourceView;
        }
    }

    private void RefreshPress(Vector2 screenPosition)
    {
        if (_pressedCollider == null || FindCollider(screenPosition) == _pressedCollider)
        {
            return;
        }

        ClearPressedTarget();
    }

    private void EndPress(Vector2 screenPosition)
    {
        bool isReleasedOverPressedCollider = _pressedCollider != null && FindCollider(screenPosition) == _pressedCollider;

        if (isReleasedOverPressedCollider)
        {
            if (_pressedProcessView != null)
            {
                _pressedProcessView.NotifyPressed();
            }
            else if (_pressedResourceView != null)
            {
                _pressedResourceView.NotifyPressed();
            }
        }

        ClearPressedTarget();
        _activeTouchFingerId = -1;
    }

    private void CancelPress()
    {
        ClearPressedTarget();
        _activeTouchFingerId = -1;
    }

    private void ClearPressedTarget()
    {
        _pressedCollider = null;
        _pressedProcessView = null;
        _pressedResourceView = null;
    }

    public bool TryGetResourceAtScreenPosition(Vector2 screenPosition, out ResourceView resourceView)
    {
        resourceView = null;

        if (_targetCamera == null)
        {
            return false;
        }

        Collider2D hitCollider = FindCollider(screenPosition);

        if (hitCollider == null)
        {
            return false;
        }

        resourceView = hitCollider.GetComponent<ResourceView>();
        return resourceView != null;
    }

    private Collider2D FindCollider(Vector2 screenPosition)
    {
        Vector3 worldPosition = _targetCamera.ScreenToWorldPoint(new Vector3(screenPosition.x,
                                                                               screenPosition.y,
                                                                               -_targetCamera.transform.position.z));
        return Physics2D.OverlapPoint(new Vector2(worldPosition.x, worldPosition.y), _inputLayerMask);
    }

    private static bool IsPointerOverUi(int pointerId)
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        return pointerId < 0
            ? EventSystem.current.IsPointerOverGameObject()
            : EventSystem.current.IsPointerOverGameObject(pointerId);
    }
}
