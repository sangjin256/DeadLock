using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Level_New", menuName = "DeadLock/Levels/Level")]
public sealed class LevelSO : ScriptableObject
{
    [SerializeField]
    private int _id;
    public int Id => _id;

    [SerializeField]
    private int _rowCount = 3;
    public int RowCount => _rowCount;

    [SerializeField]
    private int _columnCount = 5;
    public int ColumnCount => _columnCount;

    [SerializeField]
    private List<LevelProcessData> _processDataList = new();
    public IReadOnlyList<LevelProcessData> ProcessDataList => _processDataList;

    [SerializeField]
    private List<LevelResourceData> _resourceDataList = new();
    public IReadOnlyList<LevelResourceData> ResourceDataList => _resourceDataList;

    [SerializeField]
    private List<LevelRelayData> _relayDataList = new();
    public IReadOnlyList<LevelRelayData> RelayDataList => _relayDataList;

    [SerializeField]
    private LevelStarThresholdData _starThresholdData = new();
    public LevelStarThresholdData StarThresholdData => _starThresholdData;

    [SerializeField]
    private List<LevelTestCaseData> _testCaseDataList = new();
    public IReadOnlyList<LevelTestCaseData> TestCaseDataList => _testCaseDataList;
}
