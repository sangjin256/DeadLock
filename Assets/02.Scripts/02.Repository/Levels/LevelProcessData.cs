using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class LevelProcessData
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
    private List<LevelProcessSlotData> _slotDataList = new();
    public IReadOnlyList<LevelProcessSlotData> SlotDataList => _slotDataList;
}
