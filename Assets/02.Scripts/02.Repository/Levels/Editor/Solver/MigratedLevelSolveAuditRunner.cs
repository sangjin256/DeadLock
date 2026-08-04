using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class MigratedLevelSolveAuditRunner
{
    private const string MigratedAssetDirectory = "Assets/02.Scripts/02.Repository/Levels/Migrated";
    private const string MarkdownReportRelativePath = "Docs/DeadLock_MigratedLevelSolveAudit.md";
    private const string JsonReportRelativePath = "Docs/DeadLock_MigratedLevelSolveAudit.json";
    private const string PreviousBatchLogRelativePath = "Library/MigratedLevelSolveAudit.log";

    [MenuItem("Tools/DeadLock/Levels/Run Migrated Solver Audit")]
    public static void RunFromMenu()
    {
        RunAudit();
    }

    public static void RunFromBatchMode()
    {
        try
        {
            RunAudit();
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    private static void RunAudit()
    {
        string[] assetGuidArray = AssetDatabase.FindAssets("t:LevelSO", new[] { MigratedAssetDirectory });
        List<string> assetPathList = CreateSortedAssetPathList(assetGuidArray);
        Dictionary<string, LevelAuditData> auditDataByAssetPathDict = CreateCheckpointDataByAssetPathDict(assetPathList);

        for (int i = 0; i < assetPathList.Count; i++)
        {
            string assetPath = assetPathList[i];

            if (auditDataByAssetPathDict.ContainsKey(assetPath))
            {
                continue;
            }

            LevelSO levelSO = AssetDatabase.LoadAssetAtPath<LevelSO>(assetPath);
            LevelAuditData levelAuditData = CreateAuditData(assetPath, levelSO);
            auditDataByAssetPathDict.Add(assetPath, levelAuditData);
            WriteReportFiles(CreateReportData(CreateOrderedAuditDataList(assetPathList, auditDataByAssetPathDict)));
            Debug.Log($"[MigratedLevelSolveAudit] {i + 1}/{assetPathList.Count} {levelAuditData.LevelName}: {levelAuditData.Classification}");
        }

        AuditReportData reportData = CreateReportData(CreateOrderedAuditDataList(assetPathList, auditDataByAssetPathDict));
        WriteReportFiles(reportData);
        Debug.Log($"[MigratedLevelSolveAudit] Completed {reportData.TotalLevelCount} levels. " +
                  $"Report: {MarkdownReportRelativePath}");
    }

    private static List<string> CreateSortedAssetPathList(string[] assetGuidArray)
    {
        List<string> assetPathList = new List<string>(assetGuidArray.Length);

        for (int i = 0; i < assetGuidArray.Length; i++)
        {
            assetPathList.Add(AssetDatabase.GUIDToAssetPath(assetGuidArray[i]));
        }

        assetPathList.Sort(StringComparer.Ordinal);
        return assetPathList;
    }

    private static Dictionary<string, LevelAuditData> CreateCheckpointDataByAssetPathDict(IReadOnlyList<string> assetPathList)
    {
        Dictionary<string, LevelAuditData> auditDataByAssetPathDict = new Dictionary<string, LevelAuditData>();
        AuditReportData checkpointReportData = LoadCheckpointReportData();

        if (checkpointReportData != null)
        {
            AddCheckpointData(auditDataByAssetPathDict, checkpointReportData.LevelAuditDataArray, assetPathList);
        }

        RecoverPreviousBatchLogData(auditDataByAssetPathDict, assetPathList);
        return auditDataByAssetPathDict;
    }

    private static AuditReportData LoadCheckpointReportData()
    {
        string projectDirectoryPath = Directory.GetParent(Application.dataPath).FullName;
        string jsonReportPath = Path.Combine(projectDirectoryPath, JsonReportRelativePath);

        if (!File.Exists(jsonReportPath))
        {
            return null;
        }

        try
        {
            return JsonUtility.FromJson<AuditReportData>(File.ReadAllText(jsonReportPath));
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[MigratedLevelSolveAudit] Failed to load checkpoint report: {exception.Message}");
            return null;
        }
    }

    private static void AddCheckpointData(Dictionary<string, LevelAuditData> auditDataByAssetPathDict,
                                          LevelAuditData[] checkpointDataArray,
                                          IReadOnlyList<string> assetPathList)
    {
        if (checkpointDataArray == null)
        {
            return;
        }

        HashSet<string> validAssetPathSet = new HashSet<string>(assetPathList);

        for (int i = 0; i < checkpointDataArray.Length; i++)
        {
            LevelAuditData data = checkpointDataArray[i];

            if (data == null || string.IsNullOrEmpty(data.AssetPath) || !validAssetPathSet.Contains(data.AssetPath))
            {
                continue;
            }

            auditDataByAssetPathDict[data.AssetPath] = data;
        }
    }

    private static void RecoverPreviousBatchLogData(Dictionary<string, LevelAuditData> auditDataByAssetPathDict,
                                                    IReadOnlyList<string> assetPathList)
    {
        string projectDirectoryPath = Directory.GetParent(Application.dataPath).FullName;
        string previousBatchLogPath = Path.Combine(projectDirectoryPath, PreviousBatchLogRelativePath);

        if (!File.Exists(previousBatchLogPath))
        {
            return;
        }

        Dictionary<string, string> assetPathByLevelNameDict = new Dictionary<string, string>();

        for (int i = 0; i < assetPathList.Count; i++)
        {
            string assetPath = assetPathList[i];
            assetPathByLevelNameDict[Path.GetFileNameWithoutExtension(assetPath)] = assetPath;
        }

        Regex resultRegex = new Regex(@"\[MigratedLevelSolveAudit\] \d+/\d+ (?<name>[^:]+): (?<classification>\w+)");
        string[] logLineArray = File.ReadAllLines(previousBatchLogPath);

        for (int i = 0; i < logLineArray.Length; i++)
        {
            Match match = resultRegex.Match(logLineArray[i]);

            if (!match.Success || !assetPathByLevelNameDict.TryGetValue(match.Groups["name"].Value, out string assetPath))
            {
                continue;
            }

            if (auditDataByAssetPathDict.ContainsKey(assetPath))
            {
                continue;
            }

            LevelAuditData data = new LevelAuditData();
            data.AssetPath = assetPath;
            data.LevelName = match.Groups["name"].Value;
            data.GroupName = GetGroupName(data.LevelName);
            data.Classification = match.Groups["classification"].Value;
            data.SolverEndState = "RecoveredFromPreviousBatchLog";
            data.FinalMessage = "Recovered from the previous audit batch log after an unexpected shutdown. Detailed solver counters are unavailable.";
            data.WasRecoveredFromPreviousBatchLog = true;
            auditDataByAssetPathDict.Add(assetPath, data);
        }
    }

    private static List<LevelAuditData> CreateOrderedAuditDataList(IReadOnlyList<string> assetPathList,
                                                                    IReadOnlyDictionary<string, LevelAuditData> auditDataByAssetPathDict)
    {
        List<LevelAuditData> levelAuditDataList = new List<LevelAuditData>(auditDataByAssetPathDict.Count);

        for (int i = 0; i < assetPathList.Count; i++)
        {
            string assetPath = assetPathList[i];

            if (auditDataByAssetPathDict.TryGetValue(assetPath, out LevelAuditData data))
            {
                levelAuditDataList.Add(data);
            }
        }

        return levelAuditDataList;
    }

    private static LevelAuditData CreateAuditData(string assetPath, LevelSO levelSO)
    {
        LevelAuditData data = new LevelAuditData();
        data.AssetPath = assetPath;
        data.LevelName = Path.GetFileNameWithoutExtension(assetPath);
        data.GroupName = GetGroupName(data.LevelName);

        if (levelSO == null)
        {
            data.Classification = "AuditError";
            data.FinalMessage = "LevelSO asset could not be loaded.";
            return data;
        }

        try
        {
            LevelSolutionFinder finder = new LevelSolutionFinder();
            LevelSolveSettings settings = CreateEditorEquivalentSettings(levelSO);
            LevelSolveReport report = finder.FindBestSolution(levelSO, settings);
            ApplySolveResult(data, report);
        }
        catch (Exception exception)
        {
            data.Classification = "AuditError";
            data.FinalMessage = exception.ToString();
        }

        return data;
    }

    private static LevelSolveSettings CreateEditorEquivalentSettings(LevelSO levelSO)
    {
        int maxRoundCount = LevelSolveSettings.DefaultMaxRoundCount;
        LevelStarThresholdData thresholdData = levelSO.StarThresholdData;

        if (thresholdData != null)
        {
            maxRoundCount = Math.Max(maxRoundCount, thresholdData.OneStarRoundCount);
        }

        return new LevelSolveSettings(maxRoundCount,
                                      LevelSolveSettings.DefaultMaxSearchNodeCount,
                                      LevelSolveSettings.DefaultMaxEvaluatedCandidateCount,
                                      false);
    }

    private static void ApplySolveResult(LevelAuditData data, LevelSolveReport report)
    {
        data.SolverEndState = report.EndState.ToString();
        data.ExploredNodeCount = report.ExploredNodeCount;
        data.EvaluatedCandidateCount = report.EvaluatedCandidateCount;
        data.FoundSolutionCount = report.FoundSolutionCount;
        data.MinimumPossibleRoundCount = report.MinimumPossibleRoundCount;
        data.FinalMessage = report.Message;

        if (report.BestCandidate != null)
        {
            data.ClearRoundCount = report.BestCandidate.ClearRoundCount;
            data.Classification = report.IsOptimalProven ? "OptimalSolutionProven" : "SolutionFoundUnproven";
            return;
        }

        if (report.EndState == ELevelSolveEndState.ValidationFailed)
        {
            data.Classification = "DefinitionValidationFailed";
            return;
        }

        if (report.EndState == ELevelSolveEndState.SearchLimitReached ||
            report.EndState == ELevelSolveEndState.Cancelled)
        {
            data.Classification = "SearchLimitReached";
            return;
        }

        data.Classification = "NoSolutionFoundByCurrentSolver";
    }

    private static AuditReportData CreateReportData(List<LevelAuditData> levelAuditDataList)
    {
        AuditReportData reportData = new AuditReportData();
        reportData.GeneratedAtUtc = DateTime.UtcNow.ToString("O");
        reportData.TotalLevelCount = levelAuditDataList.Count;
        reportData.LevelAuditDataArray = levelAuditDataList.ToArray();
        reportData.SummaryDataArray = CreateSummaryDataArray(levelAuditDataList);
        return reportData;
    }

    private static AuditSummaryData[] CreateSummaryDataArray(List<LevelAuditData> levelAuditDataList)
    {
        Dictionary<string, AuditSummaryData> summaryDataByGroupNameDict = new Dictionary<string, AuditSummaryData>();

        for (int i = 0; i < levelAuditDataList.Count; i++)
        {
            LevelAuditData levelAuditData = levelAuditDataList[i];

            if (!summaryDataByGroupNameDict.TryGetValue(levelAuditData.GroupName, out AuditSummaryData summaryData))
            {
                summaryData = new AuditSummaryData();
                summaryData.GroupName = levelAuditData.GroupName;
                summaryDataByGroupNameDict.Add(levelAuditData.GroupName, summaryData);
            }

            summaryData.TotalCount++;
            IncrementSummaryCount(summaryData, levelAuditData.Classification);
        }

        List<AuditSummaryData> summaryDataList = new List<AuditSummaryData>(summaryDataByGroupNameDict.Values);
        summaryDataList.Sort((left, right) => string.CompareOrdinal(left.GroupName, right.GroupName));
        return summaryDataList.ToArray();
    }

    private static void IncrementSummaryCount(AuditSummaryData summaryData, string classification)
    {
        switch (classification)
        {
            case "OptimalSolutionProven":
                summaryData.OptimalSolutionProvenCount++;
                break;
            case "SolutionFoundUnproven":
                summaryData.SolutionFoundUnprovenCount++;
                break;
            case "NoSolutionFoundByCurrentSolver":
                summaryData.NoSolutionFoundByCurrentSolverCount++;
                break;
            case "SearchLimitReached":
                summaryData.SearchLimitReachedCount++;
                break;
            case "DefinitionValidationFailed":
                summaryData.DefinitionValidationFailedCount++;
                break;
            default:
                summaryData.AuditErrorCount++;
                break;
        }
    }

    private static void WriteReportFiles(AuditReportData reportData)
    {
        string projectDirectoryPath = Directory.GetParent(Application.dataPath).FullName;
        string markdownReportPath = Path.Combine(projectDirectoryPath, MarkdownReportRelativePath);
        string jsonReportPath = Path.Combine(projectDirectoryPath, JsonReportRelativePath);
        string markdown = CreateMarkdownReport(reportData);
        string json = JsonUtility.ToJson(reportData, true);
        UTF8Encoding utf8Encoding = new UTF8Encoding(false);

        File.WriteAllText(markdownReportPath, markdown, utf8Encoding);
        File.WriteAllText(jsonReportPath, json, utf8Encoding);
    }

    private static string CreateMarkdownReport(AuditReportData reportData)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("# Migrated Level Solver Audit");
        builder.AppendLine();
        builder.AppendLine($"- Generated (UTC): {reportData.GeneratedAtUtc}");
        builder.AppendLine($"- Total levels: {reportData.TotalLevelCount}");
        builder.AppendLine("- Each level is evaluated once with the same search limits as the Level Editor's optimal-solution action.");
        builder.AppendLine("- Maximum rounds use max(30, saved one-star round count); search nodes and evaluated candidates are both limited to 2,000,000.");
        builder.AppendLine("- `NoSolutionFoundByCurrentSolver` means no success candidate was found by the current Solver. It is not a mathematical proof that the level is impossible.");
        builder.AppendLine();
        builder.AppendLine("## Summary");
        builder.AppendLine();
        builder.AppendLine("| Group | Total | Optimal proven | Solution found, unproven | No solution found by current Solver | Search limit reached | Validation failed | Audit error |");
        builder.AppendLine("| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |");

        for (int i = 0; i < reportData.SummaryDataArray.Length; i++)
        {
            AuditSummaryData summaryData = reportData.SummaryDataArray[i];
            builder.AppendLine($"| {summaryData.GroupName} | {summaryData.TotalCount} | {summaryData.OptimalSolutionProvenCount} | {summaryData.SolutionFoundUnprovenCount} | {summaryData.NoSolutionFoundByCurrentSolverCount} | {summaryData.SearchLimitReachedCount} | {summaryData.DefinitionValidationFailedCount} | {summaryData.AuditErrorCount} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Level Results");
        builder.AppendLine();
        builder.AppendLine("| Level | Classification | Clear rounds | Minimum rounds | Solver state | Nodes / candidates |");
        builder.AppendLine("| --- | --- | ---: | ---: | --- | ---: |");

        for (int i = 0; i < reportData.LevelAuditDataArray.Length; i++)
        {
            LevelAuditData data = reportData.LevelAuditDataArray[i];
            string evaluationCount = data.WasRecoveredFromPreviousBatchLog
                ? "n/a (recovered)"
                : $"{data.ExploredNodeCount:N0} / {data.EvaluatedCandidateCount:N0}";
            builder.AppendLine($"| {data.LevelName} | {data.Classification} | {data.ClearRoundCount} | {data.MinimumPossibleRoundCount} | {data.SolverEndState} | {evaluationCount} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Notes");
        builder.AppendLine();

        for (int i = 0; i < reportData.LevelAuditDataArray.Length; i++)
        {
            LevelAuditData data = reportData.LevelAuditDataArray[i];
            builder.AppendLine($"- `{data.LevelName}` ({data.Classification}): {EscapeMarkdownText(data.FinalMessage)}");
        }

        return builder.ToString();
    }

    private static string EscapeMarkdownText(string value)
    {
        return (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Replace("|", "\\|");
    }

    private static string GetGroupName(string levelName)
    {
        if (levelName.StartsWith("Level_Tutorial_", StringComparison.Ordinal))
        {
            return "Tutorial";
        }

        if (levelName.Length >= 9 && levelName.StartsWith("Level_C", StringComparison.Ordinal))
        {
            return levelName.Substring(6, 3);
        }

        return "Other";
    }

    [Serializable]
    private sealed class AuditReportData
    {
        public string GeneratedAtUtc;
        public int TotalLevelCount;
        public AuditSummaryData[] SummaryDataArray;
        public LevelAuditData[] LevelAuditDataArray;
    }

    [Serializable]
    private sealed class AuditSummaryData
    {
        public string GroupName;
        public int TotalCount;
        public int OptimalSolutionProvenCount;
        public int SolutionFoundUnprovenCount;
        public int NoSolutionFoundByCurrentSolverCount;
        public int SearchLimitReachedCount;
        public int DefinitionValidationFailedCount;
        public int AuditErrorCount;
    }

    [Serializable]
    private sealed class LevelAuditData
    {
        public string AssetPath;
        public string LevelName;
        public string GroupName;
        public string Classification;
        public string SolverEndState;
        public string FinalMessage;
        public int ClearRoundCount;
        public int MinimumPossibleRoundCount;
        public int FoundSolutionCount;
        public int ExploredNodeCount;
        public int EvaluatedCandidateCount;
        public bool WasRecoveredFromPreviousBatchLog;
    }
}
