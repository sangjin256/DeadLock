using System;
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
        if (processes is null)
        {
            throw new ArgumentNullException(nameof(processes));
        }

        if (resources is null)
        {
            throw new ArgumentNullException(nameof(resources));
        }

        if (boardRules is null)
        {
            throw new ArgumentNullException(nameof(boardRules));
        }

        _processList = new List<ProcessNode>(processes);
        _resourceList = new List<ResourceNode>(resources);
        _boardRuleList = new List<IBoardRule>(boardRules);
        _connectionList = new List<Connection>();
    }

    public ProcessNode GetProcess(int processId)
    {
        return _processList.Find(process => process.Id == processId);
    }

    public ResourceNode GetResource(int resourceId)
    {
        return _resourceList.Find(resource => resource.Id == resourceId);
    }

    public Connection GetConnection(int connectionId)
    {
        return _connectionList.Find(connection => connection.Id == connectionId);
    }

    public AssignConnectionResult AssignConnection(int processId, int slotId, int resourceId)
    {
        ProcessNode process = GetProcess(processId);
        ResourceNode resource = GetResource(resourceId);

        if (process is null || resource is null || !process.TryGetSlot(slotId, out ProcessColorSlot slot))
        {
            return AssignConnectionResult.Fail(EAssignConnectionError.NotFound);
        }

        if (slot.IsConnected)
        {
            return AssignConnectionResult.Fail(EAssignConnectionError.NotConnectable);
        }

        ConnectionContext context = new ConnectionContext(process, slot, resource);

        bool canReserve = resource.Rule.CanReserve(context);

        foreach (IBoardRule rule in _boardRuleList)
        {
            if (!rule.HandlesResource(resourceId))
            {
                continue;
            }

            bool canReserveByRule = rule.CanReserve(context, this);

            if (!canReserveByRule)
            {
                return AssignConnectionResult.Fail(EAssignConnectionError.RuleRejected);
            }

            canReserve = canReserve || canReserveByRule;
        }

        if (!canReserve)
        {
            return AssignConnectionResult.Fail(EAssignConnectionError.RuleRejected);
        }

        Connection connection = new Connection(_nextConnectionId++, processId, slotId, resourceId);
        _connectionList.Add(connection);
        slot.AssignConnection(connection.Id);

        return AssignConnectionResult.Ok(connection.Id);
    }

    public SimulationReport RunSimulation(int maxRoundCount)
    {
        ResetSimulationState();

        List<RoundResult> roundResultList = new List<RoundResult>();

        if (maxRoundCount <= 0 || !AreAllSlotsReserved())
        {
            return CreateSimulationReport(ESimulationEndState.Failed, roundResultList);
        }

        List<RoundScheduleItem> scheduleItemList = CreateScheduleItemList();
        List<RoundScheduleItem> priorityScheduleItemList = new List<RoundScheduleItem>();

        for (int round = 0; round < maxRoundCount; round++)
        {
            RoundResult roundResult = new RoundResult(round);
            List<RoundScheduleItem> currentPriorityScheduleItemList = priorityScheduleItemList;
            priorityScheduleItemList = new List<RoundScheduleItem>();

            currentPriorityScheduleItemList.Sort(CompareScheduleItems);

            foreach (RoundScheduleItem item in currentPriorityScheduleItemList)
            {
                TryExecuteScheduleItem(item, roundResult);
            }

            List<RoundScheduleItem> currentRoundScheduleItemList = GetScheduleItemsForRound(scheduleItemList, round);

            foreach (RoundScheduleItem item in currentRoundScheduleItemList)
            {
                TryExecuteScheduleItem(item, roundResult);
            }

            CompleteReadyProcesses(roundResult);
            ApplyRoundEndedEffects(round);
            RequeueWaitingRequests(round + 1, roundResult, priorityScheduleItemList);

            roundResultList.Add(roundResult);

            if (AreAllProcessesCompleted())
            {
                return CreateSimulationReport(ESimulationEndState.Succeeded, roundResultList);
            }

            if (IsDeadlocked(priorityScheduleItemList))
            {
                return CreateSimulationReport(ESimulationEndState.Deadlocked, roundResultList);
            }
        }

        BlockUnresolvedConnections();
        return CreateSimulationReport(ESimulationEndState.Failed, roundResultList);
    }

    public ResourceFocusInfo GetResourceFocusInfo(int resourceId)
    {
        ResourceFocusInfoBuilder builder = new ResourceFocusInfoBuilder(resourceId);

        foreach (IBoardRule rule in _boardRuleList)
        {
            if (rule.HandlesResource(resourceId))
            {
                rule.AddFocusInfo(resourceId, builder);
            }
        }

        return builder.Build();
    }

    private void ResetSimulationState()
    {
        foreach (ProcessNode process in _processList)
        {
            process.ResetSimulationState();
        }

        foreach (ResourceNode resource in _resourceList)
        {
            resource.ResetSimulationState();
        }

        foreach (Connection connection in _connectionList)
        {
            connection.ResetSimulationState();
        }
    }

    private bool AreAllSlotsReserved()
    {
        foreach (ProcessNode process in _processList)
        {
            foreach (ProcessColorSlot slot in process.ColorSlotList)
            {
                if (!slot.IsConnected)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private List<RoundScheduleItem> CreateScheduleItemList()
    {
        List<RoundScheduleItem> scheduleItemList = new List<RoundScheduleItem>();

        foreach (ProcessNode process in _processList)
        {
            for (int slotIndex = 0; slotIndex < process.ColorSlotList.Count; slotIndex++)
            {
                ProcessColorSlot slot = process.ColorSlotList[slotIndex];
                Connection connection = GetConnection(slot.ConnectionId);

                if (connection is null)
                {
                    continue;
                }

                ResourceNode resource = GetResource(connection.ResourceId);

                if (resource is null)
                {
                    continue;
                }

                scheduleItemList.Add(CreateScheduleItem(process, slot, connection, resource, slotIndex, false));
            }
        }

        scheduleItemList.Sort(CompareScheduleItems);
        return scheduleItemList;
    }

    private RoundScheduleItem CreateScheduleItem(
        ProcessNode process,
        ProcessColorSlot slot,
        Connection connection,
        ResourceNode resource,
        int roundIndex,
        bool isPriorityFromWaiting)
    {
        int distance = process.Position.GetManhattanDistance(resource.Position);

        return new RoundScheduleItem(
            process.Id,
            slot.Id,
            connection.Id,
            resource.Id,
            roundIndex,
            distance,
            slot.SelectionOrder,
            isPriorityFromWaiting);
    }

    private List<RoundScheduleItem> GetScheduleItemsForRound(List<RoundScheduleItem> scheduleItemList, int round)
    {
        List<RoundScheduleItem> resultList = new List<RoundScheduleItem>();

        foreach (RoundScheduleItem item in scheduleItemList)
        {
            if (item.RoundIndex == round)
            {
                resultList.Add(item);
            }
        }

        resultList.Sort(CompareScheduleItems);
        return resultList;
    }

    private static int CompareScheduleItems(RoundScheduleItem left, RoundScheduleItem right)
    {
        int result = left.Distance.CompareTo(right.Distance);

        if (result != 0)
        {
            return result;
        }

        result = left.SelectionOrder.CompareTo(right.SelectionOrder);

        if (result != 0)
        {
            return result;
        }

        result = left.ProcessId.CompareTo(right.ProcessId);

        if (result != 0)
        {
            return result;
        }

        return left.SlotId.CompareTo(right.SlotId);
    }

    private void TryExecuteScheduleItem(RoundScheduleItem item, RoundResult roundResult)
    {
        ProcessNode process = GetProcess(item.ProcessId);
        ResourceNode resource = GetResource(item.ResourceId);
        Connection connection = GetConnection(item.ConnectionId);

        if (process is null || resource is null || connection is null)
        {
            return;
        }

        if (!process.TryGetSlot(item.SlotId, out ProcessColorSlot slot))
        {
            return;
        }

        if (process.State == EProcessState.Completed ||
            process.State == EProcessState.Failed ||
            slot.IsCompleted ||
            connection.State == EConnectionState.Completed ||
            connection.State == EConnectionState.Occupied ||
            connection.State == EConnectionState.Blocked)
        {
            return;
        }

        if (process.State == EProcessState.Waiting && !item.IsPriorityFromWaiting)
        {
            return;
        }

        ConnectionContext context = new ConnectionContext(process, slot, resource);

        if (!CanOccupy(context))
        {
            WaitForResource(process, resource, connection, slot, roundResult);
            return;
        }

        OccupyResource(context, connection, roundResult);
    }

    private bool CanOccupy(ConnectionContext context)
    {
        if (context.Resource.IsLocked || !context.Resource.HasAvailableCapacity)
        {
            return false;
        }

        if (!context.Resource.Rule.CanOccupy(context))
        {
            return false;
        }

        foreach (IBoardRule rule in _boardRuleList)
        {
            if (rule.HandlesResource(context.Resource.Id) && !rule.CanOccupy(context, this))
            {
                return false;
            }
        }

        return true;
    }

    private void WaitForResource(
        ProcessNode process,
        ResourceNode resource,
        Connection connection,
        ProcessColorSlot slot,
        RoundResult roundResult)
    {
        connection.Wait();
        process.Wait();
        resource.EnqueueWaiting(new WaitingRequest(process.Id, slot.Id, connection.Id, resource.Id));
        roundResult.AddWaitingConnection(connection.Id);
    }

    private void OccupyResource(ConnectionContext context, Connection connection, RoundResult roundResult)
    {
        context.Resource.OccupyConnection(connection.Id);
        context.Slot.Complete();
        connection.Occupy();
        context.Process.Run();

        RuleEffects effects = new RuleEffects();
        context.Resource.Rule.OnOccupied(context, effects);

        foreach (IBoardRule rule in _boardRuleList)
        {
            if (rule.HandlesResource(context.Resource.Id))
            {
                rule.OnOccupied(context, this, effects);
            }
        }

        ApplyEffects(effects);
        roundResult.AddOccupiedConnection(connection.Id);
    }

    private void CompleteReadyProcesses(RoundResult roundResult)
    {
        foreach (ProcessNode process in _processList)
        {
            if (process.State == EProcessState.Completed || !process.IsCompleted())
            {
                continue;
            }

            if (!CanFinishProcess(process))
            {
                continue;
            }

            process.Complete();
            roundResult.AddCompletedProcess(process.Id);
            ReleaseProcessResources(process, roundResult);
        }
    }

    private bool CanFinishProcess(ProcessNode process)
    {
        foreach (Connection connection in _connectionList)
        {
            if (connection.ProcessId != process.Id || connection.State != EConnectionState.Occupied)
            {
                continue;
            }

            ResourceNode resource = GetResource(connection.ResourceId);

            if (resource is not null && !resource.Rule.CanFinish(resource))
            {
                return false;
            }
        }

        return true;
    }

    private void ReleaseProcessResources(ProcessNode process, RoundResult roundResult)
    {
        foreach (Connection connection in _connectionList)
        {
            if (connection.ProcessId != process.Id || connection.State != EConnectionState.Occupied)
            {
                continue;
            }

            ResourceNode resource = GetResource(connection.ResourceId);

            if (resource is null)
            {
                continue;
            }

            if (!process.TryGetSlot(connection.ColorSlotId, out ProcessColorSlot slot))
            {
                continue;
            }

            ConnectionContext context = new ConnectionContext(process, slot, resource);
            resource.ReleaseConnection(connection.Id);
            connection.Complete();

            RuleEffects effects = new RuleEffects();
            resource.Rule.OnReleased(resource, effects);

            foreach (IBoardRule rule in _boardRuleList)
            {
                if (rule.HandlesResource(resource.Id))
                {
                    rule.OnReleased(context, this, effects);
                }
            }

            ApplyEffects(effects);
            roundResult.AddReleasedConnection(connection.Id);
        }
    }

    private void ApplyRoundEndedEffects(int round)
    {
        foreach (ResourceNode resource in _resourceList)
        {
            RuleEffects effects = new RuleEffects();
            resource.Rule.OnRoundEnded(resource, round, effects);
            ApplyEffects(effects);
        }
    }

    private void RequeueWaitingRequests(
        int roundIndex,
        RoundResult roundResult,
        List<RoundScheduleItem> priorityScheduleItemList)
    {
        foreach (ResourceNode resource in _resourceList)
        {
            int availableCount = resource.AvailableCapacity;

            for (int i = 0; i < availableCount; i++)
            {
                if (!resource.TryDequeueWaiting(out WaitingRequest request))
                {
                    break;
                }

                RoundScheduleItem item = CreateScheduleItemFromWaitingRequest(request, roundIndex);

                if (item is null)
                {
                    continue;
                }

                priorityScheduleItemList.Add(item);
                roundResult.AddRequeuedConnection(request.ConnectionId);
            }
        }
    }

    private RoundScheduleItem CreateScheduleItemFromWaitingRequest(WaitingRequest request, int roundIndex)
    {
        ProcessNode process = GetProcess(request.ProcessId);
        ResourceNode resource = GetResource(request.ResourceId);
        Connection connection = GetConnection(request.ConnectionId);

        if (process is null || resource is null || connection is null)
        {
            return null;
        }

        if (!process.TryGetSlot(request.SlotId, out ProcessColorSlot slot))
        {
            return null;
        }

        int distance = process.Position.GetManhattanDistance(resource.Position);

        return RoundScheduleItem.FromWaitingRequest(
            request,
            roundIndex,
            distance,
            slot.SelectionOrder);
    }

    private bool AreAllProcessesCompleted()
    {
        foreach (ProcessNode process in _processList)
        {
            if (process.State != EProcessState.Completed)
            {
                return false;
            }
        }

        return true;
    }

    private bool IsDeadlocked(List<RoundScheduleItem> priorityScheduleItemList)
    {
        if (priorityScheduleItemList.Count > 0)
        {
            return false;
        }

        bool hasUnfinishedProcess = false;

        foreach (ProcessNode process in _processList)
        {
            if (process.State == EProcessState.Completed)
            {
                continue;
            }

            hasUnfinishedProcess = true;

            if (process.State != EProcessState.Waiting)
            {
                return false;
            }
        }

        return hasUnfinishedProcess;
    }

    private void BlockUnresolvedConnections()
    {
        foreach (Connection connection in _connectionList)
        {
            if (connection.State == EConnectionState.Completed)
            {
                continue;
            }

            connection.Block();
        }

        foreach (ProcessNode process in _processList)
        {
            if (process.State != EProcessState.Completed)
            {
                process.Fail();
            }
        }
    }

    private SimulationReport CreateSimulationReport(
        ESimulationEndState endState,
        IReadOnlyList<RoundResult> roundResultList)
    {
        return new SimulationReport(
            endState,
            roundResultList,
            GetCompletedProcessIdList(),
            GetBlockedConnectionIdList());
    }

    private int[] GetCompletedProcessIdList()
    {
        List<int> idList = new List<int>();

        foreach (ProcessNode process in _processList)
        {
            if (process.State == EProcessState.Completed)
            {
                idList.Add(process.Id);
            }
        }

        return idList.ToArray();
    }

    private int[] GetBlockedConnectionIdList()
    {
        List<int> idList = new List<int>();

        foreach (Connection connection in _connectionList)
        {
            if (connection.State == EConnectionState.Blocked ||
                connection.State == EConnectionState.Waiting)
            {
                idList.Add(connection.Id);
            }
        }

        return idList.ToArray();
    }

    private void ApplyEffects(RuleEffects effects)
    {
        foreach (int resourceId in effects.LockedResourceIdSet)
        {
            GetResource(resourceId)?.SetLocked(true);
        }

        foreach (int resourceId in effects.UnlockedResourceIdSet)
        {
            GetResource(resourceId)?.SetLocked(false);
        }

        foreach (KeyValuePair<int, ColorId> pair in effects.RelayColorByResourceIdDict)
        {
            GetResource(pair.Key)?.SetRelayColor(pair.Value);
        }

        foreach (int resourceId in effects.ClearedRelayColorResourceIdSet)
        {
            GetResource(resourceId)?.ClearRelayColor();
        }
    }
}
