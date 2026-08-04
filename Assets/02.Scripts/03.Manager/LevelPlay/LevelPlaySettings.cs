public sealed class LevelPlaySettings
{
    public readonly int ThreeStarRoundCount;
    public readonly int TwoStarRoundCount;
    public readonly int OneStarRoundCount;

    public bool IsValid => ThreeStarRoundCount > 0 &&
                           ThreeStarRoundCount <= TwoStarRoundCount &&
                           TwoStarRoundCount <= OneStarRoundCount;

    public LevelPlaySettings(int threeStarRoundCount,
                             int twoStarRoundCount,
                             int oneStarRoundCount)
    {
        ThreeStarRoundCount = threeStarRoundCount;
        TwoStarRoundCount = twoStarRoundCount;
        OneStarRoundCount = oneStarRoundCount;
    }
}
