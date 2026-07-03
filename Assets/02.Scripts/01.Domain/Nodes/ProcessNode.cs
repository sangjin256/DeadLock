using System.Collections.Generic;

public sealed class ProcessNode
{
    public readonly int Id;
    
    private EProcessState _state;
    public EProcessState State => _state;

    private readonly List<ProcessColorSlot> _slotList;
    public IReadOnlyList<ProcessColorSlot> ColorSlotList => _slotList;

    public ProcessNode(int id, IEnumerable<ProcessColorSlot> slots)
    {
        Id = id;
        _slotList = new List<ProcessColorSlot>(slots);
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

    public void Wait()
    {
        _state = EProcessState.Waiting;
    }

    public void Fail()
    {
        _state = EProcessState.Failed;
    }
}