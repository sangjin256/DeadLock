public sealed class ProcessDefinition
{
    public readonly int Id;
    public readonly BoardPosition Position;
    public readonly ProcessSlotDefinition[] SlotList;

    public ProcessDefinition(int id, BoardPosition position, ProcessSlotDefinition[] slotList)
    {
        Id = id;
        Position = position;
        SlotList = slotList ?? new ProcessSlotDefinition[0];
    }
}
