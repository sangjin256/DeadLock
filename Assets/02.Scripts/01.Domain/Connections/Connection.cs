public sealed class Connection
{
    public readonly int Id;
    public readonly int ProcessId;
    public readonly int ColorSlotId;
    public readonly int ResourceId;

    private EConnectionState _state;
    public EConnectionState State => _state;

    public Connection(int id, int processId, int colorSlotId, int resourceId)
    {
        Id = id;
        ProcessId = processId;
        ColorSlotId = colorSlotId;
        ResourceId = resourceId;
        _state = EConnectionState.Planned;
    }

    public void Complete()
    {
        _state = EConnectionState.Completed;
    }

    public void Block()
    {
        _state = EConnectionState.Blocked;
    }
}