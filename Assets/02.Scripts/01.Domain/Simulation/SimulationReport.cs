using System.Collections.Generic;

public sealed class SimulationReport
{
    public readonly ESimulationEndState EndState;
    public readonly RoundResult[] RoundResultList;
    public readonly int[] CompletedProcessIdList;
    public readonly int[] BlockedConnectionIdList;

    public SimulationReport(
        ESimulationEndState endState,
        IReadOnlyList<RoundResult> roundResultList,
        int[] completedProcessIdList,
        int[] blockedConnectionIdList)
    {
        EndState = endState;
        RoundResultList = new RoundResult[roundResultList.Count];

        for (int i = 0; i < roundResultList.Count; i++)
        {
            RoundResultList[i] = roundResultList[i];
        }

        CompletedProcessIdList = completedProcessIdList;
        BlockedConnectionIdList = blockedConnectionIdList;
    }
}
