public sealed class LevelPlaySlotDTO
{
    public readonly int Id;
    public readonly int RequiredColorId;
    public readonly int SelectionOrder;
    public readonly int ConnectionId;
    public readonly bool IsCompleted;

    public LevelPlaySlotDTO(int id,
                            int requiredColorId,
                            int selectionOrder,
                            int connectionId,
                            bool isCompleted)
    {
        Id = id;
        RequiredColorId = requiredColorId;
        SelectionOrder = selectionOrder;
        ConnectionId = connectionId;
        IsCompleted = isCompleted;
    }
}
