public sealed class ProcessColorSlot
{
    public readonly int Id;
    public readonly ColorId RequiredColor;

    private int _selectionOrder;
    public int SelectionOrder => _selectionOrder;

    private int _connectionId;
    public int ConnectionId => _connectionId;

    private bool _isCompleted;
    public bool IsCompleted => _isCompleted;

    public bool IsConnected => _connectionId >= 0;

    public ProcessColorSlot(int id, ColorId requiredColor, int order)
    {
        Id = id;
        RequiredColor = requiredColor;
        _selectionOrder = order;
        _connectionId = -1;
    }

    public void SetOrder(int order)
    {
        _selectionOrder = order;
    }

    public void AssignConnection(int connectionId)
    {
        _connectionId = connectionId;
    }

    public void ClearConnection()
    {
        _connectionId = -1;
        _isCompleted = false;
    }

    public void ResetProgress()
    {
        _isCompleted = false;
    }

    public void Complete()
    {
        _isCompleted = true;
    }
}
