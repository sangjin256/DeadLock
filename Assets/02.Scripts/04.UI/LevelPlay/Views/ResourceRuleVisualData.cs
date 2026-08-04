using System;
using System.Collections.Generic;

public sealed class ResourceRuleVisualData
{
    public readonly int[] ColorSwitchColorIdArray;
    public readonly int CurrentColorSwitchIndex;
    public readonly bool HasClock;
    public readonly int ClockRemainingRoundCount;
    public readonly bool IsClockOpen;
    public readonly bool HasEmptyColor;
    public readonly bool IsEmptyColorFixed;
    public readonly bool IsSimultaneous;

    public ResourceRuleVisualData(IReadOnlyList<int> colorSwitchColorIdList,
                                  int currentColorSwitchIndex,
                                  bool hasClock,
                                  int clockRemainingRoundCount,
                                  bool isClockOpen,
                                  bool hasEmptyColor,
                                  bool isEmptyColorFixed,
                                  bool isSimultaneous)
    {
        if (colorSwitchColorIdList == null)
        {
            ColorSwitchColorIdArray = Array.Empty<int>();
        }
        else
        {
            ColorSwitchColorIdArray = new int[colorSwitchColorIdList.Count];

            for (int i = 0; i < colorSwitchColorIdList.Count; i++)
            {
                ColorSwitchColorIdArray[i] = colorSwitchColorIdList[i];
            }
        }

        CurrentColorSwitchIndex = currentColorSwitchIndex;
        HasClock = hasClock;
        ClockRemainingRoundCount = clockRemainingRoundCount;
        IsClockOpen = isClockOpen;
        HasEmptyColor = hasEmptyColor;
        IsEmptyColorFixed = isEmptyColorFixed;
        IsSimultaneous = isSimultaneous;
    }
}
