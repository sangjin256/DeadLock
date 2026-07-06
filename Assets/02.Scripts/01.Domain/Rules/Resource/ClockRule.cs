public sealed class ClockRule : IResourceRule
{
    private readonly EClockMode _mode;
    private readonly int _initialRoundCount;

    private int _remainingRoundCount;
    private bool _isOpen;
    private bool _hasTransitioned;

    public ClockRule(EClockMode mode, int roundCount)
    {
        _mode = mode;
        _initialRoundCount = roundCount;
        _remainingRoundCount = roundCount;
        _isOpen = mode == EClockMode.OnToOff;
    }

    public bool CanReserve(ConnectionContext context)
    {
        return context.Resource.Color == context.Slot.RequiredColor;
    }

    public bool CanOccupy(ConnectionContext context)
    {
        return _isOpen && context.Resource.Color == context.Slot.RequiredColor;
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
        if (_hasTransitioned)
        {
            return;
        }

        _remainingRoundCount--;

        if (_remainingRoundCount > 0)
        {
            return;
        }

        _hasTransitioned = true;

        if (_mode == EClockMode.OnToOff)
        {
            _isOpen = false;

            if (resource.OccupiedConnectionIdList.Count > 0)
            {
                effects.FailOccupiedResource(resource.Id);
            }

            return;
        }

        _isOpen = true;
    }

    public void ResetSimulationState(ResourceNode resource)
    {
        _remainingRoundCount = _initialRoundCount;
        _isOpen = _mode == EClockMode.OnToOff;
        _hasTransitioned = false;
    }

    public IResourceRule Snapshot()
    {
        return new ClockRule(_mode, _initialRoundCount);
    }
}
