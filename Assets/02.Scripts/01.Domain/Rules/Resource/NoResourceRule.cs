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

    public void OnReleased(ConnectionContext context, RuleEffects effects)
    {
    }

    public void OnRoundEnded(ResourceNode resource, int round, RuleEffects effects)
    {
    }

    public void ResetSimulationState(ResourceNode resource)
    {
    }

    public IResourceRule Snapshot() => this;

    public ResourceRuleStateSnapshot CreateStateSnapshot()
    {
        return ResourceRuleStateSnapshot.None;
    }
}
