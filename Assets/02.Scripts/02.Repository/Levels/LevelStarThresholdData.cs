using System;
using UnityEngine;

[Serializable]
public sealed class LevelStarThresholdData
{
    [SerializeField]
    private int _threeStarRoundCount;
    public int ThreeStarRoundCount => _threeStarRoundCount;

    [SerializeField]
    private int _twoStarRoundCount;
    public int TwoStarRoundCount => _twoStarRoundCount;

    [SerializeField]
    private int _oneStarRoundCount;
    public int OneStarRoundCount => _oneStarRoundCount;
}
