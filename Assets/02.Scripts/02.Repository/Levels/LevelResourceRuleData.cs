using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class LevelResourceRuleData
{
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
