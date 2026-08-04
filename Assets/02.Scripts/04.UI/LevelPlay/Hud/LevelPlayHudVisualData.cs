using System;
using System.Collections.Generic;

public sealed class LevelPlayHudVisualData
{
    public readonly int LevelId;
    public readonly bool IsPlanning;
    public readonly bool CanStartSimulation;
    public readonly int ReservedSlotCount;
    public readonly int TotalSlotCount;
    public readonly bool IsPlaybackRunning;
    public readonly bool IsPlaybackPaused;
    public readonly int CurrentRoundIndex;
    public readonly float PlaybackSpeed;
    public readonly int[] SelectedProcessRequiredColorIdArray;
    public readonly int SelectedRequiredColorIndex;
    public readonly ELevelPlayResultVisualState ResultState;
    public readonly int ResultStarCount;
    public readonly int ResultRoundCount;

    public LevelPlayHudVisualData(int levelId,
                                  bool isPlanning,
                                  bool canStartSimulation,
                                  int reservedSlotCount,
                                  int totalSlotCount,
                                  bool isPlaybackRunning,
                                  bool isPlaybackPaused,
                                  int currentRoundIndex,
                                  float playbackSpeed,
                                  IReadOnlyList<int> selectedProcessRequiredColorIdList,
                                  int selectedRequiredColorIndex,
                                  ELevelPlayResultVisualState resultState,
                                  int resultStarCount,
                                  int resultRoundCount)
    {
        LevelId = levelId;
        IsPlanning = isPlanning;
        CanStartSimulation = canStartSimulation;
        ReservedSlotCount = reservedSlotCount;
        TotalSlotCount = totalSlotCount;
        IsPlaybackRunning = isPlaybackRunning;
        IsPlaybackPaused = isPlaybackPaused;
        CurrentRoundIndex = currentRoundIndex;
        PlaybackSpeed = playbackSpeed;
        SelectedProcessRequiredColorIdArray = CopyColorIdArray(selectedProcessRequiredColorIdList);
        SelectedRequiredColorIndex = selectedRequiredColorIndex;
        ResultState = resultState;
        ResultStarCount = resultStarCount;
        ResultRoundCount = resultRoundCount;
    }

    private static int[] CopyColorIdArray(IReadOnlyList<int> colorIdList)
    {
        if (colorIdList == null || colorIdList.Count == 0)
        {
            return Array.Empty<int>();
        }

        int[] colorIdArray = new int[colorIdList.Count];

        for (int index = 0; index < colorIdList.Count; index++)
        {
            colorIdArray[index] = colorIdList[index];
        }

        return colorIdArray;
    }
}
