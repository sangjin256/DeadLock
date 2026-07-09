public sealed class RoundScheduleItem
{
    public readonly int ProcessId;
    public readonly int SlotId;
    public readonly int ConnectionId;
    public readonly int ResourceId;
    public readonly int RoundIndex;
    public readonly int Distance;
    public readonly int SelectionOrder;
    public readonly bool IsPriorityFromWaiting;
    public readonly int WaitingQueueIndex;
    public readonly int PriorityOrder;

    public RoundScheduleItem(
        int processId,
        int slotId,
        int connectionId,
        int resourceId,
        int roundIndex,
        int distance,
        int selectionOrder,
        bool isPriorityFromWaiting,
        int waitingQueueIndex,
        int priorityOrder)
    {
        ProcessId = processId;
        SlotId = slotId;
        ConnectionId = connectionId;
        ResourceId = resourceId;
        RoundIndex = roundIndex;
        Distance = distance;
        SelectionOrder = selectionOrder;
        IsPriorityFromWaiting = isPriorityFromWaiting;
        WaitingQueueIndex = waitingQueueIndex;
        PriorityOrder = priorityOrder;
    }

    public static RoundScheduleItem FromWaitingRequest(
        WaitingRequest request,
        int roundIndex,
        int distance,
        int selectionOrder,
        int waitingQueueIndex,
        int priorityOrder)
    {
        return new RoundScheduleItem(
            request.ProcessId,
            request.SlotId,
            request.ConnectionId,
            request.ResourceId,
            roundIndex,
            distance,
            selectionOrder,
            true,
            waitingQueueIndex,
            priorityOrder);
    }
}
