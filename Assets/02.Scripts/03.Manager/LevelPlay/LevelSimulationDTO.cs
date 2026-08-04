using System.Collections.Generic;

public sealed class LevelSimulationDTO
{
    public readonly ELevelPlaySimulationEndState EndState;
    public readonly int StarCount;
    public readonly int ClearRoundCount;
    public readonly LevelRoundDTO[] RoundArray;
    public readonly int[] CompletedProcessIdArray;
    public readonly int[] BlockedConnectionIdArray;

    public LevelSimulationDTO(ELevelPlaySimulationEndState endState,
                              int starCount,
                              int clearRoundCount,
                              IReadOnlyList<LevelRoundDTO> roundList,
                              int[] completedProcessIdArray,
                              int[] blockedConnectionIdArray)
    {
        EndState = endState;
        StarCount = starCount;
        ClearRoundCount = clearRoundCount;
        RoundArray = new LevelRoundDTO[roundList.Count];
        CompletedProcessIdArray = CopyIdArray(completedProcessIdArray);
        BlockedConnectionIdArray = CopyIdArray(blockedConnectionIdArray);

        for (int i = 0; i < roundList.Count; i++)
        {
            RoundArray[i] = roundList[i];
        }
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
}
