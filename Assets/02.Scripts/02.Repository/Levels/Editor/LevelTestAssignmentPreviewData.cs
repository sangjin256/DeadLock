internal sealed class LevelTestAssignmentPreviewData
{
    private readonly int _processId;
    public int ProcessId => _processId;

    private readonly int _slotId;
    public int SlotId => _slotId;

    private readonly int _resourceId;
    public int ResourceId => _resourceId;

    private readonly int _order;
    public int Order => _order;

    private readonly int _selectionOrder;
    public int SelectionOrder => _selectionOrder;

    private readonly int _distance;
    public int Distance => _distance;

    public LevelTestAssignmentPreviewData(int processId,
                                          int slotId,
                                          int resourceId,
                                          int order,
                                          int selectionOrder,
                                          int distance)
    {
        _processId = processId;
        _slotId = slotId;
        _resourceId = resourceId;
        _order = order;
        _selectionOrder = selectionOrder;
        _distance = distance;
    }
}
