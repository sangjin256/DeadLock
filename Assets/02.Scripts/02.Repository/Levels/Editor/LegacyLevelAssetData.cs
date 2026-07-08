using System.Collections.Generic;

internal sealed class LegacyLevelAssetData
{
    private readonly List<LegacyLevelNodeData> _nodeDataList = new();
    public IReadOnlyList<LegacyLevelNodeData> NodeDataList => _nodeDataList;

    private string _name = string.Empty;
    public string Name => _name;

    private int _rowCount;
    public int RowCount => _rowCount;

    private int _columnCount;
    public int ColumnCount => _columnCount;

    public void SetName(string name)
    {
        _name = name ?? string.Empty;
    }

    public void SetRowCount(int rowCount)
    {
        _rowCount = rowCount;
    }

    public void SetColumnCount(int columnCount)
    {
        _columnCount = columnCount;
    }

    public void AddNode(LegacyLevelNodeData nodeData)
    {
        _nodeDataList.Add(nodeData);
    }
}
