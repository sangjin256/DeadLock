using System;
using System.Collections.Generic;

public sealed class LevelPlayManager
{
    public event Action<LevelPlayDTO> OnLevelChanged;
    public event Action<LevelSimulationDTO> OnSimulationReady;
    public event Action<ResourceFocusDTO> OnResourceFocusChanged;

    private readonly LevelBoardFactory _boardFactory;
    private readonly LevelDefinitionValidator _definitionValidator;
    private readonly List<PlannedConnection> _plannedConnectionList = new();

    private LevelDefinition _definition;
    private LevelPlaySettings _settings;
    private Board _board;
    private LevelPlayDTO _currentLevel;
    private LevelSimulationDTO _currentSimulation;
    private ELevelPlayPhase _phase = ELevelPlayPhase.NotLoaded;

    public LevelPlayDTO CurrentLevel => _currentLevel;
    public LevelSimulationDTO CurrentSimulation => _currentSimulation;

    public LevelPlayManager(LevelBoardFactory boardFactory,
                            LevelDefinitionValidator definitionValidator)
    {
        _boardFactory = boardFactory ?? throw new ArgumentNullException(nameof(boardFactory));
        _definitionValidator = definitionValidator ?? throw new ArgumentNullException(nameof(definitionValidator));
    }

    public LevelPlayCommandResult LoadLevel(LevelDefinition definition, LevelPlaySettings settings)
    {
        if (definition is null)
        {
            return LevelPlayCommandResult.Fail(ELevelPlayCommandError.InvalidDefinition);
        }

        LevelValidationResult validationResult = _definitionValidator.Validate(definition);

        if (!validationResult.IsValid)
        {
            return LevelPlayCommandResult.Fail(ELevelPlayCommandError.InvalidDefinition,
                                               null,
                                               CreateValidationMessageList(validationResult));
        }

        if (settings is null || !settings.IsValid)
        {
            return LevelPlayCommandResult.Fail(ELevelPlayCommandError.InvalidSettings);
        }

        _definition = definition;
        _settings = settings;
        _board = _boardFactory.CreateBoard(definition);
        _plannedConnectionList.Clear();
        _currentSimulation = null;
        _phase = ELevelPlayPhase.Planning;
        RefreshCurrentLevel();
        PublishLevelChanged();

        return LevelPlayCommandResult.Ok(_currentLevel);
    }

    public LevelPlayCommandResult AssignConnection(int processId, int slotId, int resourceId)
    {
        if (!IsPlanning())
        {
            return LevelPlayCommandResult.Fail(GetPhaseError(), _currentLevel);
        }

        ProcessNode process = _board.GetProcess(processId);
        ResourceNode resource = _board.GetResource(resourceId);

        if (process is null || !process.TryGetSlot(slotId, out ProcessColorSlot slot))
        {
            return LevelPlayCommandResult.Fail(ELevelPlayCommandError.AssignmentRejected, _currentLevel);
        }

        if (resource is null)
        {
            return LevelPlayCommandResult.Fail(ELevelPlayCommandError.ResourceNotFound, _currentLevel);
        }

        if (slot.IsConnected)
        {
            return LevelPlayCommandResult.Fail(ELevelPlayCommandError.SlotAlreadyAssigned, _currentLevel);
        }

        AssignConnectionResult result = _board.AssignConnection(processId, slotId, resourceId);

        if (!result.Success)
        {
            return LevelPlayCommandResult.Fail(ELevelPlayCommandError.AssignmentRejected, _currentLevel);
        }

        int selectionOrder = GetNextSelectionOrder(processId);
        slot.SetOrder(selectionOrder);
        _plannedConnectionList.Add(new PlannedConnection(result.ConnectionId,
                                                         processId,
                                                         slotId,
                                                         resourceId,
                                                         selectionOrder));
        RefreshCurrentLevel();
        PublishLevelChanged();

        return LevelPlayCommandResult.Ok(_currentLevel, result.ConnectionId);
    }

    public LevelPlayCommandResult RemoveConnection(int connectionId)
    {
        if (!IsPlanning())
        {
            return LevelPlayCommandResult.Fail(GetPhaseError(), _currentLevel);
        }

        int planIndex = FindPlannedConnectionIndex(connectionId);

        if (planIndex < 0 || !_board.RemoveConnection(connectionId))
        {
            return LevelPlayCommandResult.Fail(ELevelPlayCommandError.ConnectionNotFound, _currentLevel);
        }

        int processId = _plannedConnectionList[planIndex].ProcessId;
        _plannedConnectionList.RemoveAt(planIndex);
        NormalizeSelectionOrder(processId);
        RefreshCurrentLevel();
        PublishLevelChanged();

        return LevelPlayCommandResult.Ok(_currentLevel);
    }

    public LevelPlayCommandResult ReplaceConnection(int processId, int slotId, int resourceId)
    {
        if (!IsPlanning())
        {
            return LevelPlayCommandResult.Fail(GetPhaseError(), _currentLevel);
        }

        ProcessNode process = _board.GetProcess(processId);
        ResourceNode targetResource = _board.GetResource(resourceId);

        if (process is null || !process.TryGetSlot(slotId, out ProcessColorSlot slot))
        {
            return LevelPlayCommandResult.Fail(ELevelPlayCommandError.AssignmentRejected, _currentLevel);
        }

        if (targetResource is null)
        {
            return LevelPlayCommandResult.Fail(ELevelPlayCommandError.ResourceNotFound, _currentLevel);
        }

        if (!slot.IsConnected)
        {
            return AssignConnection(processId, slotId, resourceId);
        }

        Connection existingConnection = _board.GetConnection(slot.ConnectionId);

        if (existingConnection is null)
        {
            return LevelPlayCommandResult.Fail(ELevelPlayCommandError.ConnectionNotFound, _currentLevel);
        }

        if (existingConnection.ResourceId == resourceId)
        {
            return LevelPlayCommandResult.Ok(_currentLevel, existingConnection.Id);
        }

        int planIndex = FindPlannedConnectionIndex(existingConnection.Id);

        if (planIndex < 0)
        {
            return LevelPlayCommandResult.Fail(ELevelPlayCommandError.ConnectionNotFound, _currentLevel);
        }

        PlannedConnection previousPlan = _plannedConnectionList[planIndex];

        if (!_board.RemoveConnection(existingConnection.Id))
        {
            return LevelPlayCommandResult.Fail(ELevelPlayCommandError.ConnectionNotFound, _currentLevel);
        }

        _plannedConnectionList.RemoveAt(planIndex);

        AssignConnectionResult assignmentResult = _board.AssignConnection(processId, slotId, resourceId);

        if (assignmentResult.Success)
        {
            slot.SetOrder(previousPlan.SelectionOrder);
            _plannedConnectionList.Insert(planIndex,
                                          new PlannedConnection(assignmentResult.ConnectionId,
                                                                processId,
                                                                slotId,
                                                                resourceId,
                                                                previousPlan.SelectionOrder));
            RefreshCurrentLevel();
            PublishLevelChanged();

            return LevelPlayCommandResult.Ok(_currentLevel, assignmentResult.ConnectionId);
        }

        AssignConnectionResult restoreResult = _board.AssignConnection(previousPlan.ProcessId,
                                                                        previousPlan.SlotId,
                                                                        previousPlan.ResourceId);

        if (!restoreResult.Success)
        {
            RefreshCurrentLevel();
            PublishLevelChanged();
            return LevelPlayCommandResult.Fail(ELevelPlayCommandError.RestoreFailed, _currentLevel);
        }

        slot.SetOrder(previousPlan.SelectionOrder);
        _plannedConnectionList.Insert(planIndex,
                                      new PlannedConnection(restoreResult.ConnectionId,
                                                            previousPlan.ProcessId,
                                                            previousPlan.SlotId,
                                                            previousPlan.ResourceId,
                                                            previousPlan.SelectionOrder));
        RefreshCurrentLevel();
        PublishLevelChanged();

        return LevelPlayCommandResult.Fail(ELevelPlayCommandError.AssignmentRejected, _currentLevel);
    }

    public LevelPlayCommandResult StartSimulation()
    {
        if (!IsPlanning())
        {
            return LevelPlayCommandResult.Fail(GetPhaseError(), _currentLevel);
        }

        if (!HasCompletePlan())
        {
            return LevelPlayCommandResult.Fail(ELevelPlayCommandError.IncompletePlan, _currentLevel);
        }

        SimulationReport report = _board.RunSimulation(_settings.OneStarRoundCount);
        _currentSimulation = CreateSimulationDTO(report);
        _phase = ELevelPlayPhase.ResultReady;
        RefreshCurrentLevel();
        PublishLevelChanged();
        OnSimulationReady?.Invoke(_currentSimulation);

        return LevelPlayCommandResult.Ok(_currentLevel, -1, _currentSimulation);
    }

    public LevelPlayCommandResult RestartLevel()
    {
        if (_board is null)
        {
            return LevelPlayCommandResult.Fail(ELevelPlayCommandError.LevelNotLoaded);
        }

        _board = _boardFactory.CreateBoard(_definition);

        for (int i = 0; i < _plannedConnectionList.Count; i++)
        {
            PlannedConnection plan = _plannedConnectionList[i];
            AssignConnectionResult result = _board.AssignConnection(plan.ProcessId, plan.SlotId, plan.ResourceId);

            if (!result.Success)
            {
                RefreshCurrentLevel();
                PublishLevelChanged();
                return LevelPlayCommandResult.Fail(ELevelPlayCommandError.RestoreFailed, _currentLevel);
            }

            ProcessNode process = _board.GetProcess(plan.ProcessId);

            if (process is null || !process.TryGetSlot(plan.SlotId, out ProcessColorSlot slot))
            {
                RefreshCurrentLevel();
                PublishLevelChanged();
                return LevelPlayCommandResult.Fail(ELevelPlayCommandError.RestoreFailed, _currentLevel);
            }

            plan.ConnectionId = result.ConnectionId;
            slot.SetOrder(plan.SelectionOrder);
        }

        _currentSimulation = null;
        _phase = ELevelPlayPhase.Planning;
        RefreshCurrentLevel();
        PublishLevelChanged();

        return LevelPlayCommandResult.Ok(_currentLevel);
    }

    public ResourceFocusDTO FocusResource(int resourceId)
    {
        if (_board is null)
        {
            return ResourceFocusDTO.Fail(ELevelPlayCommandError.LevelNotLoaded);
        }

        if (_board.GetResource(resourceId) is null)
        {
            return ResourceFocusDTO.Fail(ELevelPlayCommandError.ResourceNotFound);
        }

        ResourceFocusInfo focusInfo = _board.GetResourceFocusInfo(resourceId);
        ResourceFocusDTO focus = ResourceFocusDTO.Ok(focusInfo.FocusResourceId,
                                                      ToFocusKind(focusInfo.FocusKind),
                                                      focusInfo.HighlightedResourceIdList,
                                                      focusInfo.HighlightedConnectionIdList,
                                                      focusInfo.ActivateBoardRuleIdList);
        OnResourceFocusChanged?.Invoke(focus);

        return focus;
    }

    private bool IsPlanning()
    {
        return _board is not null && _phase == ELevelPlayPhase.Planning;
    }

    private ELevelPlayCommandError GetPhaseError()
    {
        return _board is null ? ELevelPlayCommandError.LevelNotLoaded : ELevelPlayCommandError.InvalidPhase;
    }

    private int GetNextSelectionOrder(int processId)
    {
        int count = 0;

        for (int i = 0; i < _plannedConnectionList.Count; i++)
        {
            if (_plannedConnectionList[i].ProcessId == processId)
            {
                count++;
            }
        }

        return count;
    }

    private int FindPlannedConnectionIndex(int connectionId)
    {
        for (int i = 0; i < _plannedConnectionList.Count; i++)
        {
            if (_plannedConnectionList[i].ConnectionId == connectionId)
            {
                return i;
            }
        }

        return -1;
    }

    private void NormalizeSelectionOrder(int processId)
    {
        ProcessNode process = _board.GetProcess(processId);

        if (process is null)
        {
            return;
        }

        int selectionOrder = 0;

        for (int i = 0; i < _plannedConnectionList.Count; i++)
        {
            PlannedConnection plan = _plannedConnectionList[i];

            if (plan.ProcessId != processId)
            {
                continue;
            }

            if (process.TryGetSlot(plan.SlotId, out ProcessColorSlot slot))
            {
                slot.SetOrder(selectionOrder);
            }

            plan.SelectionOrder = selectionOrder;
            selectionOrder++;
        }
    }

    private bool HasCompletePlan()
    {
        int slotCount = 0;

        for (int i = 0; i < _definition.ProcessList.Length; i++)
        {
            slotCount += _definition.ProcessList[i].SlotList.Length;
        }

        return _plannedConnectionList.Count == slotCount;
    }

    private List<string> CreateValidationMessageList(LevelValidationResult validationResult)
    {
        List<string> messageList = new List<string>(validationResult.ErrorList.Length);

        for (int i = 0; i < validationResult.ErrorList.Length; i++)
        {
            messageList.Add(validationResult.ErrorList[i].Message);
        }

        return messageList;
    }

    private void RefreshCurrentLevel()
    {
        List<LevelPlayProcessDTO> processList = CreateProcessDTOList();
        List<LevelPlayResourceDTO> resourceList = CreateResourceDTOList();
        List<LevelPlayConnectionDTO> connectionList = CreateConnectionDTOList();
        List<LevelPlayRelayDTO> relayList = CreateRelayDTOList(_board.CreateRelayStateSnapshotArray());
        _currentLevel = new LevelPlayDTO(_definition.Id,
                                         _phase,
                                         IsPlanning() && HasCompletePlan(),
                                         processList,
                                         resourceList,
                                         connectionList,
                                         relayList);
    }

    private List<LevelPlayProcessDTO> CreateProcessDTOList()
    {
        List<LevelPlayProcessDTO> processDTOList = new List<LevelPlayProcessDTO>(_definition.ProcessList.Length);

        for (int i = 0; i < _definition.ProcessList.Length; i++)
        {
            ProcessDefinition definition = _definition.ProcessList[i];
            ProcessNode process = _board.GetProcess(definition.Id);
            List<LevelPlaySlotDTO> slotDTOList = new List<LevelPlaySlotDTO>(process.ColorSlotList.Count);

            for (int slotIndex = 0; slotIndex < process.ColorSlotList.Count; slotIndex++)
            {
                ProcessColorSlot slot = process.ColorSlotList[slotIndex];
                slotDTOList.Add(new LevelPlaySlotDTO(slot.Id,
                                                     slot.RequiredColor.Value,
                                                     slot.SelectionOrder,
                                                     slot.ConnectionId,
                                                     slot.IsCompleted));
            }

            processDTOList.Add(new LevelPlayProcessDTO(process.Id,
                                                        process.Position.Row,
                                                        process.Position.Column,
                                                        ToProcessState(process.State),
                                                        slotDTOList));
        }

        return processDTOList;
    }

    private List<LevelPlayResourceDTO> CreateResourceDTOList()
    {
        List<LevelPlayResourceDTO> resourceDTOList = new List<LevelPlayResourceDTO>(_definition.ResourceList.Length);

        for (int i = 0; i < _definition.ResourceList.Length; i++)
        {
            ResourceDefinition definition = _definition.ResourceList[i];
            ResourceNode resource = _board.GetResource(definition.Id);
            resourceDTOList.Add(new LevelPlayResourceDTO(resource.Id,
                                                          resource.Position.Row,
                                                          resource.Position.Column,
                                                          resource.Color.Value,
                                                          resource.Capacity,
                                                          resource.AvailableCapacity,
                                                          resource.IsLocked,
                                                          resource.WaitingCount,
                                                          resource.OccupiedConnectionIdList,
                                                          CreateResourceRuleDTO(resource.Rule.CreateStateSnapshot())));
        }

        return resourceDTOList;
    }

    private List<LevelPlayConnectionDTO> CreateConnectionDTOList()
    {
        List<LevelPlayConnectionDTO> connectionDTOList = new List<LevelPlayConnectionDTO>(_plannedConnectionList.Count);

        for (int i = 0; i < _plannedConnectionList.Count; i++)
        {
            PlannedConnection plan = _plannedConnectionList[i];
            Connection connection = _board.GetConnection(plan.ConnectionId);

            if (connection is null)
            {
                continue;
            }

            connectionDTOList.Add(new LevelPlayConnectionDTO(connection.Id,
                                                              connection.ProcessId,
                                                              connection.ColorSlotId,
                                                              connection.ResourceId,
                                                              ToConnectionState(connection.State)));
        }

        return connectionDTOList;
    }

    private LevelSimulationDTO CreateSimulationDTO(SimulationReport report)
    {
        List<LevelRoundDTO> roundDTOList = new List<LevelRoundDTO>(report.RoundResultList.Length);

        for (int i = 0; i < report.RoundResultList.Length; i++)
        {
            RoundResult roundResult = report.RoundResultList[i];
            roundDTOList.Add(new LevelRoundDTO(roundResult.RoundIndex,
                                               roundResult.OccupiedConnectionIdList,
                                               roundResult.WaitingConnectionIdList,
                                               roundResult.RequeuedConnectionIdList,
                                               roundResult.DeferredConnectionIdList,
                                               roundResult.CompletedProcessIdList,
                                               roundResult.ReleasedConnectionIdList,
                                               roundResult.FailedProcessIdList,
                                               roundResult.BlockedConnectionIdList,
                                               CreateRoundResourceDTOList(roundResult.ResourceStateSnapshotList),
                                               CreateRelayDTOList(roundResult.RelayStateSnapshotList)));
        }

        int clearRoundCount = report.EndState == ESimulationEndState.Succeeded ? report.RoundResultList.Length : -1;
        int starCount = CalculateStarCount(report.EndState, clearRoundCount);

        return new LevelSimulationDTO(ToSimulationEndState(report.EndState),
                                      starCount,
                                      clearRoundCount,
                                      roundDTOList,
                                      report.CompletedProcessIdList,
                                      report.BlockedConnectionIdList);
    }

    private List<LevelPlayRoundResourceDTO> CreateRoundResourceDTOList(
        IReadOnlyList<ResourceStateSnapshot> resourceStateSnapshotList)
    {
        List<LevelPlayRoundResourceDTO> resourceDTOList = new List<LevelPlayRoundResourceDTO>(resourceStateSnapshotList.Count);

        for (int i = 0; i < resourceStateSnapshotList.Count; i++)
        {
            ResourceStateSnapshot snapshot = resourceStateSnapshotList[i];
            resourceDTOList.Add(new LevelPlayRoundResourceDTO(snapshot.Id,
                                                               snapshot.Color.Value,
                                                               snapshot.AvailableCapacity,
                                                               snapshot.IsLocked,
                                                               snapshot.WaitingCount,
                                                               snapshot.OccupiedConnectionIdArray,
                                                               CreateResourceRuleDTO(snapshot.RuleState)));
        }

        return resourceDTOList;
    }

    private List<LevelPlayRelayDTO> CreateRelayDTOList(IReadOnlyList<RelayStateSnapshot> relayStateSnapshotList)
    {
        List<LevelPlayRelayDTO> relayDTOList = new List<LevelPlayRelayDTO>(relayStateSnapshotList.Count);

        for (int i = 0; i < relayStateSnapshotList.Count; i++)
        {
            RelayStateSnapshot snapshot = relayStateSnapshotList[i];
            relayDTOList.Add(new LevelPlayRelayDTO(snapshot.Id,
                                                    snapshot.FirstResourceId,
                                                    snapshot.SecondResourceId,
                                                    ToRelayType(snapshot.RelayType),
                                                    snapshot.SenderResourceId,
                                                    snapshot.IsActive));
        }

        return relayDTOList;
    }

    private static LevelPlayResourceRuleDTO CreateResourceRuleDTO(ResourceRuleStateSnapshot snapshot)
    {
        List<int> colorIdList = new List<int>(snapshot.ColorSwitchColorArray.Length);

        for (int i = 0; i < snapshot.ColorSwitchColorArray.Length; i++)
        {
            colorIdList.Add(snapshot.ColorSwitchColorArray[i].Value);
        }

        return new LevelPlayResourceRuleDTO(colorIdList,
                                            snapshot.ColorSwitchCurrentIndex,
                                            snapshot.HasClock,
                                            snapshot.IsClockOpen,
                                            snapshot.ClockRemainingRoundCount,
                                            snapshot.HasEmptyColor,
                                            snapshot.IsEmptyColorFixed,
                                            snapshot.IsSimultaneous);
    }

    private int CalculateStarCount(ESimulationEndState endState, int clearRoundCount)
    {
        if (endState != ESimulationEndState.Succeeded)
        {
            return 0;
        }

        if (clearRoundCount <= _settings.ThreeStarRoundCount)
        {
            return 3;
        }

        if (clearRoundCount <= _settings.TwoStarRoundCount)
        {
            return 2;
        }

        if (clearRoundCount <= _settings.OneStarRoundCount)
        {
            return 1;
        }

        return 0;
    }

    private void PublishLevelChanged()
    {
        OnLevelChanged?.Invoke(_currentLevel);
    }

    private static ELevelPlayProcessState ToProcessState(EProcessState state)
    {
        switch (state)
        {
            case EProcessState.Waiting:
                return ELevelPlayProcessState.Waiting;

            case EProcessState.Failed:
                return ELevelPlayProcessState.Failed;

            case EProcessState.Completed:
                return ELevelPlayProcessState.Completed;

            default:
                return ELevelPlayProcessState.Running;
        }
    }

    private static ELevelPlayConnectionState ToConnectionState(EConnectionState state)
    {
        switch (state)
        {
            case EConnectionState.Occupied:
                return ELevelPlayConnectionState.Occupied;

            case EConnectionState.Waiting:
                return ELevelPlayConnectionState.Waiting;

            case EConnectionState.Completed:
                return ELevelPlayConnectionState.Completed;

            case EConnectionState.Blocked:
                return ELevelPlayConnectionState.Blocked;

            default:
                return ELevelPlayConnectionState.Planned;
        }
    }

    private static ELevelPlaySimulationEndState ToSimulationEndState(ESimulationEndState state)
    {
        switch (state)
        {
            case ESimulationEndState.Deadlocked:
                return ELevelPlaySimulationEndState.Deadlocked;

            case ESimulationEndState.Failed:
                return ELevelPlaySimulationEndState.Failed;

            default:
                return ELevelPlaySimulationEndState.Succeeded;
        }
    }

    private static ELevelPlayRelayType ToRelayType(ERelayType type)
    {
        switch (type)
        {
            case ERelayType.Transfer:
                return ELevelPlayRelayType.Transfer;

            default:
                return ELevelPlayRelayType.Link;
        }
    }

    private static ELevelPlayFocusKind ToFocusKind(EFocusKind focusKind)
    {
        switch (focusKind)
        {
            case EFocusKind.Single:
                return ELevelPlayFocusKind.Single;

            case EFocusKind.Pair:
                return ELevelPlayFocusKind.Pair;

            case EFocusKind.Group:
                return ELevelPlayFocusKind.Group;

            case EFocusKind.Board:
                return ELevelPlayFocusKind.Board;

            default:
                return ELevelPlayFocusKind.None;
        }
    }

    private sealed class PlannedConnection
    {
        private int _connectionId;
        public int ConnectionId
        {
            get => _connectionId;
            set => _connectionId = value;
        }

        private int _selectionOrder;
        public int SelectionOrder
        {
            get => _selectionOrder;
            set => _selectionOrder = value;
        }

        public readonly int ProcessId;
        public readonly int SlotId;
        public readonly int ResourceId;

        public PlannedConnection(int connectionId,
                                 int processId,
                                 int slotId,
                                 int resourceId,
                                 int selectionOrder)
        {
            _connectionId = connectionId;
            ProcessId = processId;
            SlotId = slotId;
            ResourceId = resourceId;
            _selectionOrder = selectionOrder;
        }
    }
}
