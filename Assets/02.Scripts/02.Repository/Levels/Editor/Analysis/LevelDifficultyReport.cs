using System.Collections.Generic;

public sealed class LevelDifficultyReport
{
    public readonly bool IsAvailable;
    public readonly float Score;
    public readonly ELevelDifficultyGrade Grade;
    public readonly string GradeText;
    public readonly string Summary;
    public readonly string UnavailableReason;
    public readonly string[] ReasonList;
    public readonly int OptimalRoundCount;
    public readonly int MinimumPossibleRoundCount;
    public readonly int WaitingCount;
    public readonly int RequeuedCount;
    public readonly int DeferredCount;
    public readonly int BlockedCount;
    public readonly int FoundSolutionCount;
    public readonly int EvaluatedCandidateCount;
    public readonly int AssignmentCount;
    public readonly float AverageDistance;
    public readonly float RoundPressure;
    public readonly float FlowPressure;
    public readonly float SolutionScarcityPressure;
    public readonly float RuleComplexityPressure;
    public readonly float DistancePressure;

    public LevelDifficultyReport(
        bool isAvailable,
        float score,
        ELevelDifficultyGrade grade,
        string gradeText,
        string summary,
        string unavailableReason,
        IReadOnlyList<string> reasonList,
        int optimalRoundCount,
        int minimumPossibleRoundCount,
        int waitingCount,
        int requeuedCount,
        int deferredCount,
        int blockedCount,
        int foundSolutionCount,
        int evaluatedCandidateCount,
        int assignmentCount,
        float averageDistance,
        float roundPressure,
        float flowPressure,
        float solutionScarcityPressure,
        float ruleComplexityPressure,
        float distancePressure)
    {
        IsAvailable = isAvailable;
        Score = score;
        Grade = grade;
        GradeText = gradeText ?? string.Empty;
        Summary = summary ?? string.Empty;
        UnavailableReason = unavailableReason ?? string.Empty;
        ReasonList = CreateReasonArray(reasonList);
        OptimalRoundCount = optimalRoundCount;
        MinimumPossibleRoundCount = minimumPossibleRoundCount;
        WaitingCount = waitingCount;
        RequeuedCount = requeuedCount;
        DeferredCount = deferredCount;
        BlockedCount = blockedCount;
        FoundSolutionCount = foundSolutionCount;
        EvaluatedCandidateCount = evaluatedCandidateCount;
        AssignmentCount = assignmentCount;
        AverageDistance = averageDistance;
        RoundPressure = roundPressure;
        FlowPressure = flowPressure;
        SolutionScarcityPressure = solutionScarcityPressure;
        RuleComplexityPressure = ruleComplexityPressure;
        DistancePressure = distancePressure;
    }

    public static LevelDifficultyReport CreateUnavailable(string reason)
    {
        return new LevelDifficultyReport(false,
                                         0f,
                                         ELevelDifficultyGrade.VeryEasy,
                                         string.Empty,
                                         string.Empty,
                                         reason,
                                         new[] { reason },
                                         0,
                                         0,
                                         0,
                                         0,
                                         0,
                                         0,
                                         0,
                                         0,
                                         0,
                                         0f,
                                         0f,
                                         0f,
                                         0f,
                                         0f,
                                         0f);
    }

    private static string[] CreateReasonArray(IReadOnlyList<string> reasonList)
    {
        if (reasonList is null)
        {
            return new string[0];
        }

        string[] resultArray = new string[reasonList.Count];

        for (int i = 0; i < reasonList.Count; i++)
        {
            resultArray[i] = reasonList[i] ?? string.Empty;
        }

        return resultArray;
    }
}
