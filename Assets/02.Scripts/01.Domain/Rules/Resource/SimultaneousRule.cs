public sealed class SimultaneousRule : IResourceRule
{
    private bool _isFull;

    public bool CanReserve(ConnectionContext context)
    {
        return context.Resource.Color == context.Slot.RequiredColor;
    }

    public bool CanOccupy(ConnectionContext context)
    {
        return context.Resource.Color == context.Slot.RequiredColor;
    }

    public void OnOccupied(ConnectionContext context, RuleEffects effects)
    {
    }

    public bool CanFinish(ResourceNode resource)
    {
        if (_isFull)
        {
            return true;
        }

        if (resource.OccupiedConnectionIdList.Count >= resource.Capacity)
        {
            _isFull = true;
            return true;
        }

        return false;
    }

    public void OnReleased(ConnectionContext context, RuleEffects effects)
    {
    }

    public void OnRoundEnded(ResourceNode resource, int round, RuleEffects effects)
    {
        _isFull = false;
    }

    public void ResetSimulationState(ResourceNode resource)
    {
        _isFull = false;
    }

    public IResourceRule Snapshot()
    {
        return new SimultaneousRule();
    }

    public ResourceRuleStateSnapshot CreateStateSnapshot()
    {
        return new ResourceRuleStateSnapshot(System.Array.Empty<ColorId>(),
                                             -1,
                                             false,
                                             false,
                                             0,
                                             false,
                                             false,
                                             true);
    }
}
