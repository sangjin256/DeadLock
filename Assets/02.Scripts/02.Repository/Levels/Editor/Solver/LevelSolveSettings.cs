using System;

public sealed class LevelSolveSettings
{
    public const int DefaultMaxRoundCount = 30;
    public const int DefaultMaxSearchNodeCount = 2000000;
    public const int DefaultMaxEvaluatedCandidateCount = 2000000;

    public readonly int MaxRoundCount;
    public readonly int MaxSearchNodeCount;
    public readonly int MaxEvaluatedCandidateCount;
    public readonly bool ShowProgressBar;

    public LevelSolveSettings(
        int maxRoundCount,
        int maxSearchNodeCount,
        int maxEvaluatedCandidateCount,
        bool showProgressBar)
    {
        MaxRoundCount = Math.Max(1, maxRoundCount);
        MaxSearchNodeCount = Math.Max(1, maxSearchNodeCount);
        MaxEvaluatedCandidateCount = Math.Max(1, maxEvaluatedCandidateCount);
        ShowProgressBar = showProgressBar;
    }

    public static LevelSolveSettings CreateDefault()
    {
        return new LevelSolveSettings(DefaultMaxRoundCount,
                                      DefaultMaxSearchNodeCount,
                                      DefaultMaxEvaluatedCandidateCount,
                                      true);
    }
}
