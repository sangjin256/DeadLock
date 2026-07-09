using System.Collections.Generic;

public sealed class RoundResult
{
    public readonly int RoundIndex;

    private readonly List<int> _occupiedConnectionIdList = new();
    public IReadOnlyList<int> OccupiedConnectionIdList => _occupiedConnectionIdList;

    private readonly List<int> _waitingConnectionIdList = new();
    public IReadOnlyList<int> WaitingConnectionIdList => _waitingConnectionIdList;

    private readonly List<int> _requeuedConnectionIdList = new();
    public IReadOnlyList<int> RequeuedConnectionIdList => _requeuedConnectionIdList;

    private readonly List<int> _deferredConnectionIdList = new();
    public IReadOnlyList<int> DeferredConnectionIdList => _deferredConnectionIdList;

    private readonly List<int> _completedProcessIdList = new();
    public IReadOnlyList<int> CompletedProcessIdList => _completedProcessIdList;

    private readonly List<int> _releasedConnectionIdList = new();
    public IReadOnlyList<int> ReleasedConnectionIdList => _releasedConnectionIdList;

    private readonly List<int> _failedProcessIdList = new();
    public IReadOnlyList<int> FailedProcessIdList => _failedProcessIdList;

    private readonly List<int> _blockedConnectionIdList = new();
    public IReadOnlyList<int> BlockedConnectionIdList => _blockedConnectionIdList;

    public bool HasProgress => _occupiedConnectionIdList.Count > 0 ||
                               _completedProcessIdList.Count > 0 ||
                               _releasedConnectionIdList.Count > 0 ||
                               _requeuedConnectionIdList.Count > 0 ||
                               _deferredConnectionIdList.Count > 0 ||
                               _failedProcessIdList.Count > 0 ||
                               _blockedConnectionIdList.Count > 0;

    public RoundResult(int roundIndex)
    {
        RoundIndex = roundIndex;
    }

    public void AddOccupiedConnection(int connectionId)
    {
        AddUnique(_occupiedConnectionIdList, connectionId);
    }

    public void AddWaitingConnection(int connectionId)
    {
        AddUnique(_waitingConnectionIdList, connectionId);
    }

    public void AddRequeuedConnection(int connectionId)
    {
        AddUnique(_requeuedConnectionIdList, connectionId);
    }

    public void AddDeferredConnection(int connectionId)
    {
        AddUnique(_deferredConnectionIdList, connectionId);
    }

    public void AddCompletedProcess(int processId)
    {
        AddUnique(_completedProcessIdList, processId);
    }

    public void AddReleasedConnection(int connectionId)
    {
        AddUnique(_releasedConnectionIdList, connectionId);
    }

    public void AddFailedProcess(int processId)
    {
        AddUnique(_failedProcessIdList, processId);
    }

    public void AddBlockedConnection(int connectionId)
    {
        AddUnique(_blockedConnectionIdList, connectionId);
    }

    private static void AddUnique(List<int> idList, int id)
    {
        if (!idList.Contains(id))
        {
            idList.Add(id);
        }
    }
}
