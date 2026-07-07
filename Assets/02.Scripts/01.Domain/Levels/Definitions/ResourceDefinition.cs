public sealed class ResourceDefinition
{
    public readonly int Id;
    public readonly BoardPosition Position;
    public readonly ColorId InitialColor;
    public readonly int Capacity;
    public readonly ResourceRuleDefinition RuleDefinition;

    public ResourceDefinition(int id,
                              BoardPosition position,
                              ColorId initialColor,
                              int capacity,
                              ResourceRuleDefinition ruleDefinition)
    {
        Id = id;
        Position = position;
        InitialColor = initialColor;
        Capacity = capacity;
        RuleDefinition = ruleDefinition;
    }
}
