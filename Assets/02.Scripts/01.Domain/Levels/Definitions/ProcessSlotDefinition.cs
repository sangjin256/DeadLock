public sealed class ProcessSlotDefinition
{
    public readonly int Id;
    public readonly ColorId RequiredColor;
    public readonly int SelectionOrder;

    public ProcessSlotDefinition(int id, ColorId requiredColor, int selectionOrder)
    {
        Id = id;
        RequiredColor = requiredColor;
        SelectionOrder = selectionOrder;
    }
}
