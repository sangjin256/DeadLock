using System.Collections.Generic;
using UnityEngine;

internal sealed class LegacyLevelNodeData
{
    private readonly List<Color> _colorList = new();
    public IReadOnlyList<Color> ColorList => _colorList;

    private int _index;
    public int Index => _index;

    private int _nodeType;
    public int NodeType => _nodeType;

    private int _maxCount;
    public int MaxCount => _maxCount;

    private int _fixedNum;
    public int FixedNum => _fixedNum;

    private bool _isSimultaneous;
    public bool IsSimultaneous => _isSimultaneous;

    private bool _isColorSwitch;
    public bool IsColorSwitch => _isColorSwitch;

    private bool _isStartWithEmptyColor;
    public bool IsStartWithEmptyColor => _isStartWithEmptyColor;

    private bool _isClockOnToOff;
    public bool IsClockOnToOff => _isClockOnToOff;

    private bool _isClockOffToOn;
    public bool IsClockOffToOn => _isClockOffToOn;

    private int _clockNum;
    public int ClockNum => _clockNum;

    private bool _isBlock;
    public bool IsBlock => _isBlock;

    private string _blockBinaryString = string.Empty;
    public string BlockBinaryString => _blockBinaryString;

    public void Init(int index, int nodeType)
    {
        _index = index;
        _nodeType = nodeType;
    }

    public void AddColor(Color color)
    {
        _colorList.Add(color);
    }

    public void SetMaxCount(int maxCount)
    {
        _maxCount = maxCount;
    }

    public void SetFixedNum(int fixedNum)
    {
        _fixedNum = fixedNum;
    }

    public void SetSimultaneous(bool isSimultaneous)
    {
        _isSimultaneous = isSimultaneous;
    }

    public void SetColorSwitch(bool isColorSwitch)
    {
        _isColorSwitch = isColorSwitch;
    }

    public void SetStartWithEmptyColor(bool isStartWithEmptyColor)
    {
        _isStartWithEmptyColor = isStartWithEmptyColor;
    }

    public void SetClockOnToOff(bool isClockOnToOff)
    {
        _isClockOnToOff = isClockOnToOff;
    }

    public void SetClockOffToOn(bool isClockOffToOn)
    {
        _isClockOffToOn = isClockOffToOn;
    }

    public void SetClockNum(int clockNum)
    {
        _clockNum = clockNum;
    }

    public void SetBlock(bool isBlock)
    {
        _isBlock = isBlock;
    }

    public void SetBlockBinaryString(string blockBinaryString)
    {
        _blockBinaryString = blockBinaryString ?? string.Empty;
    }
}
