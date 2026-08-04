using System.Collections.Generic;

public sealed class LevelPlayResourceDTO
{
    public readonly int Id;
    public readonly int Row;
    public readonly int Column;
    public readonly int ColorId;
    public readonly int Capacity;
    public readonly int AvailableCapacity;
    public readonly bool IsLocked;
    public readonly int WaitingCount;
    public readonly int[] OccupiedConnectionIdArray;
    public readonly LevelPlayResourceRuleDTO Rule;

    public LevelPlayResourceDTO(int id,
                                int row,
                                int column,
                                int colorId,
                                int capacity,
                                int availableCapacity,
                                bool isLocked,
                                int waitingCount,
                                IReadOnlyList<int> occupiedConnectionIdList,
                                LevelPlayResourceRuleDTO rule)
    {
        Id = id;
        Row = row;
        Column = column;
        ColorId = colorId;
        Capacity = capacity;
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
