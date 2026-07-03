using System.Collections.Generic;

public sealed class RuleEffects
{
    public readonly List<int> LockedResourceIdList = new();
    public readonly List<int> UnlockedResourceIdList = new();

    public void LockResource(int resourceId)
    {
        if (!LockedResourceIdList.Contains(resourceId))
        {
            LockedResourceIdList.Add(resourceId);
        }
    }

    public void UnlockResource(int resourceId)
    {
        if (!UnlockedResourceIdList.Contains(resourceId))
        {
            UnlockedResourceIdList.Add(resourceId);
        }
    }
}
