using System;
using System.Collections.Generic;
using UnityEditor;

public sealed class LevelSolutionFinder
{
    private const int SmallAssignmentRandomOrderVariantCount = 50000;
    private const int MediumAssignmentRandomOrderVariantCount = 4096;
    private const int LargeAssignmentRandomOrderVariantCount = 512;

    private readonly LevelSOMapper _mapper = new LevelSOMapper();
    private readonly LevelDefinitionValidator _validator = new LevelDefinitionValidator();
    private readonly LevelBoardFactory _factory = new LevelBoardFactory();

    private LevelDefinition _definition;
    private LevelSolveSettings _settings;
    private List<SolveTarget> _targetList;
    private LevelSolveAssignment[] _currentAssignmentArray;
    private LevelSolveCandidate _bestCandidate;
    private int _minimumPossibleRoundCount;
    private int _exploredNodeCount;
    private int _evaluatedCandidateCount;
    private int _foundSolutionCount;
    private int _slowestSucceededRoundCount;
    private long _assignmentCombinationCount;
    private bool _isCancelled;
    private bool _isSearchLimitReached;
    private bool _isMinimumRoundReached;

    public LevelSolveReport FindBestSolution(LevelSO levelSO, LevelSolveSettings settings)
    {
        _settings = settings ?? LevelSolveSettings.CreateDefault();
        _definition = _mapper.ToLevelDefinition(levelSO);
        InitSearchState();

        LevelValidationResult validationResult = _validator.Validate(_definition);

        if (!validationResult.IsValid)
        {
            return CreateReport(ELevelSolveEndState.ValidationFailed,
                                validationResult,
                                false,
                                "최종 검증 오류가 있어 자동 해 찾기를 실행할 수 없습니다.");
        }

        if (!TryBuildTargets(out string targetErrorMessage))
        {
            return CreateReport(ELevelSolveEndState.NoSolution,
                                validationResult,
                                false,
                                targetErrorMessage);
        }

        try
        {
            Search(0);
        }
        finally
        {
            if (_settings.ShowProgressBar)
            {
                EditorUtility.ClearProgressBar();
            }
        }

        bool isOptimalProven = !_isCancelled &&
                               !_isSearchLimitReached &&
                               _isMinimumRoundReached;

        if (_isCancelled)
        {
            return CreateReport(ELevelSolveEndState.Cancelled,
                                validationResult,
                                false,
                                "사용자가 자동 해 찾기를 취소했습니다.");
        }

        if (_bestCandidate is null && !_isSearchLimitReached)
        {
            return CreateReport(ELevelSolveEndState.NoSolution,
                                validationResult,
                                false,
                                CreateNoSolutionMessage());
        }

        if (_bestCandidate is null && _isSearchLimitReached)
        {
            return CreateReport(ELevelSolveEndState.SearchLimitReached,
                                validationResult,
                                false,
                                CreateSearchLimitNoSolutionMessage());
        }

        if (_bestCandidate is null && _isSearchLimitReached && DateTime.MinValue == DateTime.MaxValue)
        {
            return CreateReport(ELevelSolveEndState.SearchLimitReached,
                                validationResult,
                                false,
                                "탐색 제한에 도달했지만 성공 해를 찾지 못했습니다.");
        }

        if (_bestCandidate is null)
        {
            return CreateReport(ELevelSolveEndState.NoSolution,
                                validationResult,
                                false,
                                "성공 가능한 예약 연결 조합을 찾지 못했습니다.");
        }

        if (_isSearchLimitReached)
        {
            return CreateReport(ELevelSolveEndState.BestFoundUnproven,
                                validationResult,
                                false,
                                "탐색 제한에 도달해 현재 최선 해만 확인되었습니다.");
        }

        if (!isOptimalProven)
        {
            return CreateReport(ELevelSolveEndState.BestFoundUnproven,
                                validationResult,
                                false,
                                "Best solution found from ordered candidates. Optimal order is not fully proven.");
        }

        return CreateReport(ELevelSolveEndState.SolvedOptimal,
                            validationResult,
                            isOptimalProven,
                            "최적 해를 찾았습니다.");
    }

    private void InitSearchState()
    {
        _targetList = new List<SolveTarget>();
        _currentAssignmentArray = new LevelSolveAssignment[0];
        _bestCandidate = null;
        _minimumPossibleRoundCount = 0;
        _exploredNodeCount = 0;
        _evaluatedCandidateCount = 0;
        _foundSolutionCount = 0;
        _slowestSucceededRoundCount = 0;
        _assignmentCombinationCount = 1;
        _isCancelled = false;
        _isSearchLimitReached = false;
        _isMinimumRoundReached = false;
    }

    private bool TryBuildTargets(out string errorMessage)
    {
        errorMessage = string.Empty;

        if (_definition.ProcessList.Length == 0 || _definition.ResourceList.Length == 0)
        {
            errorMessage = "Process 또는 Resource가 없어 자동 해 찾기를 실행할 수 없습니다.";
            return false;
        }

        for (int processIndex = 0; processIndex < _definition.ProcessList.Length; processIndex++)
        {
            ProcessDefinition process = _definition.ProcessList[processIndex];
            _minimumPossibleRoundCount = Math.Max(_minimumPossibleRoundCount, process.SlotList.Length);

            for (int slotIndex = 0; slotIndex < process.SlotList.Length; slotIndex++)
            {
                ProcessSlotDefinition slot = process.SlotList[slotIndex];
                List<ResourceCandidate> candidateList = CreateResourceCandidateList(process, slot);

                if (candidateList.Count == 0)
                {
                    errorMessage = $"P{process.Id}:S{slot.Id} 슬롯에 예약 가능한 Resource가 없습니다.";
                    return false;
                }

                _targetList.Add(new SolveTarget(process.Id,
                                                slot.Id,
                                                slot.SelectionOrder,
                                                candidateList));
                _assignmentCombinationCount = CalculateCappedCombinationCount(_assignmentCombinationCount,
                                                                              candidateList.Count);
            }
        }

        if (_targetList.Count == 0)
        {
            errorMessage = "Process 슬롯이 없어 자동 해 찾기를 실행할 수 없습니다.";
            return false;
        }

        _targetList.Sort(CompareTargets);
        _currentAssignmentArray = new LevelSolveAssignment[_targetList.Count];
        return true;
    }

    private List<ResourceCandidate> CreateResourceCandidateList(ProcessDefinition process, ProcessSlotDefinition slot)
    {
        List<ResourceCandidate> candidateList = new List<ResourceCandidate>();

        for (int i = 0; i < _definition.ResourceList.Length; i++)
        {
            ResourceDefinition resource = _definition.ResourceList[i];
            Board board = _factory.CreateBoard(_definition);
            AssignConnectionResult result = board.AssignConnection(process.Id, slot.Id, resource.Id);

            if (!result.Success)
            {
                continue;
            }

            int distance = process.Position.GetManhattanDistance(resource.Position);
            candidateList.Add(new ResourceCandidate(resource.Id, distance));
        }

        candidateList.Sort(CompareResourceCandidates);
        return candidateList;
    }

    private void Search(int targetIndex)
    {
        if (_isCancelled || _isSearchLimitReached || _isMinimumRoundReached)
        {
            return;
        }

        if (targetIndex >= _targetList.Count)
        {
            EvaluateCurrentAssignments();
            return;
        }

        SolveTarget target = _targetList[targetIndex];

        for (int i = 0; i < target.CandidateList.Count; i++)
        {
            _exploredNodeCount++;

            if (_exploredNodeCount > _settings.MaxSearchNodeCount)
            {
                _isSearchLimitReached = true;
                return;
            }

            if (ShouldCancelByProgressBar())
            {
                _isCancelled = true;
                return;
            }

            ResourceCandidate candidate = target.CandidateList[i];
            _currentAssignmentArray[targetIndex] = new LevelSolveAssignment(target.ProcessId,
                                                                            target.SlotId,
                                                                            candidate.ResourceId);

            Search(targetIndex + 1);

            if (_isCancelled || _isSearchLimitReached || _isMinimumRoundReached)
            {
                return;
            }
        }
    }

    private bool ShouldCancelByProgressBar()
    {
        if (!_settings.ShowProgressBar || _exploredNodeCount % 128 != 0)
        {
            return false;
        }

        float nodeProgress = (float)_exploredNodeCount / _settings.MaxSearchNodeCount;
        float candidateProgress = (float)_evaluatedCandidateCount / _settings.MaxEvaluatedCandidateCount;
        float progress = Math.Min(1f, Math.Max(nodeProgress, candidateProgress));
        string info = $"탐색 노드 {_exploredNodeCount:N0} / {_settings.MaxSearchNodeCount:N0}, " +
                      $"후보 평가 {_evaluatedCandidateCount:N0} / {_settings.MaxEvaluatedCandidateCount:N0}";
        return EditorUtility.DisplayCancelableProgressBar("최적 해 찾기", info, progress);
    }

    private void EvaluateCurrentAssignments()
    {
        List<LevelSolveAssignment[]> orderedAssignmentVariantList = CreateOrderedAssignmentVariantList(_currentAssignmentArray);

        for (int i = 0; i < orderedAssignmentVariantList.Count; i++)
        {
            if (_evaluatedCandidateCount >= _settings.MaxEvaluatedCandidateCount)
            {
                _isSearchLimitReached = true;
                return;
            }

            _evaluatedCandidateCount++;

            LevelSolveAssignment[] orderedAssignmentArray = orderedAssignmentVariantList[i];
            LevelDefinition orderedDefinition = LevelDefinitionSelectionOrderUtility.CreateWithAssignmentOrder(_definition,
                                                                                                             orderedAssignmentArray);
            Board board = _factory.CreateBoard(orderedDefinition);

            for (int j = 0; j < orderedAssignmentArray.Length; j++)
            {
                LevelSolveAssignment assignment = orderedAssignmentArray[j];
                AssignConnectionResult result = board.AssignConnection(assignment.ProcessId,
                                                                       assignment.SlotId,
                                                                       assignment.ResourceId);

                if (!result.Success)
                {
                    return;
                }
            }

            SimulationReport simulationReport = board.RunSimulation(_settings.MaxRoundCount);

            if (simulationReport.EndState != ESimulationEndState.Succeeded)
            {
                continue;
            }

            _foundSolutionCount++;

            int clearRoundCount = simulationReport.RoundResultList.Length;
            _slowestSucceededRoundCount = Math.Max(_slowestSucceededRoundCount, clearRoundCount);

            LevelSolveCandidate candidate = new LevelSolveCandidate(orderedAssignmentArray,
                                                                    simulationReport,
                                                                    clearRoundCount,
                                                                    CountWaiting(simulationReport),
                                                                    CountRequeued(simulationReport),
                                                                    CountBlocked(simulationReport),
                                                                    CalculateTotalDistance(orderedAssignmentArray));

            if (_bestCandidate is null || CompareCandidates(candidate, _bestCandidate) < 0)
            {
                _bestCandidate = candidate;
            }

            if (_bestCandidate.ClearRoundCount <= _minimumPossibleRoundCount)
            {
                _isMinimumRoundReached = true;
                return;
            }
        }
    }

    private LevelSolveReport CreateReport(
        ELevelSolveEndState endState,
        LevelValidationResult validationResult,
        bool isOptimalProven,
        string message)
    {
        LevelStarThresholdRecommendation recommendation = CreateStarThresholdRecommendation(isOptimalProven);

        return new LevelSolveReport(endState,
                                    _bestCandidate,
                                    recommendation,
                                    validationResult,
                                    _minimumPossibleRoundCount,
                                    _exploredNodeCount,
                                    _evaluatedCandidateCount,
                                    _foundSolutionCount,
                                    isOptimalProven,
                                    message);
    }

    private LevelStarThresholdRecommendation CreateStarThresholdRecommendation(bool isOptimalProven)
    {
        if (_bestCandidate is null)
        {
            return null;
        }

        int optimalRoundCount = _bestCandidate.ClearRoundCount;
        int margin = Math.Max(1, (int)Math.Ceiling(optimalRoundCount * 0.5f));
        int twoStarRoundCount = optimalRoundCount + margin;
        int oneStarRoundCount = twoStarRoundCount + margin;

        if (_slowestSucceededRoundCount > 0)
        {
            oneStarRoundCount = Math.Max(oneStarRoundCount, _slowestSucceededRoundCount);
        }

        string reason = isOptimalProven ?
            "증명된 최적 라운드 기준 추천입니다." :
            "탐색 제한 또는 취소 전 현재 최선 라운드 기준 추천입니다.";

        return new LevelStarThresholdRecommendation(optimalRoundCount,
                                                    twoStarRoundCount,
                                                    oneStarRoundCount,
                                                    isOptimalProven,
                                                    reason);
    }

    private int CalculateTotalDistance(LevelSolveAssignment[] assignmentArray)
    {
        int totalDistance = 0;

        for (int i = 0; i < assignmentArray.Length; i++)
        {
            ProcessDefinition process = FindProcess(assignmentArray[i].ProcessId);
            ResourceDefinition resource = FindResource(assignmentArray[i].ResourceId);

            if (process is null || resource is null)
            {
                continue;
            }

            totalDistance += process.Position.GetManhattanDistance(resource.Position);
        }

        return totalDistance;
    }

    private ProcessDefinition FindProcess(int processId)
    {
        for (int i = 0; i < _definition.ProcessList.Length; i++)
        {
            if (_definition.ProcessList[i].Id == processId)
            {
                return _definition.ProcessList[i];
            }
        }

        return null;
    }

    private ResourceDefinition FindResource(int resourceId)
    {
        for (int i = 0; i < _definition.ResourceList.Length; i++)
        {
            if (_definition.ResourceList[i].Id == resourceId)
            {
                return _definition.ResourceList[i];
            }
        }

        return null;
    }

    private ProcessSlotDefinition FindSlot(int processId, int slotId)
    {
        ProcessDefinition process = FindProcess(processId);

        if (process is null)
        {
            return null;
        }

        for (int i = 0; i < process.SlotList.Length; i++)
        {
            if (process.SlotList[i].Id == slotId)
            {
                return process.SlotList[i];
            }
        }

        return null;
    }

    private List<LevelSolveAssignment[]> CreateOrderedAssignmentVariantList(LevelSolveAssignment[] assignmentArray)
    {
        List<LevelSolveAssignment[]> variantList = new List<LevelSolveAssignment[]>();
        HashSet<string> signatureSet = new HashSet<string>();

        AddAssignmentVariant(variantList, signatureSet, CreateSortedAssignmentArray(assignmentArray));
        AddAssignmentVariant(variantList, signatureSet, CreateSearchOrderAssignmentArray(assignmentArray));
        AddAssignmentVariant(variantList, signatureSet, CreateUrgencyAssignmentArray(assignmentArray));
        AddAssignmentVariant(variantList, signatureSet, CreateEarlyWaitingAssignmentArray(assignmentArray));
        AddAssignmentVariant(variantList, signatureSet, CreateDistanceOrderedAssignmentArray(assignmentArray));
        AddRandomAssignmentVariants(variantList, signatureSet, assignmentArray);

        return variantList;
    }

    private void AddAssignmentVariant(
        List<LevelSolveAssignment[]> variantList,
        HashSet<string> signatureSet,
        LevelSolveAssignment[] assignmentArray)
    {
        string signature = CreateAssignmentOrderSignature(assignmentArray);

        if (!signatureSet.Add(signature))
        {
            return;
        }

        variantList.Add(assignmentArray);
    }

    private string CreateAssignmentOrderSignature(LevelSolveAssignment[] assignmentArray)
    {
        List<string> partList = new List<string>();

        for (int i = 0; i < assignmentArray.Length; i++)
        {
            LevelSolveAssignment assignment = assignmentArray[i];
            partList.Add(assignment.ProcessId + ":" + assignment.SlotId + ":" + assignment.ResourceId);
        }

        return string.Join("|", partList);
    }

    private LevelSolveAssignment[] CreateSearchOrderAssignmentArray(LevelSolveAssignment[] assignmentArray)
    {
        LevelSolveAssignment[] resultArray = new LevelSolveAssignment[assignmentArray.Length];

        for (int i = 0; i < assignmentArray.Length; i++)
        {
            resultArray[i] = assignmentArray[i];
        }

        return resultArray;
    }

    private LevelSolveAssignment[] CreateUrgencyAssignmentArray(LevelSolveAssignment[] assignmentArray)
    {
        LevelSolveAssignment[] resultArray = CreateSearchOrderAssignmentArray(assignmentArray);
        Array.Sort(resultArray, CompareAssignmentsByUrgency);
        return resultArray;
    }

    private LevelSolveAssignment[] CreateDistanceOrderedAssignmentArray(LevelSolveAssignment[] assignmentArray)
    {
        LevelSolveAssignment[] resultArray = CreateSearchOrderAssignmentArray(assignmentArray);
        Array.Sort(resultArray, CompareAssignmentsByDistance);
        return resultArray;
    }

    private LevelSolveAssignment[] CreateEarlyWaitingAssignmentArray(LevelSolveAssignment[] assignmentArray)
    {
        LevelSolveAssignment[] resultArray = CreateSearchOrderAssignmentArray(assignmentArray);
        Array.Sort(resultArray, CompareAssignmentsByEarlyWaiting);
        return resultArray;
    }

    private LevelSolveAssignment[] CreateSortedAssignmentArray(LevelSolveAssignment[] assignmentArray)
    {
        LevelSolveAssignment[] sortedArray = new LevelSolveAssignment[assignmentArray.Length];

        for (int i = 0; i < assignmentArray.Length; i++)
        {
            sortedArray[i] = assignmentArray[i];
        }

        Array.Sort(sortedArray, CompareAssignments);
        return sortedArray;
    }

    private void AddRandomAssignmentVariants(
        List<LevelSolveAssignment[]> variantList,
        HashSet<string> signatureSet,
        LevelSolveAssignment[] assignmentArray)
    {
        Random random = new Random(CreateAssignmentSeed(assignmentArray));

        int maxRandomOrderVariantCount = GetMaxRandomOrderVariantCount();

        for (int i = 0; i < maxRandomOrderVariantCount; i++)
        {
            AddAssignmentVariant(variantList, signatureSet, CreateRandomProcessOrderAssignmentArray(assignmentArray, random));
        }
    }

    private int GetMaxRandomOrderVariantCount()
    {
        if (_assignmentCombinationCount <= 1)
        {
            return SmallAssignmentRandomOrderVariantCount;
        }

        if (_assignmentCombinationCount <= 128)
        {
            return MediumAssignmentRandomOrderVariantCount;
        }

        return LargeAssignmentRandomOrderVariantCount;
    }

    private string CreateNoSolutionMessage()
    {
        return "성공 가능한 예약 연결 조합을 찾지 못했습니다. " +
               $"후보 {_evaluatedCandidateCount:N0}개를 평가했습니다.";
    }

    private string CreateSearchLimitNoSolutionMessage()
    {
        return "탐색 제한에 도달했지만 성공 해를 찾지 못했습니다. " +
               $"후보 {_evaluatedCandidateCount:N0}개를 평가했습니다.";
    }

    private static long CalculateCappedCombinationCount(long currentCount, int candidateCount)
    {
        const long maxCount = long.MaxValue / 2;

        if (candidateCount <= 0)
        {
            return currentCount;
        }

        if (currentCount > maxCount / candidateCount)
        {
            return maxCount;
        }

        return currentCount * candidateCount;
    }

    private LevelSolveAssignment[] CreateRandomProcessOrderAssignmentArray(
        LevelSolveAssignment[] assignmentArray,
        Random random)
    {
        Dictionary<int, List<LevelSolveAssignment>> assignmentListByProcessIdDict = new Dictionary<int, List<LevelSolveAssignment>>();
        List<int> processIdList = new List<int>();

        for (int i = 0; i < assignmentArray.Length; i++)
        {
            LevelSolveAssignment assignment = assignmentArray[i];

            if (!assignmentListByProcessIdDict.TryGetValue(assignment.ProcessId, out List<LevelSolveAssignment> processAssignmentList))
            {
                processAssignmentList = new List<LevelSolveAssignment>();
                assignmentListByProcessIdDict.Add(assignment.ProcessId, processAssignmentList);
                processIdList.Add(assignment.ProcessId);
            }

            processAssignmentList.Add(assignment);
        }

        processIdList.Sort();
        List<LevelSolveAssignment> resultList = new List<LevelSolveAssignment>(assignmentArray.Length);

        for (int i = 0; i < processIdList.Count; i++)
        {
            List<LevelSolveAssignment> processAssignmentList = assignmentListByProcessIdDict[processIdList[i]];
            Shuffle(processAssignmentList, random);
            resultList.AddRange(processAssignmentList);
        }

        return resultList.ToArray();
    }

    private static void Shuffle(List<LevelSolveAssignment> assignmentList, Random random)
    {
        for (int i = assignmentList.Count - 1; i > 0; i--)
        {
            int swapIndex = random.Next(i + 1);
            LevelSolveAssignment temp = assignmentList[i];
            assignmentList[i] = assignmentList[swapIndex];
            assignmentList[swapIndex] = temp;
        }
    }

    private static int CreateAssignmentSeed(LevelSolveAssignment[] assignmentArray)
    {
        unchecked
        {
            int seed = 17;

            for (int i = 0; i < assignmentArray.Length; i++)
            {
                seed = seed * 31 + assignmentArray[i].ProcessId;
                seed = seed * 31 + assignmentArray[i].SlotId;
                seed = seed * 31 + assignmentArray[i].ResourceId;
            }

            return seed;
        }
    }

    private int CompareAssignments(LevelSolveAssignment left, LevelSolveAssignment right)
    {
        int result = left.ProcessId.CompareTo(right.ProcessId);

        if (result != 0)
        {
            return result;
        }

        ProcessSlotDefinition leftSlot = FindSlot(left.ProcessId, left.SlotId);
        ProcessSlotDefinition rightSlot = FindSlot(right.ProcessId, right.SlotId);
        int leftSelectionOrder = leftSlot?.SelectionOrder ?? left.SlotId;
        int rightSelectionOrder = rightSlot?.SelectionOrder ?? right.SlotId;
        result = leftSelectionOrder.CompareTo(rightSelectionOrder);

        if (result != 0)
        {
            return result;
        }

        result = left.SlotId.CompareTo(right.SlotId);

        if (result != 0)
        {
            return result;
        }

        return left.ResourceId.CompareTo(right.ResourceId);
    }

    private int CompareAssignmentsByUrgency(LevelSolveAssignment left, LevelSolveAssignment right)
    {
        int result = left.ProcessId.CompareTo(right.ProcessId);

        if (result != 0)
        {
            return result;
        }

        result = GetAssignmentUrgency(left).CompareTo(GetAssignmentUrgency(right));

        if (result != 0)
        {
            return result;
        }

        result = GetAssignmentDistance(left).CompareTo(GetAssignmentDistance(right));

        if (result != 0)
        {
            return result;
        }

        return left.SlotId.CompareTo(right.SlotId);
    }

    private int CompareAssignmentsByDistance(LevelSolveAssignment left, LevelSolveAssignment right)
    {
        int result = left.ProcessId.CompareTo(right.ProcessId);

        if (result != 0)
        {
            return result;
        }

        result = GetAssignmentDistance(left).CompareTo(GetAssignmentDistance(right));

        if (result != 0)
        {
            return result;
        }

        result = GetAssignmentUrgency(left).CompareTo(GetAssignmentUrgency(right));

        if (result != 0)
        {
            return result;
        }

        return left.SlotId.CompareTo(right.SlotId);
    }

    private int CompareAssignmentsByEarlyWaiting(LevelSolveAssignment left, LevelSolveAssignment right)
    {
        int result = left.ProcessId.CompareTo(right.ProcessId);

        if (result != 0)
        {
            return result;
        }

        result = GetEarlyWaitingPriority(left).CompareTo(GetEarlyWaitingPriority(right));

        if (result != 0)
        {
            return result;
        }

        result = GetAssignmentDistance(left).CompareTo(GetAssignmentDistance(right));

        if (result != 0)
        {
            return result;
        }

        return left.SlotId.CompareTo(right.SlotId);
    }

    private int GetAssignmentUrgency(LevelSolveAssignment assignment)
    {
        ResourceDefinition resource = FindResource(assignment.ResourceId);

        if (resource is null)
        {
            return 1000;
        }

        int urgency = 200;

        for (int i = 0; i < resource.RuleDefinitionList.Length; i++)
        {
            ResourceRuleDefinition ruleDefinition = resource.RuleDefinitionList[i];

            if (ruleDefinition is ClockRuleDefinition clockDefinition)
            {
                if (clockDefinition.Mode == EClockMode.OnToOff)
                {
                    urgency = Math.Min(urgency, clockDefinition.RoundCount);
                }
                else
                {
                    urgency = Math.Max(urgency, 300 + clockDefinition.RoundCount);
                }
            }
            else if (ruleDefinition is SimultaneousRuleDefinition)
            {
                urgency = Math.Min(urgency, 100);
            }
            else if (ruleDefinition is ColorSwitchRuleDefinition)
            {
                urgency = Math.Min(urgency, 150);
            }
            else if (ruleDefinition is EmptyColorRuleDefinition)
            {
                urgency = Math.Min(urgency, 180);
            }
        }

        return urgency;
    }

    private int GetEarlyWaitingPriority(LevelSolveAssignment assignment)
    {
        ResourceDefinition resource = FindResource(assignment.ResourceId);

        if (resource is null)
        {
            return 1000;
        }

        int priority = 500;

        for (int i = 0; i < resource.RuleDefinitionList.Length; i++)
        {
            ResourceRuleDefinition ruleDefinition = resource.RuleDefinitionList[i];

            if (ruleDefinition is ClockRuleDefinition clockDefinition)
            {
                if (clockDefinition.Mode == EClockMode.OffToOn)
                {
                    priority = Math.Min(priority, clockDefinition.RoundCount);
                }
                else
                {
                    priority = Math.Min(priority, 100 + clockDefinition.RoundCount);
                }
            }
            else if (ruleDefinition is SimultaneousRuleDefinition)
            {
                priority = Math.Min(priority, 200);
            }
            else if (ruleDefinition is ColorSwitchRuleDefinition)
            {
                priority = Math.Min(priority, 250);
            }
            else if (ruleDefinition is EmptyColorRuleDefinition)
            {
                priority = Math.Min(priority, 300);
            }
        }

        return priority;
    }

    private int GetAssignmentDistance(LevelSolveAssignment assignment)
    {
        ProcessDefinition process = FindProcess(assignment.ProcessId);
        ResourceDefinition resource = FindResource(assignment.ResourceId);

        if (process is null || resource is null)
        {
            return int.MaxValue;
        }

        return process.Position.GetManhattanDistance(resource.Position);
    }

    private int CompareTargets(SolveTarget left, SolveTarget right)
    {
        int result = left.CandidateList.Count.CompareTo(right.CandidateList.Count);

        if (result != 0)
        {
            return result;
        }

        result = GetTargetCriticalPriority(left).CompareTo(GetTargetCriticalPriority(right));

        if (result != 0)
        {
            return result;
        }

        result = left.ProcessId.CompareTo(right.ProcessId);

        if (result != 0)
        {
            return result;
        }

        result = left.SelectionOrder.CompareTo(right.SelectionOrder);

        if (result != 0)
        {
            return result;
        }

        return left.SlotId.CompareTo(right.SlotId);
    }

    private int CompareResourceCandidates(ResourceCandidate left, ResourceCandidate right)
    {
        int result = GetResourceSearchPriority(left.ResourceId).CompareTo(GetResourceSearchPriority(right.ResourceId));

        if (result != 0)
        {
            return result;
        }

        result = left.Distance.CompareTo(right.Distance);

        if (result != 0)
        {
            return result;
        }

        return left.ResourceId.CompareTo(right.ResourceId);
    }

    private int GetTargetCriticalPriority(SolveTarget target)
    {
        int priority = 10000;

        for (int i = 0; i < target.CandidateList.Count; i++)
        {
            priority = Math.Min(priority, GetResourceSearchPriority(target.CandidateList[i].ResourceId));
        }

        return priority;
    }

    private int GetResourceSearchPriority(int resourceId)
    {
        ResourceDefinition resource = FindResource(resourceId);

        if (resource is null)
        {
            return 10000;
        }

        bool hasSimultaneous = false;
        bool hasOnToOffClock = false;
        bool hasOffToOnClock = false;
        int onToOffRoundCount = 0;
        int offToOnRoundCount = 0;

        for (int i = 0; i < resource.RuleDefinitionList.Length; i++)
        {
            ResourceRuleDefinition ruleDefinition = resource.RuleDefinitionList[i];

            if (ruleDefinition is SimultaneousRuleDefinition)
            {
                hasSimultaneous = true;
            }
            else if (ruleDefinition is ClockRuleDefinition clockDefinition)
            {
                if (clockDefinition.Mode == EClockMode.OnToOff)
                {
                    hasOnToOffClock = true;
                    onToOffRoundCount = clockDefinition.RoundCount;
                }
                else
                {
                    hasOffToOnClock = true;
                    offToOnRoundCount = clockDefinition.RoundCount;
                }
            }
        }

        if (hasSimultaneous && hasOnToOffClock)
        {
            return onToOffRoundCount;
        }

        if (hasOnToOffClock)
        {
            return 100 + onToOffRoundCount;
        }

        if (hasSimultaneous)
        {
            return 200;
        }

        if (hasOffToOnClock)
        {
            return 400 + offToOnRoundCount;
        }

        return 1000;
    }

    private static int CompareCandidates(LevelSolveCandidate left, LevelSolveCandidate right)
    {
        int result = left.ClearRoundCount.CompareTo(right.ClearRoundCount);

        if (result != 0)
        {
            return result;
        }

        result = left.WaitingCount.CompareTo(right.WaitingCount);

        if (result != 0)
        {
            return result;
        }

        result = left.RequeuedCount.CompareTo(right.RequeuedCount);

        if (result != 0)
        {
            return result;
        }

        result = left.BlockedCount.CompareTo(right.BlockedCount);

        if (result != 0)
        {
            return result;
        }

        result = left.TotalDistance.CompareTo(right.TotalDistance);

        if (result != 0)
        {
            return result;
        }

        return CompareAssignmentArrays(left.AssignmentArray, right.AssignmentArray);
    }

    private static int CompareAssignmentArrays(LevelSolveAssignment[] leftArray, LevelSolveAssignment[] rightArray)
    {
        int length = Math.Min(leftArray.Length, rightArray.Length);

        for (int i = 0; i < length; i++)
        {
            int result = leftArray[i].ProcessId.CompareTo(rightArray[i].ProcessId);

            if (result != 0)
            {
                return result;
            }

            result = leftArray[i].SlotId.CompareTo(rightArray[i].SlotId);

            if (result != 0)
            {
                return result;
            }

            result = leftArray[i].ResourceId.CompareTo(rightArray[i].ResourceId);

            if (result != 0)
            {
                return result;
            }
        }

        return leftArray.Length.CompareTo(rightArray.Length);
    }

    private static int CountWaiting(SimulationReport report)
    {
        int count = 0;

        for (int i = 0; i < report.RoundResultList.Length; i++)
        {
            count += report.RoundResultList[i].WaitingConnectionIdList.Count;
        }

        return count;
    }

    private static int CountRequeued(SimulationReport report)
    {
        int count = 0;

        for (int i = 0; i < report.RoundResultList.Length; i++)
        {
            count += report.RoundResultList[i].RequeuedConnectionIdList.Count;
        }

        return count;
    }

    private static int CountBlocked(SimulationReport report)
    {
        int count = 0;

        for (int i = 0; i < report.RoundResultList.Length; i++)
        {
            count += report.RoundResultList[i].BlockedConnectionIdList.Count;
        }

        return count;
    }

    private sealed class SolveTarget
    {
        public readonly int ProcessId;
        public readonly int SlotId;
        public readonly int SelectionOrder;
        public readonly List<ResourceCandidate> CandidateList;

        public SolveTarget(
            int processId,
            int slotId,
            int selectionOrder,
            List<ResourceCandidate> candidateList)
        {
            ProcessId = processId;
            SlotId = slotId;
            SelectionOrder = selectionOrder;
            CandidateList = candidateList;
        }
    }

    private sealed class ResourceCandidate
    {
        public readonly int ResourceId;
        public readonly int Distance;

        public ResourceCandidate(int resourceId, int distance)
        {
            ResourceId = resourceId;
            Distance = distance;
        }
    }
}
