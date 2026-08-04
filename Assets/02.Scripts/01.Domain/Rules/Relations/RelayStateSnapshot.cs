public sealed class RelayStateSnapshot
{
    public readonly int Id;
    public readonly int FirstResourceId;
    public readonly int SecondResourceId;
    public readonly ERelayType RelayType;
    public readonly int SenderResourceId;
    public readonly bool IsActive;

    public RelayStateSnapshot(int id,
                              int firstResourceId,
                              int secondResourceId,
                              ERelayType relayType,
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
