using System;
using System.Collections.Generic;

public sealed class ResourceRuleStateSnapshot
{
    public readonly ColorId[] ColorSwitchColorArray;
    public readonly int ColorSwitchCurrentIndex;
    public readonly bool HasClock;
    public readonly bool IsClockOpen;
    public readonly int ClockRemainingRoundCount;
    public readonly bool HasEmptyColor;
    public readonly bool IsEmptyColorFixed;
    public readonly bool IsSimultaneous;

    public ResourceRuleStateSnapshot(IReadOnlyList<ColorId> colorSwitchColorList,
                                     int colorSwitchCurrentIndex,
                                     bool hasClock,
                                     bool isClockOpen,
                                     int clockRemainingRoundCount,
                                     bool hasEmptyColor,
                                     bool isEmptyColorFixed,
                                     bool isSimultaneous)
    {
        if (colorSwitchColorList is null)
        {
            throw new ArgumentNullException(nameof(colorSwitchColorList));
        }

        ColorSwitchColorArray = new ColorId[colorSwitchColorList.Count];

        for (int i = 0; i < colorSwitchColorList.Count; i++)
        {
            ColorSwitchColorArray[i] = colorSwitchColorList[i];
        }

        ColorSwitchCurrentIndex = colorSwitchCurrentIndex;
        HasClock = hasClock;
        IsClockOpen = isClockOpen;
        ClockRemainingRoundCount = clockRemainingRoundCount;
        HasEmptyColor = hasEmptyColor;
        IsEmptyColorFixed = isEmptyColorFixed;
        IsSimultaneous = isSimultaneous;
    }

    public static ResourceRuleStateSnapshot None { get; } = new ResourceRuleStateSnapshot(Array.Empty<ColorId>(),
                                                                                            -1,
                                                                                            false,
                                                                                            false,
                                                                                            0,
                                                                                            false,
                                                                                            false,
                                                                                            false);
}
