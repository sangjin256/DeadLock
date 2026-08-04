public sealed class LevelPlayRelayDTO
{
    public readonly int Id;
    public readonly int FirstResourceId;
    public readonly int SecondResourceId;
    public readonly ELevelPlayRelayType RelayType;
    public readonly int SenderResourceId;
    public readonly bool IsActive;

    public LevelPlayRelayDTO(int id,
                             int firstResourceId,
                             int secondResourceId,
                             ELevelPlayRelayType relayType,
                             int senderResourceId,
                             bool isActive)
    {
        Id = id;
        FirstResourceId = firstResourceId;
        SecondResourceId = secondResourceId;
        RelayType = relayType;
        SenderResourceId = senderResourceId;
        IsActive = isActive;
    }
}
