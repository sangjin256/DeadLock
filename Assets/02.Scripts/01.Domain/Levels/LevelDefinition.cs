public sealed class LevelDefinition
{
    public readonly int Id;
    public readonly int RowCount;
    public readonly int ColumnCount;
    public readonly ProcessNode[] ProcessList;
    public readonly ResourceNode[] ResourceList;
    public readonly IBoardRule[] BoardRuleList;

    public LevelDefinition(int id, 
                           int rowCount, 
                           int columnCount, 
                           ProcessNode[] processList, 
                           ResourceNode[] resourceList, 
                           IBoardRule[] boardRuleList)
    {
        Id = id;
        RowCount = rowCount;
        ColumnCount = columnCount;
        ProcessList = processList;
        ResourceList = resourceList;
        BoardRuleList = boardRuleList;
    }

    public Board CreateBoard()
    {
        return new Board(ProcessList, ResourceList, BoardRuleList);
    }
}