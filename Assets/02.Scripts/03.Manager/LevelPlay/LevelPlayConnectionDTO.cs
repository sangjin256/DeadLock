public sealed class LevelPlayConnectionDTO
{
    public readonly int Id;
    public readonly int ProcessId;
    public readonly int SlotId;
    public readonly int ResourceId;
    public readonly ELevelPlayConnectionState State;

    public LevelPlayConnectionDTO(int id,
                                  int processId,
                                  int slotId,
                                  int resourceId,
                                  ELevelPlayConnectionState state)
    {
        Id = id;
        ProcessId = processId;
        SlotId = slotId;
        ResourceId = resourceId;
        State = state;
    }
}
