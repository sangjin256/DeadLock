using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class LevelTestCaseData
{
    [SerializeField]
    private string _name = string.Empty;
    public string Name => _name;

    [SerializeField]
    private int _maxRoundCount;
    public int MaxRoundCount => _maxRoundCount;

    [SerializeField]
    private ESimulationEndState _expectedEndState;
    public ESimulationEndState ExpectedEndState => _expectedEndState;

    [SerializeField]
    private List<LevelAssignedConnectionData> _assignedConnectionDataList = new();
    public IReadOnlyList<LevelAssignedConnectionData> AssignedConnectionDataList => _assignedConnectionDataList;
}
