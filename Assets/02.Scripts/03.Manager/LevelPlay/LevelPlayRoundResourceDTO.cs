using System.Collections.Generic;

public sealed class LevelPlayRoundResourceDTO
{
    public readonly int Id;
    public readonly int ColorId;
    public readonly int AvailableCapacity;
    public readonly bool IsLocked;
    public readonly int WaitingCount;
    public readonly int[] OccupiedConnectionIdArray;
    public readonly LevelPlayResourceRuleDTO Rule;

    public LevelPlayRoundResourceDTO(int id,
                                     int colorId,
                                     int availableCapacity,
                                     bool isLocked,
                                     int waitingCount,
                                     IReadOnlyList<int> occupiedConnectionIdList,
                                     LevelPlayResourceRuleDTO rule)
    {
        Id = id;
        ColorId = colorId;
        AvailableCapacity = availableCapacity;
        IsLocked = isLocked;
        WaitingCount = waitingCount;
        Rule = rule;
        OccupiedConnectionIdArray = new int[occupiedConnectionIdList.Count];

        for (int i = 0; i < occupiedConnectionIdList.Count; i++)
        {
            OccupiedConnectionIdArray[i] = occupiedConnectionIdList[i];
        }
    }
}
