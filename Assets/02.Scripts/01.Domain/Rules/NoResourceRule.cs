public sealed class NoResourceRule : IResourceRule
{
    public static readonly NoResourceRule Instance = new NoResourceRule();

    private NoResourceRule()
    {
    }

    public bool CanConnect(ConnectionContext context)
    {
        return context.Resource.Color.Equals(context.Slot.RequiredColor);
    }

    public void OnConnected(ConnectionContext context, RuleEffects effects)
    {
    }

    public bool CanFinish(ResourceNode resource)
    {
        return true;
    }

    public void OnRoundEnded(ResourceNode resource, int round, RuleEffects effects)
    {
    }

    public IResourceRule Snapshot() => this;
}