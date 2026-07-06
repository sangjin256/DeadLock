using System.Collections.Generic;

public sealed class ResourceNode
{
    public readonly int Id;
    public readonly int Capacity;
    public readonly IResourceRule Rule;
    public readonly BoardPosition Position;

    public int AvailableCapacity => Capacity - _occupiedConnectionIdList.Count;
    public bool HasAvailableCapacity => AvailableCapacity > 0;
    public bool WasOccupiedThisRound => _wasOccupiedThisRound;

    private readonly ColorId _initialColor;
    private ColorId _color;
    public ColorId Color
    {
        get
        {
            if (_hasLatchedOccupiedColor)
            {
                return _latchedOccupiedColor;
            }

            if (_hasRelayColor)
            {
                return _relayColor;
            }

            return _color;
        }
    }

    private bool _isLocked;
    public bool IsLocked => _isLocked;

    private readonly List<int> _occupiedConnectionIdList;
    public IReadOnlyList<int> OccupiedConnectionIdList => _occupiedConnectionIdList;

    private readonly Queue<WaitingRequest> _waitingRequestQueue;
    public int WaitingCount => _waitingRequestQueue.Count;

    private ColorId _relayColor;
    private bool _hasRelayColor;

    private ColorId _pendingRelayColor;
    private bool _hasPendingRelayColor;
    private bool _hasPendingRelayColorChange;

    private ColorId _latchedOccupiedColor;
    private bool _hasLatchedOccupiedColor;

    private bool _wasOccupiedThisRound;

    public ResourceNode(int id, ColorId color, int capacity, IResourceRule rule)
        : this(id, color, capacity, rule, BoardPosition.Zero)
    {
    }

    public ResourceNode(int id, ColorId color, int capacity, IResourceRule rule, BoardPosition position)
    {
        Id = id;
        _initialColor = color;
        _color = color;
        Capacity = capacity;
        Rule = rule ?? NoResourceRule.Instance;
        Position = position;
        _occupiedConnectionIdList = new List<int>();
        _waitingRequestQueue = new Queue<WaitingRequest>();
    }

    public void OccupyConnection(int connectionId)
    {
        if (_occupiedConnectionIdList.Contains(connectionId))
        {
            return;
        }

        if (_occupiedConnectionIdList.Count == 0)
        {
            _latchedOccupiedColor = Color;
            _hasLatchedOccupiedColor = true;
        }

        _occupiedConnectionIdList.Add(connectionId);
        _wasOccupiedThisRound = true;
    }

    public void ReleaseConnection(int connectionId)
    {
        _occupiedConnectionIdList.Remove(connectionId);

        if (_occupiedConnectionIdList.Count == 0)
        {
            _hasLatchedOccupiedColor = false;
            ApplyPendingRelayColorChange();
        }
    }

    public void EnqueueWaiting(WaitingRequest request)
    {
        if (request is not null)
        {
            _waitingRequestQueue.Enqueue(request);
        }
    }

    public bool TryDequeueWaiting(out WaitingRequest request)
    {
        if (_waitingRequestQueue.Count == 0)
        {
            request = null;
            return false;
        }

        request = _waitingRequestQueue.Dequeue();
        return true;
    }

    public void ChangeColor(ColorId newColor)
    {
        _color = newColor;
    }

    public void ResetColor()
    {
        _color = _initialColor;
    }

    public void SetLocked(bool locked)
    {
        _isLocked = locked;
    }

    public void SetRelayColor(ColorId color)
    {
        if (_occupiedConnectionIdList.Count > 0)
        {
            _pendingRelayColor = color;
            _hasPendingRelayColor = true;
            _hasPendingRelayColorChange = true;
            return;
        }

        _relayColor = color;
        _hasRelayColor = true;
        ClearPendingRelayColorChange();
    }

    public void ClearRelayColor()
    {
        if (_occupiedConnectionIdList.Count > 0)
        {
            _hasPendingRelayColor = false;
            _hasPendingRelayColorChange = true;
            return;
        }

        _hasRelayColor = false;
        ClearPendingRelayColorChange();
    }

    public void ResetSimulationState()
    {
        _occupiedConnectionIdList.Clear();
        _waitingRequestQueue.Clear();
        _isLocked = false;
        _hasRelayColor = false;
        _hasLatchedOccupiedColor = false;
        _wasOccupiedThisRound = false;
        ResetColor();
        ClearPendingRelayColorChange();
    }

    public void ResetRoundState()
    {
        _wasOccupiedThisRound = false;
    }

    private void ApplyPendingRelayColorChange()
    {
        if (!_hasPendingRelayColorChange)
        {
            return;
        }

        if (_hasPendingRelayColor)
        {
            _relayColor = _pendingRelayColor;
            _hasRelayColor = true;
        }
        else
        {
            _hasRelayColor = false;
        }

        ClearPendingRelayColorChange();
    }

    private void ClearPendingRelayColorChange()
    {
        _pendingRelayColor = ColorId.None;
        _hasPendingRelayColor = false;
        _hasPendingRelayColorChange = false;
    }
}
