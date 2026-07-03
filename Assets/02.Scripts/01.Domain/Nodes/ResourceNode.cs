using System.Collections.Generic;

public sealed class ResourceNode
{
    public readonly int Id;
    public readonly int Capacity;
    public readonly IResourceRule Rule;

    public bool HasAvailableCapacity => _connectionIdList.Count < Capacity;

    private ColorId _color;
    public ColorId Color => _color;
    private bool _isLocked;
    public bool IsLocked => _isLocked;

    private readonly List<int> _connectionIdList;
    public IReadOnlyList<int> ConnectionIdList => _connectionIdList;

    public ResourceNode(int id, ColorId color, int capacity, IResourceRule rule)
    {
        Id = id;
        _color = color;
        Capacity = capacity;
        Rule = rule ?? NoResourceRule.Instance;
        _connectionIdList = new List<int>();
    }

    public void AddConnection(int connectionId)
    {
        if (!_connectionIdList.Contains(connectionId))
        {
            _connectionIdList.Add(connectionId);
        }
    }

    public void RemoveConnection(int connectionId)
    {
        _connectionIdList.Remove(connectionId);
    }

    public void ChangeColor(ColorId newColor)
    {
        _color = newColor;
    }

    public void SetLocked(bool locked)
    {
        _isLocked = locked;
    }
}
