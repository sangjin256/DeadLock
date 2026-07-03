using System.Collections.Generic;

public sealed class RuleEffects
{
    public readonly HashSet<int> LockedResourceIdSet = new();

    public readonly HashSet<int> UnlockedResourceIdSet = new();

    public void LockResource(int resourceId)
    {
        LockedResourceIdSet.Add(resourceId);
    }

    public void UnlockResource(int resourceId)
    {
        UnlockedResourceIdSet.Add(resourceId);
    }
}
