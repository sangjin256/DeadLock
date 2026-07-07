public sealed class RelayRuleDefinition : BoardRuleDefinition
{
    public readonly ERelayType RelayType;
    public readonly int FirstResourceId;
    public readonly int SecondResourceId;
    public readonly int SenderResourceId;

    public RelayRuleDefinition(int id,
                               ERelayType relayType,
                               int firstResourceId,
                               int secondResourceId,
                               int senderResourceId)
        : base(id)
    {
        RelayType = relayType;
        FirstResourceId = firstResourceId;
        SecondResourceId = secondResourceId;
        SenderResourceId = senderResourceId;
    }
}
