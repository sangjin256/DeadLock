public sealed class ClockRuleDefinition : ResourceRuleDefinition
{
    public readonly EClockMode Mode;
    public readonly int RoundCount;

    public ClockRuleDefinition(EClockMode mode, int roundCount)
    {
        Mode = mode;
        RoundCount = roundCount;
    }
}
