public sealed class LevelSolveReport
{
    public readonly ELevelSolveEndState EndState;
    public readonly LevelSolveCandidate BestCandidate;
    public readonly LevelStarThresholdRecommendation StarThresholdRecommendation;
    public readonly LevelValidationResult ValidationResult;
    public readonly int MinimumPossibleRoundCount;
    public readonly int ExploredNodeCount;
    public readonly int EvaluatedCandidateCount;
    public readonly int FoundSolutionCount;
    public readonly bool IsOptimalProven;
    public readonly string Message;

    public LevelSolveReport(
        ELevelSolveEndState endState,
        LevelSolveCandidate bestCandidate,
        LevelStarThresholdRecommendation starThresholdRecommendation,
        LevelValidationResult validationResult,
        int minimumPossibleRoundCount,
        int exploredNodeCount,
        int evaluatedCandidateCount,
        int foundSolutionCount,
        bool isOptimalProven,
        string message)
    {
        EndState = endState;
        BestCandidate = bestCandidate;
        StarThresholdRecommendation = starThresholdRecommendation;
        ValidationResult = validationResult;
        MinimumPossibleRoundCount = minimumPossibleRoundCount;
        ExploredNodeCount = exploredNodeCount;
        EvaluatedCandidateCount = evaluatedCandidateCount;
        FoundSolutionCount = foundSolutionCount;
        IsOptimalProven = isOptimalProven;
        Message = message ?? string.Empty;
    }
}
