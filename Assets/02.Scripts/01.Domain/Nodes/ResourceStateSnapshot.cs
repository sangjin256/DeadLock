using System.Collections.Generic;

public sealed class ResourceStateSnapshot
{
    public readonly int Id;
    public readonly ColorId Color;
    public readonly int AvailableCapacity;
    public readonly bool IsLocked;
    public readonly int WaitingCount;
    public readonly int[] OccupiedConnectionIdArray;
    public readonly ResourceRuleStateSnapshot RuleState;

    public ResourceStateSnapshot(int id,
                                 ColorId color,
                                 int availableCapacity,
                                 bool isLocked,
                                 int waitingCount,
                                 IReadOnlyList<int> occupiedConnectionIdList,
                                 ResourceRuleStateSnapshot ruleState)
    {
        Id = id;
        Color = color;
        AvailableCapacity = availableCapacity;
        IsLocked = isLocked;
        WaitingCount = waitingCount;
        RuleState = ruleState ?? ResourceRuleStateSnapshot.None;
        OccupiedConnectionIdArray = new int[occupiedConnectionIdList.Count];

        for (int i = 0; i < occupiedConnectionIdList.Count; i++)
        {
            OccupiedConnectionIdArray[i] = occupiedConnectionIdList[i];
        }
    }
}
