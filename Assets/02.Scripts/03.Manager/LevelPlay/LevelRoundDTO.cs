using System.Collections.Generic;

public sealed class LevelRoundDTO
{
    public readonly int RoundIndex;
    public readonly int[] OccupiedConnectionIdArray;
    public readonly int[] WaitingConnectionIdArray;
    public readonly int[] RequeuedConnectionIdArray;
    public readonly int[] DeferredConnectionIdArray;
    public readonly int[] CompletedProcessIdArray;
    public readonly int[] ReleasedConnectionIdArray;
    public readonly int[] FailedProcessIdArray;
    public readonly int[] BlockedConnectionIdArray;
    public readonly LevelPlayRoundResourceDTO[] ResourceArray;
    public readonly LevelPlayRelayDTO[] RelayArray;

    public LevelRoundDTO(int roundIndex,
                         IReadOnlyList<int> occupiedConnectionIdList,
                         IReadOnlyList<int> waitingConnectionIdList,
                         IReadOnlyList<int> requeuedConnectionIdList,
                         IReadOnlyList<int> deferredConnectionIdList,
                         IReadOnlyList<int> completedProcessIdList,
                         IReadOnlyList<int> releasedConnectionIdList,
                         IReadOnlyList<int> failedProcessIdList,
                         IReadOnlyList<int> blockedConnectionIdList,
                         IReadOnlyList<LevelPlayRoundResourceDTO> resourceList,
                         IReadOnlyList<LevelPlayRelayDTO> relayList)
    {
        RoundIndex = roundIndex;
        OccupiedConnectionIdArray = CopyIdArray(occupiedConnectionIdList);
        WaitingConnectionIdArray = CopyIdArray(waitingConnectionIdList);
        RequeuedConnectionIdArray = CopyIdArray(requeuedConnectionIdList);
        DeferredConnectionIdArray = CopyIdArray(deferredConnectionIdList);
        CompletedProcessIdArray = CopyIdArray(completedProcessIdList);
        ReleasedConnectionIdArray = CopyIdArray(releasedConnectionIdList);
        FailedProcessIdArray = CopyIdArray(failedProcessIdList);
        BlockedConnectionIdArray = CopyIdArray(blockedConnectionIdList);
        ResourceArray = CopyArray(resourceList);
        RelayArray = CopyArray(relayList);
    }

    private static int[] CopyIdArray(IReadOnlyList<int> idList)
    {
        int[] idArray = new int[idList.Count];

        for (int i = 0; i < idList.Count; i++)
        {
            idArray[i] = idList[i];
        }

        return idArray;
    }

    private static T[] CopyArray<T>(IReadOnlyList<T> itemList)
    {
        T[] itemArray = new T[itemList.Count];

        for (int i = 0; i < itemList.Count; i++)
        {
            itemArray[i] = itemList[i];
        }

        return itemArray;
    }
}
