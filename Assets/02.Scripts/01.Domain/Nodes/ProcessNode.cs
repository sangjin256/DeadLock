using System.Collections.Generic;
using System;

public sealed class ProcessNode
{
    public readonly int Id;
    public readonly BoardPosition Position;
    
    private EProcessState _state;
    public EProcessState State => _state;

    private readonly List<ProcessColorSlot> _slotList;
    public IReadOnlyList<ProcessColorSlot> ColorSlotList => _slotList;

    public ProcessNode(int id, List<ProcessColorSlot> slotList)
        : this(id, slotList, BoardPosition.Zero)
    {
    }

    public ProcessNode(int id, List<ProcessColorSlot> slotList, BoardPosition position)
    {
        if (slotList is null)
        {
            throw new ArgumentNullException(nameof(slotList));
        }

        Id = id;
        Position = position;
        _slotList = slotList;
        _state = EProcessState.Running;
    }

    public bool TryGetSlot(int slotId, out ProcessColorSlot slot)
    {
        slot = _slotList.Find(item => item.Id == slotId);
        return slot != null;
    }

    public bool IsCompleted()
    {
        return _slotList.Count > 0 && _slotList.TrueForAll(slot => slot.IsCompleted);
    }

    public void RefreshState()
    {
        _state = IsCompleted() ? EProcessState.Completed : EProcessState.Running;
    }

    public void Run()
    {
        if (_state != EProcessState.Completed && _state != EProcessState.Failed)
        {
            _state = EProcessState.Running;
        }
    }

    public void Wait()
    {
        _state = EProcessState.Waiting;
    }

    public void Complete()
    {
        _state = EProcessState.Completed;
    }

    public void Fail()
    {
        _state = EProcessState.Failed;
    }

    public void ResetSimulationState()
    {
        foreach (ProcessColorSlot slot in _slotList)
        {
            slot.ResetProgress();
        }

        _state = EProcessState.Running;
    }
}
