using System;
using UnityEngine;

[Serializable]
public sealed class LevelAssignedConnectionData
{
    [SerializeField]
    private int _processId;
    public int ProcessId => _processId;

    [SerializeField]
    private int _slotId;
    public int SlotId => _slotId;

    [SerializeField]
    private int _resourceId;
    public int ResourceId => _resourceId;
}
