public sealed class LevelStarThresholdRecommendation
{
    public readonly int ThreeStarRoundCount;
    public readonly int TwoStarRoundCount;
    public readonly int OneStarRoundCount;
    public readonly bool IsBasedOnProvenOptimal;
    public readonly string Reason;

    public LevelStarThresholdRecommendation(
        int threeStarRoundCount,
        int twoStarRoundCount,
        int oneStarRoundCount,
        bool isBasedOnProvenOptimal,
        string reason)
    {
        ThreeStarRoundCount = threeStarRoundCount;
        TwoStarRoundCount = twoStarRoundCount;
        OneStarRoundCount = oneStarRoundCount;
        IsBasedOnProvenOptimal = isBasedOnProvenOptimal;
        Reason = reason ?? string.Empty;
    }
}
