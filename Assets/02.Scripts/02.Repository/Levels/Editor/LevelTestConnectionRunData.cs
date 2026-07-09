internal sealed class LevelTestConnectionRunData
{
    private readonly int _connectionId;
    public int ConnectionId => _connectionId;

    private readonly int _processId;
    public int ProcessId => _processId;

    private readonly int _slotId;
    public int SlotId => _slotId;

    private readonly int _resourceId;
    public int ResourceId => _resourceId;

    public LevelTestConnectionRunData(int connectionId,
                                      int processId,
                                      int slotId,
                                      int resourceId)
    {
        _connectionId = connectionId;
        _processId = processId;
        _slotId = slotId;
        _resourceId = resourceId;
    }
}
