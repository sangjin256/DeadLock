public sealed class WaitingRequest
{
    public readonly int ProcessId;
    public readonly int SlotId;
    public readonly int ConnectionId;
    public readonly int ResourceId;

    public WaitingRequest(int processId, int slotId, int connectionId, int resourceId)
    {
        ProcessId = processId;
        SlotId = slotId;
        ConnectionId = connectionId;
        ResourceId = resourceId;
    }
}
