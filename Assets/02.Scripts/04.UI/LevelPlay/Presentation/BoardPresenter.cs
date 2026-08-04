using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class BoardPresenter : MonoBehaviour
{
    [SerializeField]
    private LevelPlayBootstrap _bootstrap;

    [SerializeField]
    private Transform _boardRoot;

    [SerializeField]
    private ProcessView _processViewPrefab;

    [SerializeField]
    private ResourceView _resourceViewPrefab;

    [SerializeField]
    private ConnectionView _connectionViewPrefab;

    [SerializeField]
    private RelayView _relayViewPrefab;

    [SerializeField]
    private LevelPlayHudView _hudView;

    [SerializeField]
    private BoardInputController _boardInputController;

    [SerializeField]
    private float _cellSpacing = 1.6f;

    [SerializeField]
    private Vector2 _boardOffset;

    [SerializeField]
    private float _roundDuration = 0.9f;

    private readonly Dictionary<int, ProcessView> _processViewByIdDict = new();
    private readonly Dictionary<int, ResourceView> _resourceViewByIdDict = new();
    private readonly Dictionary<int, ConnectionView> _connectionViewByIdDict = new();
    private readonly Dictionary<int, RelayView> _relayViewByIdDict = new();
    private readonly HashSet<int> _occupiedConnectionIdSet = new();
    private readonly HashSet<int> _waitingConnectionIdSet = new();
    private readonly HashSet<int> _completedConnectionIdSet = new();
    private readonly HashSet<int> _blockedConnectionIdSet = new();
    private readonly HashSet<int> _completedProcessIdSet = new();
    private readonly HashSet<int> _failedProcessIdSet = new();
    private readonly HashSet<int> _highlightedResourceIdSet = new();
    private readonly HashSet<int> _activeBoardRuleIdSet = new();

    private LevelPlayManager _manager;
    private LevelPlayDTO _planningLevel;
    private LevelSimulationDTO _resultSimulation;
    private LevelPlayRoundResourceDTO[] _playbackResourceArray;
    private LevelPlayRelayDTO[] _playbackRelayArray;
    private CancellationTokenSource _playbackCancellationTokenSource;
    private float _rowCenter;
    private float _columnCenter;
    private float _playbackSpeed = 1f;
    private bool _isPlaybackPaused;
    private bool _isPlaybackRunning;
    private bool _isResultVisible;
    private int _currentRoundIndex;
    private int _focusedResourceId = -1;
    private int _selectedProcessId = -1;
    private int _selectedSlotId = -1;

    private void Start()
    {
        if (!TryInitialize())
        {
            return;
        }

        _manager.OnLevelChanged += RefreshBoard;
        _manager.OnSimulationReady += HandleSimulationReady;
        _manager.OnResourceFocusChanged += HandleResourceFocusChanged;
        SubscribeHudView();
        RefreshBoard(_manager.CurrentLevel);
    }

    private void Update()
    {
        if (_manager == null || _manager.CurrentLevel == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartLevel();
            return;
        }

        if (_manager.CurrentLevel.Phase == ELevelPlayPhase.Planning)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                StartSimulation();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace))
            {
                RemoveSelectedConnection();
            }

            return;
        }

        if (!_isPlaybackRunning)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            TogglePlaybackPause();
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SetPlaybackSpeed(0.5f);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SetPlaybackSpeed(1f);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SetPlaybackSpeed(2f);
        }
    }

    private void OnDestroy()
    {
        if (_manager != null)
        {
            _manager.OnLevelChanged -= RefreshBoard;
            _manager.OnSimulationReady -= HandleSimulationReady;
            _manager.OnResourceFocusChanged -= HandleResourceFocusChanged;
        }

        UnsubscribeHudView();

        CancelPlayback();

        foreach (KeyValuePair<int, ProcessView> pair in _processViewByIdDict)
        {
            pair.Value.OnPressed -= HandleProcessPressed;
        }

        foreach (KeyValuePair<int, ResourceView> pair in _resourceViewByIdDict)
        {
            pair.Value.OnPressed -= HandleResourcePressed;
        }
    }

    private bool TryInitialize()
    {
        if (_bootstrap == null || !_bootstrap.IsInitialized || _bootstrap.Manager == null)
        {
            Debug.LogError("[BoardPresenter] A successfully initialized LevelPlayBootstrap reference is required.", this);
            return false;
        }

        if (_boardRoot == null ||
            _processViewPrefab == null ||
            _resourceViewPrefab == null ||
            _connectionViewPrefab == null ||
            _relayViewPrefab == null ||
            _boardInputController == null)
        {
            Debug.LogError("[BoardPresenter] Board root and all LevelPlay View prefabs must be assigned.", this);
            return false;
        }

        _manager = _bootstrap.Manager;
        return true;
    }

    private void RefreshBoard(LevelPlayDTO level)
    {
        if (level == null)
        {
            return;
        }

        CalculateBoardCenter(level);
        Dictionary<int, Dictionary<int, int>> slotColorIdByProcessIdDict = CreateSlotColorIdMap(level);
        Dictionary<int, LevelPlayConnectionDTO> connectionByIdDict = CreateConnectionMap(level);
        HashSet<int> activeProcessIdSet = new();
        HashSet<int> activeResourceIdSet = new();
        HashSet<int> activeConnectionIdSet = new();
        HashSet<int> activeRelayIdSet = new();

        RefreshProcessViews(level, activeProcessIdSet);
        RefreshResourceViews(level, slotColorIdByProcessIdDict, connectionByIdDict, activeResourceIdSet);
        RefreshConnectionViews(level, slotColorIdByProcessIdDict, activeConnectionIdSet);
        RefreshRelayViews(level.RelayArray, activeRelayIdSet);
        SetInactiveViews(_processViewByIdDict, activeProcessIdSet);
        SetInactiveViews(_resourceViewByIdDict, activeResourceIdSet);
        SetInactiveViews(_connectionViewByIdDict, activeConnectionIdSet);
        SetInactiveViews(_relayViewByIdDict, activeRelayIdSet);
        RefreshHud();
    }

    private void RefreshProcessViews(LevelPlayDTO level, HashSet<int> activeProcessIdSet)
    {
        for (int i = 0; i < level.ProcessArray.Length; i++)
        {
            LevelPlayProcessDTO process = level.ProcessArray[i];
            ProcessView processView = GetOrCreateProcessView(process.Id);
            processView.gameObject.SetActive(true);
            processView.Refresh(process.Id,
                                GetBoardPosition(process.Row, process.Column),
                                CreateSlotIdList(process.SlotArray),
                                CreateRequiredColorIdList(process.SlotArray),
                                ToProcessVisualState(process));
            activeProcessIdSet.Add(process.Id);
        }
    }

    private void RefreshResourceViews(LevelPlayDTO level,
                                      IReadOnlyDictionary<int, Dictionary<int, int>> slotColorIdByProcessIdDict,
                                      IReadOnlyDictionary<int, LevelPlayConnectionDTO> connectionByIdDict,
                                      HashSet<int> activeResourceIdSet)
    {
        for (int i = 0; i < level.ResourceArray.Length; i++)
        {
            LevelPlayResourceDTO resource = level.ResourceArray[i];
            ResourceView resourceView = GetOrCreateResourceView(resource.Id);
            resourceView.gameObject.SetActive(true);
            resourceView.Refresh(resource.Id,
                                 GetBoardPosition(resource.Row, resource.Column),
                                 resource.ColorId,
                                 resource.Capacity,
                                 CreateOccupiedColorIdList(resource, slotColorIdByProcessIdDict, connectionByIdDict),
                                 resource.IsLocked,
                                 IsResourceHighlighted(resource.Id),
                                 ToResourceRuleVisualData(resource.Rule));
            activeResourceIdSet.Add(resource.Id);
        }
    }

    private void RefreshConnectionViews(LevelPlayDTO level,
                                        IReadOnlyDictionary<int, Dictionary<int, int>> slotColorIdByProcessIdDict,
                                        HashSet<int> activeConnectionIdSet)
    {
        for (int i = 0; i < level.ConnectionArray.Length; i++)
        {
            LevelPlayConnectionDTO connection = level.ConnectionArray[i];

            if (!_processViewByIdDict.TryGetValue(connection.ProcessId, out ProcessView processView) ||
                !_resourceViewByIdDict.TryGetValue(connection.ResourceId, out ResourceView resourceView))
            {
                continue;
            }

            ConnectionView connectionView = GetOrCreateConnectionView(connection.Id);
            connectionView.gameObject.SetActive(true);
            Vector2 processWorldPosition = ToVector2(processView.transform.position);
            Vector2 resourceWorldPosition = ToVector2(resourceView.transform.position);

            connectionView.Refresh(ToBoardLocalPosition(processWorldPosition),
                                   ToBoardLocalPosition(resourceWorldPosition),
                                   GetRequiredColorId(connection, slotColorIdByProcessIdDict),
                                   ToConnectionVisualState(connection.State));
            activeConnectionIdSet.Add(connection.Id);
        }
    }

    private ProcessView GetOrCreateProcessView(int id)
    {
        if (_processViewByIdDict.TryGetValue(id, out ProcessView view))
        {
            return view;
        }

        view = Instantiate(_processViewPrefab, _boardRoot);
        view.name = "Process_" + id;
        view.OnPressed += HandleProcessPressed;
        _processViewByIdDict.Add(id, view);
        return view;
    }

    private ResourceView GetOrCreateResourceView(int id)
    {
        if (_resourceViewByIdDict.TryGetValue(id, out ResourceView view))
        {
            return view;
        }

        view = Instantiate(_resourceViewPrefab, _boardRoot);
        view.name = "Resource_" + id;
        view.OnPressed += HandleResourcePressed;
        _resourceViewByIdDict.Add(id, view);
        return view;
    }

    private ConnectionView GetOrCreateConnectionView(int id)
    {
        if (_connectionViewByIdDict.TryGetValue(id, out ConnectionView view))
        {
            return view;
        }

        view = Instantiate(_connectionViewPrefab, _boardRoot);
        view.name = "Connection_" + id;
        _connectionViewByIdDict.Add(id, view);
        return view;
    }

    private RelayView GetOrCreateRelayView(int id)
    {
        if (_relayViewByIdDict.TryGetValue(id, out RelayView view))
        {
            return view;
        }

        view = Instantiate(_relayViewPrefab, _boardRoot);
        view.name = "Relay_" + id;
        _relayViewByIdDict.Add(id, view);
        return view;
    }

    private void RefreshRelayViews(IReadOnlyList<LevelPlayRelayDTO> relayList,
                                   HashSet<int> activeRelayIdSet)
    {
        for (int i = 0; i < relayList.Count; i++)
        {
            LevelPlayRelayDTO relay = relayList[i];

            if (!_resourceViewByIdDict.TryGetValue(relay.FirstResourceId, out ResourceView firstResourceView) ||
                !_resourceViewByIdDict.TryGetValue(relay.SecondResourceId, out ResourceView secondResourceView))
            {
                continue;
            }

            ResourceView startResourceView = firstResourceView;
            ResourceView endResourceView = secondResourceView;

            if (relay.RelayType == ELevelPlayRelayType.Transfer)
            {
                if (relay.SenderResourceId == relay.SecondResourceId)
                {
                    startResourceView = secondResourceView;
                    endResourceView = firstResourceView;
                }
                else if (relay.SenderResourceId != relay.FirstResourceId)
                {
                    Debug.LogError("[BoardPresenter] Transfer Relay sender must be one of its endpoint Resources: " + relay.Id, this);
                    continue;
                }
            }

            Vector2 startWorldPosition = ToVector2(startResourceView.transform.position);
            Vector2 endWorldPosition = ToVector2(endResourceView.transform.position);
            RelayView relayView = GetOrCreateRelayView(relay.Id);
            relayView.gameObject.SetActive(true);
            relayView.Refresh(ToBoardLocalPosition(startWorldPosition),
                              ToBoardLocalPosition(endWorldPosition),
                              relay.RelayType == ELevelPlayRelayType.Transfer
                                  ? ERelayVisualType.Transfer
                                  : ERelayVisualType.Link,
                              relay.IsActive || _activeBoardRuleIdSet.Contains(relay.Id));
            activeRelayIdSet.Add(relay.Id);
        }
    }

    private void HandleProcessPressed(int processId)
    {
        if (!IsPlanning() || !TryGetProcess(processId, out _))
        {
            return;
        }

        ClearResourceFocus();
        _selectedProcessId = processId;
        _selectedSlotId = -1;
        RefreshBoard(_manager.CurrentLevel);
    }

    private void HandleResourcePressed(int resourceId)
    {
        if (_manager == null || _manager.CurrentLevel == null)
        {
            return;
        }

        _manager.FocusResource(resourceId);
    }

    private void HandleRequiredColorPressed(int colorIndex)
    {
        if (!IsPlanning() || !TryGetSlotByColorIndex(_selectedProcessId, colorIndex, out LevelPlaySlotDTO slot))
        {
            return;
        }

        _selectedSlotId = slot.Id;
        RefreshBoard(_manager.CurrentLevel);
    }

    private void HandleRequiredColorDropped(int colorIndex, Vector2 screenPosition)
    {
        HandleRequiredColorPressed(colorIndex);

        if (!IsPlanning() ||
            !_boardInputController.TryGetResourceAtScreenPosition(screenPosition, out ResourceView resourceView))
        {
            return;
        }

        AssignSelectedSlotToResource(resourceView.ResourceId);
    }

    private void AssignSelectedSlotToResource(int resourceId)
    {
        if (!TryGetSlot(_selectedProcessId, _selectedSlotId, out LevelPlaySlotDTO slot))
        {
            return;
        }

        LevelPlayCommandResult result = slot.ConnectionId >= 0
            ? _manager.ReplaceConnection(_selectedProcessId, _selectedSlotId, resourceId)
            : _manager.AssignConnection(_selectedProcessId, _selectedSlotId, resourceId);

        if (!result.Success)
        {
            Debug.LogWarning("[BoardPresenter] Connection command was rejected: " + result.Error, this);
            PlaySelectedProcessRejectedFeedback();
            return;
        }

        _selectedSlotId = -1;
        RefreshBoard(_manager.CurrentLevel);
    }

    private void RemoveSelectedConnection()
    {
        if (!TryGetSlot(_selectedProcessId, _selectedSlotId, out LevelPlaySlotDTO slot) || slot.ConnectionId < 0)
        {
            return;
        }

        LevelPlayCommandResult result = _manager.RemoveConnection(slot.ConnectionId);

        if (!result.Success)
        {
            Debug.LogWarning("[BoardPresenter] Connection removal was rejected: " + result.Error, this);
            PlaySelectedProcessRejectedFeedback();
            return;
        }

        ClearSelection();
        RefreshBoard(_manager.CurrentLevel);
    }

    private bool IsPlanning()
    {
        return _manager != null &&
               _manager.CurrentLevel != null &&
               _manager.CurrentLevel.Phase == ELevelPlayPhase.Planning;
    }

    private bool TryGetSlot(int processId, int slotId, out LevelPlaySlotDTO selectedSlot)
    {
        selectedSlot = null;

        if (_manager == null || _manager.CurrentLevel == null)
        {
            return false;
        }

        LevelPlayProcessDTO[] processArray = _manager.CurrentLevel.ProcessArray;

        for (int processIndex = 0; processIndex < processArray.Length; processIndex++)
        {
            LevelPlayProcessDTO process = processArray[processIndex];

            if (process.Id != processId)
            {
                continue;
            }

            for (int slotIndex = 0; slotIndex < process.SlotArray.Length; slotIndex++)
            {
                LevelPlaySlotDTO slot = process.SlotArray[slotIndex];

                if (slot.Id == slotId)
                {
                    selectedSlot = slot;
                    return true;
                }
            }
        }

        return false;
    }

    private bool TryGetProcess(int processId, out LevelPlayProcessDTO selectedProcess)
    {
        selectedProcess = null;

        if (_manager == null || _manager.CurrentLevel == null)
        {
            return false;
        }

        LevelPlayProcessDTO[] processArray = _manager.CurrentLevel.ProcessArray;

        for (int processIndex = 0; processIndex < processArray.Length; processIndex++)
        {
            LevelPlayProcessDTO process = processArray[processIndex];

            if (process.Id != processId)
            {
                continue;
            }

            selectedProcess = process;
            return true;
        }

        return false;
    }

    private bool TryGetSlotByColorIndex(int processId, int colorIndex, out LevelPlaySlotDTO selectedSlot)
    {
        selectedSlot = null;

        if (!TryGetProcess(processId, out LevelPlayProcessDTO process) ||
            colorIndex < 0 ||
            colorIndex >= process.SlotArray.Length)
        {
            return false;
        }

        selectedSlot = process.SlotArray[colorIndex];
        return true;
    }

    private void ClearSelection()
    {
        _selectedProcessId = -1;
        _selectedSlotId = -1;
    }

    private void PlaySelectedProcessRejectedFeedback()
    {
        if (_processViewByIdDict.TryGetValue(_selectedProcessId, out ProcessView processView))
        {
            processView.PlayRejectedFeedback();
        }
    }

    private void HandleResourceFocusChanged(ResourceFocusDTO focus)
    {
        if (focus == null || !focus.Success)
        {
            return;
        }

        _focusedResourceId = focus.FocusedResourceId;
        ReplaceIdSet(_highlightedResourceIdSet, focus.HighlightedResourceIdArray);
        ReplaceIdSet(_activeBoardRuleIdSet, focus.ActiveBoardRuleIdArray);

        if (_planningLevel != null && !IsPlanning())
        {
            RefreshPlaybackBoard();
            return;
        }

        RefreshBoard(_manager.CurrentLevel);
    }

    private void ClearResourceFocus()
    {
        _focusedResourceId = -1;
        _highlightedResourceIdSet.Clear();
        _activeBoardRuleIdSet.Clear();
    }

    private void StartSimulation()
    {
        if (!_manager.CurrentLevel.CanStartSimulation)
        {
            Debug.LogWarning("[BoardPresenter] Complete every Process slot reservation before starting the simulation.", this);
            PlaySelectedProcessRejectedFeedback();
            return;
        }

        _planningLevel = _manager.CurrentLevel;
        ClearResultState();
        ClearResourceFocus();
        ClearSelection();
        LevelPlayCommandResult result = _manager.StartSimulation();

        if (!result.Success)
        {
            _planningLevel = null;
            Debug.LogWarning("[BoardPresenter] Simulation could not start: " + result.Error, this);
        }
    }

    private void RestartLevel()
    {
        CancelPlayback();
        ClearResourceFocus();
        ClearSelection();
        _planningLevel = null;
        ClearResultState();
        ClearPlaybackState();
        LevelPlayCommandResult result = _manager.RestartLevel();

        if (!result.Success)
        {
            Debug.LogWarning("[BoardPresenter] Level restart was rejected: " + result.Error, this);
        }
    }

    private void HandleSimulationReady(LevelSimulationDTO simulation)
    {
        if (_planningLevel == null || simulation == null)
        {
            return;
        }

        CancelPlayback();
        ClearResultState();
        ClearPlaybackState();
        _isPlaybackRunning = true;
        _playbackCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        PlaySimulationAsync(simulation, _playbackCancellationTokenSource.Token).Forget();
    }

    private async UniTask PlaySimulationAsync(LevelSimulationDTO simulation, CancellationToken cancellationToken)
    {
        bool isPlaybackComplete = false;

        try
        {
            RefreshPlaybackBoard();

            for (int i = 0; i < simulation.RoundArray.Length; i++)
            {
                await WaitForRoundDurationAsync(cancellationToken);
                ApplyRound(simulation.RoundArray[i]);
                RefreshPlaybackBoard();
            }

            AddIdArray(_completedProcessIdSet, simulation.CompletedProcessIdArray);
            AddIdArray(_blockedConnectionIdSet, simulation.BlockedConnectionIdArray);
            RefreshPlaybackBoard();
            isPlaybackComplete = true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            if (_playbackCancellationTokenSource != null &&
                _playbackCancellationTokenSource.Token == cancellationToken)
            {
                _playbackCancellationTokenSource.Dispose();
                _playbackCancellationTokenSource = null;
                _isPlaybackRunning = false;
                _isPlaybackPaused = false;

                if (isPlaybackComplete)
                {
                    _resultSimulation = simulation;
                    _isResultVisible = true;
                }

                RefreshHud();
            }
        }
    }

    private async UniTask WaitForRoundDurationAsync(CancellationToken cancellationToken)
    {
        float elapsedTime = 0f;

        while (elapsedTime < _roundDuration)
        {
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

            if (!_isPlaybackPaused)
            {
                elapsedTime += Time.unscaledDeltaTime * _playbackSpeed;
            }
        }
    }

    private void ApplyRound(LevelRoundDTO round)
    {
        _currentRoundIndex = round.RoundIndex;
        _playbackResourceArray = round.ResourceArray;
        _playbackRelayArray = round.RelayArray;
        RemoveIdArray(_waitingConnectionIdSet, round.RequeuedConnectionIdArray);
        AddIdArray(_occupiedConnectionIdSet, round.OccupiedConnectionIdArray);
        RemoveIdArray(_waitingConnectionIdSet, round.OccupiedConnectionIdArray);
        AddIdArray(_waitingConnectionIdSet, round.WaitingConnectionIdArray);
        RemoveIdArray(_occupiedConnectionIdSet, round.ReleasedConnectionIdArray);
        AddIdArray(_completedConnectionIdSet, round.ReleasedConnectionIdArray);
        RemoveIdArray(_occupiedConnectionIdSet, round.BlockedConnectionIdArray);
        RemoveIdArray(_waitingConnectionIdSet, round.BlockedConnectionIdArray);
        AddIdArray(_blockedConnectionIdSet, round.BlockedConnectionIdArray);
        AddIdArray(_completedProcessIdSet, round.CompletedProcessIdArray);
        AddIdArray(_failedProcessIdSet, round.FailedProcessIdArray);
    }

    private void RefreshPlaybackBoard()
    {
        if (_planningLevel == null)
        {
            return;
        }

        CalculateBoardCenter(_planningLevel);
        Dictionary<int, Dictionary<int, int>> slotColorIdByProcessIdDict = CreateSlotColorIdMap(_planningLevel);
        Dictionary<int, LevelPlayConnectionDTO> connectionByIdDict = CreateConnectionMap(_planningLevel);
        HashSet<int> activeProcessIdSet = new();
        HashSet<int> activeResourceIdSet = new();
        HashSet<int> activeConnectionIdSet = new();
        HashSet<int> activeRelayIdSet = new();
        Dictionary<int, LevelPlayRoundResourceDTO> playbackResourceByIdDict = CreateRoundResourceMap(_playbackResourceArray);

        for (int i = 0; i < _planningLevel.ProcessArray.Length; i++)
        {
            LevelPlayProcessDTO process = _planningLevel.ProcessArray[i];
            ProcessView processView = GetOrCreateProcessView(process.Id);
            processView.gameObject.SetActive(true);
            processView.Refresh(process.Id,
                                GetBoardPosition(process.Row, process.Column),
                                CreateSlotIdList(process.SlotArray),
                                CreateRequiredColorIdList(process.SlotArray),
                                GetPlaybackProcessVisualState(process.Id, connectionByIdDict));
            activeProcessIdSet.Add(process.Id);
        }

        for (int i = 0; i < _planningLevel.ResourceArray.Length; i++)
        {
            LevelPlayResourceDTO resource = _planningLevel.ResourceArray[i];
            playbackResourceByIdDict.TryGetValue(resource.Id, out LevelPlayRoundResourceDTO playbackResource);
            ResourceView resourceView = GetOrCreateResourceView(resource.Id);
            resourceView.gameObject.SetActive(true);
            resourceView.Refresh(resource.Id,
                                 GetBoardPosition(resource.Row, resource.Column),
                                 playbackResource?.ColorId ?? resource.ColorId,
                                 resource.Capacity,
                                 playbackResource != null
                                     ? CreateOccupiedColorIdList(playbackResource.OccupiedConnectionIdArray,
                                                                 slotColorIdByProcessIdDict,
                                                                 connectionByIdDict)
                                     : CreatePlaybackOccupiedColorIdList(resource.Id,
                                                                        slotColorIdByProcessIdDict,
                                                                        connectionByIdDict),
                                 playbackResource?.IsLocked ?? resource.IsLocked,
                                 IsResourceHighlighted(resource.Id),
                                 ToResourceRuleVisualData(playbackResource?.Rule ?? resource.Rule));
            activeResourceIdSet.Add(resource.Id);
        }

        for (int i = 0; i < _planningLevel.ConnectionArray.Length; i++)
        {
            LevelPlayConnectionDTO connection = _planningLevel.ConnectionArray[i];

            if (!_processViewByIdDict.TryGetValue(connection.ProcessId, out ProcessView processView) ||
                !_resourceViewByIdDict.TryGetValue(connection.ResourceId, out ResourceView resourceView))
            {
                continue;
            }

            ConnectionView connectionView = GetOrCreateConnectionView(connection.Id);
            connectionView.gameObject.SetActive(true);
            Vector2 processWorldPosition = ToVector2(processView.transform.position);
            Vector2 resourceWorldPosition = ToVector2(resourceView.transform.position);

            connectionView.Refresh(ToBoardLocalPosition(processWorldPosition),
                                   ToBoardLocalPosition(resourceWorldPosition),
                                   GetRequiredColorId(connection, slotColorIdByProcessIdDict),
                                   GetPlaybackConnectionVisualState(connection.Id));
            activeConnectionIdSet.Add(connection.Id);
        }

        RefreshRelayViews(_playbackRelayArray ?? _planningLevel.RelayArray, activeRelayIdSet);

        SetInactiveViews(_processViewByIdDict, activeProcessIdSet);
        SetInactiveViews(_resourceViewByIdDict, activeResourceIdSet);
        SetInactiveViews(_connectionViewByIdDict, activeConnectionIdSet);
        SetInactiveViews(_relayViewByIdDict, activeRelayIdSet);
        RefreshHud();
    }

    private EProcessVisualState GetPlaybackProcessVisualState(int processId,
                                                               IReadOnlyDictionary<int, LevelPlayConnectionDTO> connectionByIdDict)
    {
        if (_failedProcessIdSet.Contains(processId))
        {
            return EProcessVisualState.Failed;
        }

        if (_completedProcessIdSet.Contains(processId))
        {
            return EProcessVisualState.Completed;
        }

        foreach (int connectionId in _waitingConnectionIdSet)
        {
            if (connectionByIdDict.TryGetValue(connectionId, out LevelPlayConnectionDTO connection) &&
                connection.ProcessId == processId)
            {
                return EProcessVisualState.Waiting;
            }
        }

        return EProcessVisualState.Default;
    }

    private List<int> CreatePlaybackOccupiedColorIdList(int resourceId,
                                                         IReadOnlyDictionary<int, Dictionary<int, int>> slotColorIdByProcessIdDict,
                                                         IReadOnlyDictionary<int, LevelPlayConnectionDTO> connectionByIdDict)
    {
        List<int> occupiedColorIdList = new();

        foreach (int connectionId in _occupiedConnectionIdSet)
        {
            if (!connectionByIdDict.TryGetValue(connectionId, out LevelPlayConnectionDTO connection) ||
                connection.ResourceId != resourceId ||
                !slotColorIdByProcessIdDict.TryGetValue(connection.ProcessId, out Dictionary<int, int> colorIdBySlotIdDict) ||
                !colorIdBySlotIdDict.TryGetValue(connection.SlotId, out int colorId))
            {
                continue;
            }

            occupiedColorIdList.Add(colorId);
        }

        return occupiedColorIdList;
    }

    private EConnectionVisualState GetPlaybackConnectionVisualState(int connectionId)
    {
        if (_blockedConnectionIdSet.Contains(connectionId))
        {
            return EConnectionVisualState.Blocked;
        }

        if (_completedConnectionIdSet.Contains(connectionId))
        {
            return EConnectionVisualState.Completed;
        }

        if (_occupiedConnectionIdSet.Contains(connectionId))
        {
            return EConnectionVisualState.Occupied;
        }

        if (_waitingConnectionIdSet.Contains(connectionId))
        {
            return EConnectionVisualState.Waiting;
        }

        return EConnectionVisualState.Planned;
    }

    private void ClearPlaybackState()
    {
        _playbackResourceArray = null;
        _playbackRelayArray = null;
        _occupiedConnectionIdSet.Clear();
        _waitingConnectionIdSet.Clear();
        _completedConnectionIdSet.Clear();
        _blockedConnectionIdSet.Clear();
        _completedProcessIdSet.Clear();
        _failedProcessIdSet.Clear();
        _isPlaybackPaused = false;
        _currentRoundIndex = 0;
        _playbackSpeed = 1f;
    }

    private void ClearResultState()
    {
        _resultSimulation = null;
        _isResultVisible = false;
    }

    private void SubscribeHudView()
    {
        if (_hudView == null)
        {
            return;
        }

        _hudView.OnRunPressed += StartSimulation;
        _hudView.OnRestartPressed += RestartLevel;
        _hudView.OnPausePressed += TogglePlaybackPause;
        _hudView.OnPlaybackSpeedPressed += SetPlaybackSpeed;
        _hudView.OnRequiredColorPressed += HandleRequiredColorPressed;
        _hudView.OnRequiredColorDropped += HandleRequiredColorDropped;
    }

    private void UnsubscribeHudView()
    {
        if (_hudView == null)
        {
            return;
        }

        _hudView.OnRunPressed -= StartSimulation;
        _hudView.OnRestartPressed -= RestartLevel;
        _hudView.OnPausePressed -= TogglePlaybackPause;
        _hudView.OnPlaybackSpeedPressed -= SetPlaybackSpeed;
        _hudView.OnRequiredColorPressed -= HandleRequiredColorPressed;
        _hudView.OnRequiredColorDropped -= HandleRequiredColorDropped;
    }

    private void TogglePlaybackPause()
    {
        if (!_isPlaybackRunning)
        {
            return;
        }

        _isPlaybackPaused = !_isPlaybackPaused;
        RefreshHud();
    }

    private void SetPlaybackSpeed(float speed)
    {
        if (!_isPlaybackRunning || speed <= 0f)
        {
            return;
        }

        _playbackSpeed = speed;
        RefreshHud();
    }

    private void RefreshHud()
    {
        if (_hudView == null || _manager == null || _manager.CurrentLevel == null)
        {
            return;
        }

        _hudView.Refresh(CreateHudVisualData(_manager.CurrentLevel));
    }

    private LevelPlayHudVisualData CreateHudVisualData(LevelPlayDTO level)
    {
        List<int> selectedRequiredColorIdList = new();
        int selectedRequiredColorIndex = -1;
        int reservedSlotCount = 0;
        int totalSlotCount = 0;

        for (int processIndex = 0; processIndex < level.ProcessArray.Length; processIndex++)
        {
            LevelPlayProcessDTO process = level.ProcessArray[processIndex];

            for (int slotIndex = 0; slotIndex < process.SlotArray.Length; slotIndex++)
            {
                LevelPlaySlotDTO slot = process.SlotArray[slotIndex];
                totalSlotCount++;

                if (slot.ConnectionId >= 0)
                {
                    reservedSlotCount++;
                }

                if (process.Id != _selectedProcessId)
                {
                    continue;
                }

                selectedRequiredColorIdList.Add(slot.RequiredColorId);

                if (slot.Id == _selectedSlotId)
                {
                    selectedRequiredColorIndex = slotIndex;
                }
            }
        }

        return new LevelPlayHudVisualData(level.LevelId,
                                           level.Phase == ELevelPlayPhase.Planning,
                                           level.CanStartSimulation,
                                           reservedSlotCount,
                                           totalSlotCount,
                                           _isPlaybackRunning,
                                           _isPlaybackPaused,
                                           _currentRoundIndex,
                                           _playbackSpeed,
                                           selectedRequiredColorIdList,
                                           selectedRequiredColorIndex,
                                           GetResultVisualState(),
                                           _resultSimulation != null ? _resultSimulation.StarCount : 0,
                                           _resultSimulation != null ? _resultSimulation.ClearRoundCount : 0);
    }

    private ELevelPlayResultVisualState GetResultVisualState()
    {
        if (!_isResultVisible || _resultSimulation == null)
        {
            return ELevelPlayResultVisualState.None;
        }

        switch (_resultSimulation.EndState)
        {
            case ELevelPlaySimulationEndState.Succeeded:
                return ELevelPlayResultVisualState.Clear;

            case ELevelPlaySimulationEndState.Deadlocked:
                return ELevelPlayResultVisualState.Deadlocked;

            default:
                return ELevelPlayResultVisualState.Failed;
        }
    }

    private bool IsResourceHighlighted(int resourceId)
    {
        return resourceId == _focusedResourceId || _highlightedResourceIdSet.Contains(resourceId);
    }

    private static void ReplaceIdSet(HashSet<int> idSet, IReadOnlyList<int> idList)
    {
        idSet.Clear();

        for (int i = 0; i < idList.Count; i++)
        {
            idSet.Add(idList[i]);
        }
    }

    private void CancelPlayback()
    {
        if (_playbackCancellationTokenSource != null)
        {
            _playbackCancellationTokenSource.Cancel();
            _playbackCancellationTokenSource.Dispose();
            _playbackCancellationTokenSource = null;
        }

        _isPlaybackRunning = false;
        _isPlaybackPaused = false;
    }

    private static void AddIdArray(HashSet<int> idSet, IReadOnlyList<int> idList)
    {
        for (int i = 0; i < idList.Count; i++)
        {
            idSet.Add(idList[i]);
        }
    }

    private static void RemoveIdArray(HashSet<int> idSet, IReadOnlyList<int> idList)
    {
        for (int i = 0; i < idList.Count; i++)
        {
            idSet.Remove(idList[i]);
        }
    }

    private void CalculateBoardCenter(LevelPlayDTO level)
    {
        int minimumRow = int.MaxValue;
        int maximumRow = int.MinValue;
        int minimumColumn = int.MaxValue;
        int maximumColumn = int.MinValue;

        for (int i = 0; i < level.ProcessArray.Length; i++)
        {
            IncludePosition(level.ProcessArray[i].Row,
                            level.ProcessArray[i].Column,
                            ref minimumRow,
                            ref maximumRow,
                            ref minimumColumn,
                            ref maximumColumn);
        }

        for (int i = 0; i < level.ResourceArray.Length; i++)
        {
            IncludePosition(level.ResourceArray[i].Row,
                            level.ResourceArray[i].Column,
                            ref minimumRow,
                            ref maximumRow,
                            ref minimumColumn,
                            ref maximumColumn);
        }

        _rowCenter = (minimumRow + maximumRow) * 0.5f;
        _columnCenter = (minimumColumn + maximumColumn) * 0.5f;
    }

    private Vector2 GetBoardPosition(int row, int column)
    {
        return new Vector2((column - _columnCenter) * _cellSpacing, (_rowCenter - row) * _cellSpacing) + _boardOffset;
    }

    private static void IncludePosition(int row,
                                        int column,
                                        ref int minimumRow,
                                        ref int maximumRow,
                                        ref int minimumColumn,
                                        ref int maximumColumn)
    {
        minimumRow = Mathf.Min(minimumRow, row);
        maximumRow = Mathf.Max(maximumRow, row);
        minimumColumn = Mathf.Min(minimumColumn, column);
        maximumColumn = Mathf.Max(maximumColumn, column);
    }

    private static Dictionary<int, Dictionary<int, int>> CreateSlotColorIdMap(LevelPlayDTO level)
    {
        Dictionary<int, Dictionary<int, int>> colorIdBySlotIdByProcessIdDict = new();

        for (int i = 0; i < level.ProcessArray.Length; i++)
        {
            LevelPlayProcessDTO process = level.ProcessArray[i];
            Dictionary<int, int> colorIdBySlotIdDict = new();

            for (int slotIndex = 0; slotIndex < process.SlotArray.Length; slotIndex++)
            {
                LevelPlaySlotDTO slot = process.SlotArray[slotIndex];
                colorIdBySlotIdDict[slot.Id] = slot.RequiredColorId;
            }

            colorIdBySlotIdByProcessIdDict[process.Id] = colorIdBySlotIdDict;
        }

        return colorIdBySlotIdByProcessIdDict;
    }

    private static Dictionary<int, LevelPlayConnectionDTO> CreateConnectionMap(LevelPlayDTO level)
    {
        Dictionary<int, LevelPlayConnectionDTO> connectionByIdDict = new();

        for (int i = 0; i < level.ConnectionArray.Length; i++)
        {
            LevelPlayConnectionDTO connection = level.ConnectionArray[i];
            connectionByIdDict[connection.Id] = connection;
        }

        return connectionByIdDict;
    }

    private static List<int> CreateRequiredColorIdList(IReadOnlyList<LevelPlaySlotDTO> slotList)
    {
        List<int> requiredColorIdList = new(slotList.Count);

        for (int i = 0; i < slotList.Count; i++)
        {
            requiredColorIdList.Add(slotList[i].RequiredColorId);
        }

        return requiredColorIdList;
    }

    private static List<int> CreateSlotIdList(IReadOnlyList<LevelPlaySlotDTO> slotList)
    {
        List<int> slotIdList = new(slotList.Count);

        for (int i = 0; i < slotList.Count; i++)
        {
            slotIdList.Add(slotList[i].Id);
        }

        return slotIdList;
    }

    private static List<int> CreateOccupiedColorIdList(LevelPlayResourceDTO resource,
                                                        IReadOnlyDictionary<int, Dictionary<int, int>> slotColorIdByProcessIdDict,
                                                        IReadOnlyDictionary<int, LevelPlayConnectionDTO> connectionByIdDict)
    {
        return CreateOccupiedColorIdList(resource.OccupiedConnectionIdArray,
                                         slotColorIdByProcessIdDict,
                                         connectionByIdDict);
    }

    private static List<int> CreateOccupiedColorIdList(IReadOnlyList<int> occupiedConnectionIdList,
                                                        IReadOnlyDictionary<int, Dictionary<int, int>> slotColorIdByProcessIdDict,
                                                        IReadOnlyDictionary<int, LevelPlayConnectionDTO> connectionByIdDict)
    {
        List<int> occupiedColorIdList = new(occupiedConnectionIdList.Count);

        for (int i = 0; i < occupiedConnectionIdList.Count; i++)
        {
            int connectionId = occupiedConnectionIdList[i];

            if (!connectionByIdDict.TryGetValue(connectionId, out LevelPlayConnectionDTO connection) ||
                !slotColorIdByProcessIdDict.TryGetValue(connection.ProcessId, out Dictionary<int, int> colorIdBySlotIdDict) ||
                !colorIdBySlotIdDict.TryGetValue(connection.SlotId, out int colorId))
            {
                continue;
            }

            occupiedColorIdList.Add(colorId);
        }

        return occupiedColorIdList;
    }

    private static int GetRequiredColorId(LevelPlayConnectionDTO connection,
                                          IReadOnlyDictionary<int, Dictionary<int, int>> slotColorIdByProcessIdDict)
    {
        if (slotColorIdByProcessIdDict.TryGetValue(connection.ProcessId, out Dictionary<int, int> colorIdBySlotIdDict) &&
            colorIdBySlotIdDict.TryGetValue(connection.SlotId, out int colorId))
        {
            return colorId;
        }

        return 0;
    }

    private static ResourceRuleVisualData ToResourceRuleVisualData(LevelPlayResourceRuleDTO rule)
    {
        if (rule == null)
        {
            throw new ArgumentNullException(nameof(rule));
        }

        return new ResourceRuleVisualData(rule.ColorSwitchColorIdArray,
                                          rule.ColorSwitchCurrentIndex,
                                          rule.HasClock,
                                          rule.ClockRemainingRoundCount,
                                          rule.IsClockOpen,
                                          rule.HasEmptyColor,
                                          rule.IsEmptyColorFixed,
                                          rule.IsSimultaneous);
    }

    private EProcessVisualState ToProcessVisualState(LevelPlayProcessDTO process)
    {
        if (process.Id == _selectedProcessId)
        {
            return EProcessVisualState.Selected;
        }

        switch (process.State)
        {
            case ELevelPlayProcessState.Waiting:
                return EProcessVisualState.Waiting;

            case ELevelPlayProcessState.Failed:
                return EProcessVisualState.Failed;

            case ELevelPlayProcessState.Completed:
                return EProcessVisualState.Completed;

            default:
                return EProcessVisualState.Default;
        }
    }

    private static EConnectionVisualState ToConnectionVisualState(ELevelPlayConnectionState state)
    {
        switch (state)
        {
            case ELevelPlayConnectionState.Occupied:
                return EConnectionVisualState.Occupied;

            case ELevelPlayConnectionState.Waiting:
                return EConnectionVisualState.Waiting;

            case ELevelPlayConnectionState.Completed:
                return EConnectionVisualState.Completed;

            case ELevelPlayConnectionState.Blocked:
                return EConnectionVisualState.Blocked;

            default:
                return EConnectionVisualState.Planned;
        }
    }

    private static Vector2 ToVector2(Vector3 position)
    {
        return new Vector2(position.x, position.y);
    }

    private Vector2 ToBoardLocalPosition(Vector2 worldPosition)
    {
        Vector3 localPosition = _boardRoot.InverseTransformPoint(worldPosition);
        return ToVector2(localPosition);
    }

    private static Dictionary<int, LevelPlayRoundResourceDTO> CreateRoundResourceMap(
        IReadOnlyList<LevelPlayRoundResourceDTO> resourceList)
    {
        Dictionary<int, LevelPlayRoundResourceDTO> resourceByIdDict = new();

        if (resourceList == null)
        {
            return resourceByIdDict;
        }

        for (int i = 0; i < resourceList.Count; i++)
        {
            LevelPlayRoundResourceDTO resource = resourceList[i];
            resourceByIdDict[resource.Id] = resource;
        }

        return resourceByIdDict;
    }

    private static void SetInactiveViews<TView>(IReadOnlyDictionary<int, TView> viewByIdDict,
                                                HashSet<int> activeIdSet) where TView : Component
    {
        foreach (KeyValuePair<int, TView> pair in viewByIdDict)
        {
            if (!activeIdSet.Contains(pair.Key))
            {
                pair.Value.gameObject.SetActive(false);
            }
        }
    }
}
