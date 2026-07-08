using System;
using UnityEngine;

[Serializable]
public sealed class LevelProcessSlotData
{
    [SerializeField]
    private int _id;
    public int Id => _id;

    [SerializeField]
    private int _requiredColorId;
    public int RequiredColorId => _requiredColorId;

    [SerializeField]
    private int _selectionOrder;
    public int SelectionOrder => _selectionOrder;
}
