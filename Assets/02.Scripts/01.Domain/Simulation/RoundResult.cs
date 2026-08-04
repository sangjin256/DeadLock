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

    private ResourceStateSnapshot[] _resourceStateSnapshotArray = new ResourceStateSnapshot[0];
    public IReadOnlyList<ResourceStateSnapshot> ResourceStateSnapshotList => _resourceStateSnapshotArray;

    private RelayStateSnapshot[] _relayStateSnapshotArray = new RelayStateSnapshot[0];
    public IReadOnlyList<RelayStateSnapshot> RelayStateSnapshotList => _relayStateSnapshotArray;

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

    public void CaptureBoardState(IReadOnlyList<ResourceStateSnapshot> resourceStateSnapshotList,
                                  IReadOnlyList<RelayStateSnapshot> relayStateSnapshotList)
    {
        _resourceStateSnapshotArray = CopySnapshotArray(resourceStateSnapshotList);
        _relayStateSnapshotArray = CopySnapshotArray(relayStateSnapshotList);
    }

    private static void AddUnique(List<int> idList, int id)
    {
        if (!idList.Contains(id))
        {
            idList.Add(id);
        }
    }

    private static T[] CopySnapshotArray<T>(IReadOnlyList<T> snapshotList)
    {
        T[] snapshotArray = new T[snapshotList.Count];

        for (int i = 0; i < snapshotList.Count; i++)
        {
            snapshotArray[i] = snapshotList[i];
        }

        return snapshotArray;
    }
}
