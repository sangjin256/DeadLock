using System;
using System.Collections.Generic;

public sealed class LevelPlayResourceRuleDTO
{
    public readonly int[] ColorSwitchColorIdArray;
    public readonly int ColorSwitchCurrentIndex;
    public readonly bool HasClock;
    public readonly bool IsClockOpen;
    public readonly int ClockRemainingRoundCount;
    public readonly bool HasEmptyColor;
    public readonly bool IsEmptyColorFixed;
    public readonly bool IsSimultaneous;

    public LevelPlayResourceRuleDTO(IReadOnlyList<int> colorSwitchColorIdList,
                                    int colorSwitchCurrentIndex,
                                    bool hasClock,
                                    bool isClockOpen,
                                    int clockRemainingRoundCount,
                                    bool hasEmptyColor,
                                    bool isEmptyColorFixed,
                                    bool isSimultaneous)
    {
        if (colorSwitchColorIdList is null)
        {
            throw new ArgumentNullException(nameof(colorSwitchColorIdList));
        }

        ColorSwitchColorIdArray = new int[colorSwitchColorIdList.Count];

        for (int i = 0; i < colorSwitchColorIdList.Count; i++)
        {
            ColorSwitchColorIdArray[i] = colorSwitchColorIdList[i];
        }

        ColorSwitchCurrentIndex = colorSwitchCurrentIndex;
        HasClock = hasClock;
        IsClockOpen = isClockOpen;
        ClockRemainingRoundCount = clockRemainingRoundCount;
        HasEmptyColor = hasEmptyColor;
        IsEmptyColorFixed = isEmptyColorFixed;
        IsSimultaneous = isSimultaneous;
    }
}
