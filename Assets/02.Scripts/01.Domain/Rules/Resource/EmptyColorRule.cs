public sealed class EmptyColorRule : IResourceRule
{
    private ColorId _fixedColor;
    private bool _isColorFixed;

    public bool CanReserve(ConnectionContext context)
    {
        return true;
    }

    public bool CanOccupy(ConnectionContext context)
    {
        return !_isColorFixed || _fixedColor == context.Slot.RequiredColor;
    }

    public void OnOccupied(ConnectionContext context, RuleEffects effects)
    {
        if (_isColorFixed)
        {
            return;
        }

        context.Resource.ChangeColor(context.Slot.RequiredColor);
        _fixedColor = context.Slot.RequiredColor;
        _isColorFixed = true;
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
        _fixedColor = ColorId.None;
        _isColorFixed = false;
        resource.ResetColor();
    }

    public IResourceRule Snapshot()
    {
        return new EmptyColorRule();
    }

    public ResourceRuleStateSnapshot CreateStateSnapshot()
    {
        return new ResourceRuleStateSnapshot(System.Array.Empty<ColorId>(),
                                             -1,
                                             false,
                                             false,
                                             0,
                                             true,
                                             _isColorFixed,
                                             false);
    }
}
