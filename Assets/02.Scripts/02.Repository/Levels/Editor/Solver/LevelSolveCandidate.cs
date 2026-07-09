using System.Collections.Generic;

public sealed class LevelSolveCandidate
{
    public readonly LevelSolveAssignment[] AssignmentArray;
    public readonly SimulationReport SimulationReport;
    public readonly int ClearRoundCount;
    public readonly int WaitingCount;
    public readonly int RequeuedCount;
    public readonly int BlockedCount;
    public readonly int TotalDistance;

    public LevelSolveCandidate(
        IReadOnlyList<LevelSolveAssignment> assignmentList,
        SimulationReport simulationReport,
        int clearRoundCount,
        int waitingCount,
        int requeuedCount,
        int blockedCount,
        int totalDistance)
    {
        AssignmentArray = new LevelSolveAssignment[assignmentList.Count];

        for (int i = 0; i < assignmentList.Count; i++)
        {
            AssignmentArray[i] = assignmentList[i];
        }

        SimulationReport = simulationReport;
        ClearRoundCount = clearRoundCount;
        WaitingCount = waitingCount;
        RequeuedCount = requeuedCount;
        BlockedCount = blockedCount;
        TotalDistance = totalDistance;
    }
}
