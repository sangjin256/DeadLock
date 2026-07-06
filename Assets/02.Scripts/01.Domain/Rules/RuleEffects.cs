using System.Collections.Generic;

public sealed class RuleEffects
{
    public readonly HashSet<int> LockedResourceIdSet = new();

    public readonly HashSet<int> UnlockedResourceIdSet = new();

    public readonly Dictionary<int, ColorId> RelayColorByResourceIdDict = new();

    public readonly HashSet<int> ClearedRelayColorResourceIdSet = new();

    public void LockResource(int resourceId)
    {
        LockedResourceIdSet.Add(resourceId);
        UnlockedResourceIdSet.Remove(resourceId);
    }

    public void UnlockResource(int resourceId)
    {
        UnlockedResourceIdSet.Add(resourceId);
        LockedResourceIdSet.Remove(resourceId);
    }

    public void SetRelayColor(int resourceId, ColorId color)
    {
        RelayColorByResourceIdDict[resourceId] = color;
        ClearedRelayColorResourceIdSet.Remove(resourceId);
    }

    public void ClearRelayColor(int resourceId)
    {
        ClearedRelayColorResourceIdSet.Add(resourceId);
        RelayColorByResourceIdDict.Remove(resourceId);
    }
}
