using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class LevelResourceData
{
    [SerializeField]
    private int _id;
    public int Id => _id;

    [SerializeField]
    private int _row;
    public int Row => _row;

    [SerializeField]
    private int _column;
    public int Column => _column;

    [SerializeField]
    private int _initialColorId;
    public int InitialColorId => _initialColorId;

    [SerializeField]
    private int _capacity = 1;
    public int Capacity => _capacity;

    [SerializeField]
    private ELevelResourceRuleType _ruleType;
    public ELevelResourceRuleType RuleType => _ruleType;

    [SerializeField]
    private List<int> _colorIdList = new();
    public IReadOnlyList<int> ColorIdList => _colorIdList;

    [SerializeField]
    private EClockMode _clockMode;
    public EClockMode ClockMode => _clockMode;

    [SerializeField]
    private int _clockRoundCount;
    public int ClockRoundCount => _clockRoundCount;
}
