public sealed class LevelSolveAssignment
{
    public readonly int ProcessId;
    public readonly int SlotId;
    public readonly int ResourceId;

    public LevelSolveAssignment(int processId, int slotId, int resourceId)
    {
        ProcessId = processId;
        SlotId = slotId;
        ResourceId = resourceId;
    }
}
