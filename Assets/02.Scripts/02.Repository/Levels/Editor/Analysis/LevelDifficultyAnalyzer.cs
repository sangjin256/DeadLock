using System;
using System.Collections.Generic;

public sealed class LevelDifficultyAnalyzer
{
    private const float RoundPressureWeight = 0.25f;
    private const float FlowPressureWeight = 0.25f;
    private const float SolutionScarcityPressureWeight = 0.25f;
    private const float RuleComplexityPressureWeight = 0.15f;
    private const float DistancePressureWeight = 0.10f;

    private readonly LevelSOMapper _mapper = new LevelSOMapper();

    public LevelDifficultyReport Analyze(LevelSO levelSO, LevelSolveReport solveReport)
    {
        if (levelSO == null)
        {
            return LevelDifficultyReport.CreateUnavailable("LevelSO가 선택되지 않아 난이도를 분석할 수 없습니다.");
        }

        if (solveReport == null)
        {
            return LevelDifficultyReport.CreateUnavailable("자동 해 찾기 결과가 없어 난이도를 분석할 수 없습니다.");
        }

        if (solveReport.BestCandidate == null)
        {
            return LevelDifficultyReport.CreateUnavailable("성공 해가 없어 난이도를 분석할 수 없습니다.");
        }

        LevelDefinition definition = _mapper.ToLevelDefinition(levelSO);

        if (definition is null)
        {
            return LevelDifficultyReport.CreateUnavailable("LevelSO를 LevelDefinition으로 변환할 수 없습니다.");
        }

        LevelSolveCandidate candidate = solveReport.BestCandidate;
        int assignmentCount = Math.Max(1, candidate.AssignmentArray.Length);
        int optimalRoundCount = Math.Max(1, candidate.ClearRoundCount);
        int minimumPossibleRoundCount = Math.Max(1, solveReport.MinimumPossibleRoundCount);
        int deferredCount = CountDeferred(candidate.SimulationReport);
        float averageDistance = candidate.TotalDistance / (float)assignmentCount;

        float roundPressure = CalculateRoundPressure(optimalRoundCount, minimumPossibleRoundCount);
        float flowPressure = CalculateFlowPressure(candidate.WaitingCount,
                                                   candidate.RequeuedCount,
                                                   deferredCount,
                                                   candidate.BlockedCount,
                                                   assignmentCount);
        float solutionScarcityPressure = CalculateSolutionScarcityPressure(solveReport.FoundSolutionCount,
                                                                           solveReport.EvaluatedCandidateCount);
        float ruleComplexityPressure = CalculateRuleComplexityPressure(definition);
        float distancePressure = CalculateDistancePressure(averageDistance, definition);
        float weightedPressure = Clamp01((roundPressure * RoundPressureWeight) +
                                         (flowPressure * FlowPressureWeight) +
                                         (solutionScarcityPressure * SolutionScarcityPressureWeight) +
                                         (ruleComplexityPressure * RuleComplexityPressureWeight) +
                                         (distancePressure * DistancePressureWeight));
        float score = RoundToSingleDecimal(1f + (4f * weightedPressure));
        ELevelDifficultyGrade grade = GetGrade(score);
        string gradeText = GetGradeText(grade);
        List<string> reasonList = CreateReasonList(optimalRoundCount,
                                                   minimumPossibleRoundCount,
                                                   candidate.WaitingCount,
                                                   candidate.RequeuedCount,
                                                   deferredCount,
                                                   solveReport.FoundSolutionCount,
                                                   solveReport.EvaluatedCandidateCount,
                                                   averageDistance,
                                                   roundPressure,
                                                   flowPressure,
                                                   solutionScarcityPressure,
                                                   ruleComplexityPressure,
                                                   distancePressure);
        string summary = $"난이도 {score:0.0} / 5.0 ({gradeText})";

        return new LevelDifficultyReport(true,
                                         score,
                                         grade,
                                         gradeText,
                                         summary,
                                         string.Empty,
                                         reasonList,
                                         optimalRoundCount,
                                         minimumPossibleRoundCount,
                                         candidate.WaitingCount,
                                         candidate.RequeuedCount,
                                         deferredCount,
                                         candidate.BlockedCount,
                                         solveReport.FoundSolutionCount,
                                         solveReport.EvaluatedCandidateCount,
                                         assignmentCount,
                                         averageDistance,
                                         roundPressure,
                                         flowPressure,
                                         solutionScarcityPressure,
                                         ruleComplexityPressure,
                                         distancePressure);
    }

    private static int CountDeferred(SimulationReport report)
    {
        if (report is null)
        {
            return 0;
        }

        int count = 0;

        for (int i = 0; i < report.RoundResultList.Length; i++)
        {
            count += report.RoundResultList[i].DeferredConnectionIdList.Count;
        }

        return count;
    }

    private static float CalculateRoundPressure(int optimalRoundCount, int minimumPossibleRoundCount)
    {
        float extraRoundCount = Math.Max(0, optimalRoundCount - minimumPossibleRoundCount);
        float tolerance = Math.Max(1f, minimumPossibleRoundCount * 1.5f);
        return Clamp01(extraRoundCount / tolerance);
    }

    private static float CalculateFlowPressure(
        int waitingCount,
        int requeuedCount,
        int deferredCount,
        int blockedCount,
        int assignmentCount)
    {
        float flowCost = waitingCount + requeuedCount + deferredCount + (blockedCount * 2f);
        float tolerance = Math.Max(1f, assignmentCount * 0.75f);
        return Clamp01(flowCost / tolerance);
    }

    private static float CalculateSolutionScarcityPressure(int foundSolutionCount, int evaluatedCandidateCount)
    {
        if (evaluatedCandidateCount <= 0 || foundSolutionCount <= 0)
        {
            return 1f;
        }

        float successRatio = foundSolutionCount / (float)evaluatedCandidateCount;
        return 1f - Clamp01(successRatio / 0.10f);
    }

    private static float CalculateRuleComplexityPressure(LevelDefinition definition)
    {
        float complexity = 0f;

        for (int i = 0; i < definition.ResourceList.Length; i++)
        {
            ResourceDefinition resource = definition.ResourceList[i];

            for (int ruleIndex = 0; ruleIndex < resource.RuleDefinitionList.Length; ruleIndex++)
            {
                complexity += GetResourceRuleWeight(resource.RuleDefinitionList[ruleIndex]);
            }
        }

        for (int i = 0; i < definition.BoardRuleList.Length; i++)
        {
            complexity += GetBoardRuleWeight(definition.BoardRuleList[i]);
        }

        float denominator = Math.Max(1f, definition.ResourceList.Length + definition.BoardRuleList.Length);
        return Clamp01(complexity / denominator);
    }

    private static float CalculateDistancePressure(float averageDistance, LevelDefinition definition)
    {
        int boardDistance = Math.Max(1, definition.RowCount + definition.ColumnCount - 2);
        return Clamp01(averageDistance / boardDistance);
    }

    private static float GetResourceRuleWeight(ResourceRuleDefinition ruleDefinition)
    {
        if (ruleDefinition is ClockRuleDefinition)
        {
            return 1.0f;
        }

        if (ruleDefinition is ColorSwitchRuleDefinition ||
            ruleDefinition is SimultaneousRuleDefinition)
        {
            return 0.8f;
        }

        if (ruleDefinition is EmptyColorRuleDefinition)
        {
            return 0.5f;
        }

        return 0f;
    }

    private static float GetBoardRuleWeight(BoardRuleDefinition ruleDefinition)
    {
        RelayRuleDefinition relayRuleDefinition = ruleDefinition as RelayRuleDefinition;

        if (relayRuleDefinition is null)
        {
            return 0f;
        }

        return relayRuleDefinition.RelayType == ERelayType.Transfer ? 1.0f : 0.8f;
    }

    private static List<string> CreateReasonList(
        int optimalRoundCount,
        int minimumPossibleRoundCount,
        int waitingCount,
        int requeuedCount,
        int deferredCount,
        int foundSolutionCount,
        int evaluatedCandidateCount,
        float averageDistance,
        float roundPressure,
        float flowPressure,
        float solutionScarcityPressure,
        float ruleComplexityPressure,
        float distancePressure)
    {
        List<string> reasonList = new List<string>
        {
            $"라운드 압박 {GetPressureText(roundPressure)}: 최선 {optimalRoundCount}R / 이론상 최소 {minimumPossibleRoundCount}R",
            $"흐름 압박 {GetPressureText(flowPressure)}: waiting {waitingCount}, 재투입 {requeuedCount}, 이월 {deferredCount}",
            $"성공 해 희소성 {GetPressureText(solutionScarcityPressure)}: 성공 {foundSolutionCount:N0} / 후보 평가 {evaluatedCandidateCount:N0}",
            $"Rule/Relay 복잡도 {GetPressureText(ruleComplexityPressure)}",
            $"평균 연결 거리 {GetPressureText(distancePressure)}: {averageDistance:0.0}",
        };

        return reasonList;
    }

    private static ELevelDifficultyGrade GetGrade(float score)
    {
        if (score < 1.8f)
        {
            return ELevelDifficultyGrade.VeryEasy;
        }

        if (score < 2.6f)
        {
            return ELevelDifficultyGrade.Easy;
        }

        if (score < 3.4f)
        {
            return ELevelDifficultyGrade.Normal;
        }

        if (score < 4.2f)
        {
            return ELevelDifficultyGrade.Hard;
        }

        return ELevelDifficultyGrade.VeryHard;
    }

    private static string GetGradeText(ELevelDifficultyGrade grade)
    {
        switch (grade)
        {
            case ELevelDifficultyGrade.VeryEasy:
                return "매우 쉬움";
            case ELevelDifficultyGrade.Easy:
                return "쉬움";
            case ELevelDifficultyGrade.Normal:
                return "보통";
            case ELevelDifficultyGrade.Hard:
                return "어려움";
            case ELevelDifficultyGrade.VeryHard:
                return "매우 어려움";
            default:
                return "알 수 없음";
        }
    }

    private static string GetPressureText(float pressure)
    {
        if (pressure < 0.34f)
        {
            return "낮음";
        }

        if (pressure < 0.67f)
        {
            return "보통";
        }

        return "높음";
    }

    private static float RoundToSingleDecimal(float value)
    {
        return (float)Math.Round(value, 1);
    }

    private static float Clamp01(float value)
    {
        if (value < 0f)
        {
            return 0f;
        }

        if (value > 1f)
        {
            return 1f;
        }

        return value;
    }
}
