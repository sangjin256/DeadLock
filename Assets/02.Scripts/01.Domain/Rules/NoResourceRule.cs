public sealed class NoResourceRule : IResourceRule
{
    public static readonly NoResourceRule Instance = new NoResourceRule();

    private NoResourceRule()
    {
    }

    public bool CanReserve(ConnectionContext context)
    {
        return context.Resource.Color.Equals(context.Slot.RequiredColor);
    }

    public bool CanOccupy(ConnectionContext context)
    {
        return context.Resource.Color.Equals(context.Slot.RequiredColor);
    }

    public void OnOccupied(ConnectionContext context, RuleEffects effects)
    {
    }

    public bool CanFinish(ResourceNode resource)
    {
        return true;
    }

    public void OnReleased(ResourceNode resource, RuleEffects effects)
    {
    }

    public void OnRoundEnded(ResourceNode resource, int round, RuleEffects effects)
    {
    }

    public IResourceRule Snapshot() => this;
}
