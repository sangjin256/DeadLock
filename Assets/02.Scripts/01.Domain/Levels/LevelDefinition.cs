public sealed class LevelDefinition
{
    public readonly int Id;
    public readonly int RowCount;
    public readonly int ColumnCount;
    public readonly ProcessDefinition[] ProcessList;
    public readonly ResourceDefinition[] ResourceList;
    public readonly BoardRuleDefinition[] BoardRuleList;

    public LevelDefinition(int id, 
                           int rowCount, 
                           int columnCount, 
                           ProcessDefinition[] processList,
                           ResourceDefinition[] resourceList,
                           BoardRuleDefinition[] boardRuleList)
    {
        Id = id;
        RowCount = rowCount;
        ColumnCount = columnCount;
        ProcessList = processList ?? new ProcessDefinition[0];
        ResourceList = resourceList ?? new ResourceDefinition[0];
        BoardRuleList = boardRuleList ?? new BoardRuleDefinition[0];
    }
}
