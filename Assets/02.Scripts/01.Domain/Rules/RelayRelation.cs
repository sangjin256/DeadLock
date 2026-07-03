public sealed class RelayRelation
{
    public readonly int Id;
    public readonly int FirstResourceId;
    public readonly int SecondResourceId;
    public readonly ERelayType RelayType;
    public readonly int SenderResourceId;

    public RelayRelation(int id, int firstResourceId, int secondResourceId, ERelayType relayType, int senderResourceId)
    {
        Id = id;
        FirstResourceId = firstResourceId;
        SecondResourceId = secondResourceId;
        RelayType = relayType;
        SenderResourceId = senderResourceId;
    }

    public bool Contains(int resourceId)
    {
        return FirstResourceId == resourceId || SecondResourceId == resourceId;
    }

    public int GetOther(int resourceId)
    {
        return FirstResourceId == resourceId ? SecondResourceId : FirstResourceId;
    }
}