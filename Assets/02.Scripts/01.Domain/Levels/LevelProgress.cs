using System.Collections.Generic;

public sealed class LevelProgress
{
    private readonly HashSet<int> _clearedLevelIdSet = new();

    private int _unlockedLevelId;
    public int UnlockedLevelId => _unlockedLevelId;

    public LevelProgress(int unlockedLevelId)
    {
        _unlockedLevelId = unlockedLevelId;
    }

    public bool IsCleared(int levelId)
    {
        return _clearedLevelIdSet.Contains(levelId);
    }

    public bool TryMarkCleared(int levelId)
    {
        bool added = _clearedLevelIdSet.Add(levelId);

        if(levelId >= _unlockedLevelId)
        {
            _unlockedLevelId = levelId + 1;
        }

        return added;
    }

    public int[] GetClearedLevelIds()
    {
        int[] result = new int[_clearedLevelIdSet.Count];
        _clearedLevelIdSet.CopyTo(result);
        return result;
    }
}