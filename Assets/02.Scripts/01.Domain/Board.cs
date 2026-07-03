using System.Collections.Generic;

public sealed class Board
{
    private readonly List<ProcessNode> _processList;
    private readonly List<ResourceNode> _resourceList;
    private readonly List<Connection> _connectionList;
    private readonly List<IBoardRule> _boardRuleList;

    private int _nextConnectionId;

    public Board(IEnumerable<ProcessNode> processes, IEnumerable<ResourceNode> resources, IEnumerable<IBoardRule> boardRules)
    {
        _processList = new List<ProcessNode>(processes);
        _resourceList = new List<ResourceNode>(resources);
        _boardRuleList = new List<IBoardRule>(boardRules);
        _connectionList = new List<Connection>();
    }

    public ResourceNode GetResource(int resourceId)
    {
        return _resourceList.Find(resource => resource.Id == resourceId);
    }

    public AssignConnectionResult AssignConnection(int processId, int slotId, int resourceId)
    {
        ProcessNode process = _processList.Find(item => item.Id == processId);
        ResourceNode resource = GetResource(resourceId);

        if(process == null || resource == null || !process.TryGetSlot(slotId, out ProcessColorSlot slot))
        {
            return AssignConnectionResult.Fail(EAssignConnectionError.NotFound);;
        }

        if(slot.IsConnected || resource.IsLocked || !resource.HasAvailableCapacity)
        {
            return AssignConnectionResult.Fail(EAssignConnectionError.NotConnectable);
        }

        ConnectionContext context = new ConnectionContext(process, slot, resource);

        if (!resource.Rule.CanConnect(context))
        {
            return AssignConnectionResult.Fail(EAssignConnectionError.RuleRejected);
        }

        foreach(IBoardRule rule in _boardRuleList)
        {
            if(rule.HandlesResource(resourceId) && !rule.CanConnect(context, this))
            {
                return AssignConnectionResult.Fail(EAssignConnectionError.RuleRejected);
            }
        }

        Connection connection = new Connection(_nextConnectionId++, processId, slotId, resourceId);
        _connectionList.Add(connection);
        slot.AssignConnection(connection.Id);
        resource.AddConnection(connection.Id);

        RuleEffects effects = new RuleEffects();
        resource.Rule.OnConnected(context, effects);

        foreach(IBoardRule rule in _boardRuleList)
        {
            if(rule.HandlesResource(resourceId))
            {
                rule.OnConnected(context, this, effects);
            }
        }

        ApplyEffects(effects);
        return AssignConnectionResult.Ok(connection.Id);
    }

    public ResourceFocusInfo GetResourceFocusInfo(int resourceId)
    {
        ResourceFocusInfoBuilder builder = new ResourceFocusInfoBuilder(resourceId);

        foreach(IBoardRule rule in _boardRuleList)
        {
            if(rule.HandlesResource(resourceId))
            {
                rule.AddFocusInfo(resourceId, builder);
            }
        }

        return builder.Build();
    }

    private void ApplyEffects(RuleEffects effects)
    {
        foreach(int resourceId in effects.LockedResourceIdList)
        {
            GetResource(resourceId)?.SetLocked(true);
        }

        foreach(int resourceId in effects.UnlockedResourceIdList)
        {
            GetResource(resourceId)?.SetLocked(false);
        }

    }
}
