using System;
using System.IO;
using UnityEditor;
using UnityEngine;

internal sealed class LevelGenerationCandidateImportService
{
    private readonly LevelGenerationCandidateLevelSOWriter _writer = new LevelGenerationCandidateLevelSOWriter();
    private readonly LevelSOMapper _mapper = new LevelSOMapper();
    private readonly LevelDefinitionValidator _validator = new LevelDefinitionValidator();
    private readonly LevelSolutionFinder _solutionFinder = new LevelSolutionFinder();
    private readonly LevelDifficultyAnalyzer _difficultyAnalyzer = new LevelDifficultyAnalyzer();

    public LevelGenerationCandidateImportReport Import(string jsonFilePath, string outputAssetFolder)
    {
        LevelGenerationCandidateImportReport report = new LevelGenerationCandidateImportReport();
        report.SetInputPath(jsonFilePath);

        try
        {
            LevelGenerationCandidateData candidateData = LoadCandidateData(jsonFilePath);
            LevelSO tempLevelSO = ScriptableObject.CreateInstance<LevelSO>();
            tempLevelSO.hideFlags = HideFlags.HideAndDontSave;

            try
            {
                _writer.Write(tempLevelSO, candidateData, report);

                if (!Validate(tempLevelSO, report))
                {
                    return report;
                }

                LevelSolveReport solveReport = Solve(tempLevelSO, report);

                if (solveReport.BestCandidate is null)
                {
                    report.AddError("성공 가능한 해를 찾지 못해 LevelSO를 저장하지 않았습니다.");
                    return report;
                }

                AnalyzeDifficulty(tempLevelSO, solveReport, report);
                CreateAsset(candidateData, outputAssetFolder, report);
                return report;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tempLevelSO);
            }
        }
        catch (Exception exception)
        {
            report.AddError(exception.Message);
            return report;
        }
    }

    public void WriteReport(LevelGenerationCandidateImportReport report, string outputReportFolder)
    {
        if (report == null)
        {
            return;
        }

        EnsureAssetFolder(outputReportFolder);

        string reportName = GetSafeReportName(report);
        string reportAssetPath = AssetDatabase.GenerateUniqueAssetPath($"{outputReportFolder}/{reportName}.txt");
        report.SetReportAssetPath(reportAssetPath);
        File.WriteAllText(reportAssetPath, report.ToText());
        AssetDatabase.ImportAsset(reportAssetPath);
    }

    private LevelGenerationCandidateData LoadCandidateData(string jsonFilePath)
    {
        if (string.IsNullOrWhiteSpace(jsonFilePath))
        {
            throw new InvalidOperationException("JSON 파일 경로가 비어 있습니다.");
        }

        if (!File.Exists(jsonFilePath))
        {
            throw new FileNotFoundException("JSON 파일을 찾을 수 없습니다.", jsonFilePath);
        }

        string json = File.ReadAllText(jsonFilePath);
        LevelGenerationCandidateData candidateData = JsonUtility.FromJson<LevelGenerationCandidateData>(json);

        if (candidateData == null)
        {
            throw new InvalidOperationException("LevelGenerationCandidate JSON을 읽을 수 없습니다.");
        }

        return candidateData;
    }

    private bool Validate(LevelSO levelSO, LevelGenerationCandidateImportReport report)
    {
        LevelDefinition definition = _mapper.ToLevelDefinition(levelSO);
        LevelValidationResult validationResult = _validator.Validate(definition);

        if (validationResult.IsValid)
        {
            report.AddInfo("LevelDefinition validation succeeded.");
            return true;
        }

        for (int i = 0; i < validationResult.ErrorList.Length; i++)
        {
            report.AddError($"Validation: {validationResult.ErrorList[i].Message}");
        }

        return false;
    }

    private LevelSolveReport Solve(LevelSO levelSO, LevelGenerationCandidateImportReport report)
    {
        LevelSolveReport solveReport = _solutionFinder.FindBestSolution(levelSO, LevelSolveSettings.CreateDefault());
        report.AddInfo($"Solver: {solveReport.EndState} / {solveReport.Message}");
        report.AddInfo($"Solver stats: explored {solveReport.ExploredNodeCount:N0}, evaluated {solveReport.EvaluatedCandidateCount:N0}, succeeded {solveReport.FoundSolutionCount:N0}");

        if (solveReport.BestCandidate is not null)
        {
            report.AddInfo($"Best clear round: {solveReport.BestCandidate.ClearRoundCount}");
            AddStarThresholdInfo(solveReport, report);
        }

        if (solveReport.BestCandidate is not null && !solveReport.IsOptimalProven)
        {
            report.AddWarning("성공 해는 찾았지만 최적 해임은 증명되지 않았습니다.");
        }

        return solveReport;
    }

    private void AnalyzeDifficulty(
        LevelSO levelSO,
        LevelSolveReport solveReport,
        LevelGenerationCandidateImportReport report)
    {
        LevelDifficultyReport difficultyReport = _difficultyAnalyzer.Analyze(levelSO, solveReport);

        if (!difficultyReport.IsAvailable)
        {
            report.AddWarning($"Difficulty unavailable: {difficultyReport.UnavailableReason}");
            return;
        }

        report.AddInfo($"Difficulty: {difficultyReport.Score:0.0} / 5.0 ({difficultyReport.GradeText})");

        for (int i = 0; i < difficultyReport.ReasonList.Length; i++)
        {
            report.AddInfo($"Difficulty reason: {difficultyReport.ReasonList[i]}");
        }
    }

    private void CreateAsset(
        LevelGenerationCandidateData candidateData,
        string outputAssetFolder,
        LevelGenerationCandidateImportReport report)
    {
        EnsureAssetFolder(outputAssetFolder);

        LevelSO levelSO = ScriptableObject.CreateInstance<LevelSO>();
        _writer.Write(levelSO, candidateData, null);

        string assetName = GetSafeAssetName(candidateData);
        string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{outputAssetFolder}/{assetName}.asset");
        AssetDatabase.CreateAsset(levelSO, assetPath);
        EditorUtility.SetDirty(levelSO);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        report.SetCreatedAssetPath(assetPath);
        report.AddInfo($"Saved asset: {assetPath}");
    }

    private void AddStarThresholdInfo(LevelSolveReport solveReport, LevelGenerationCandidateImportReport report)
    {
        LevelStarThresholdRecommendation recommendation = solveReport.StarThresholdRecommendation;

        if (recommendation is null)
        {
            return;
        }

        report.AddInfo($"Star thresholds: 3★ {recommendation.ThreeStarRoundCount}R, 2★ {recommendation.TwoStarRoundCount}R, 1★ {recommendation.OneStarRoundCount}R");
        report.AddInfo($"Star threshold basis: {recommendation.Reason}");
    }

    private void EnsureAssetFolder(string assetFolder)
    {
        if (AssetDatabase.IsValidFolder(assetFolder))
        {
            return;
        }

        string[] pathPartArray = assetFolder.Split('/');
        string currentPath = pathPartArray[0];

        for (int i = 1; i < pathPartArray.Length; i++)
        {
            string nextPath = currentPath + "/" + pathPartArray[i];

            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(currentPath, pathPartArray[i]);
            }

            currentPath = nextPath;
        }
    }

    private string GetSafeAssetName(LevelGenerationCandidateData candidateData)
    {
        string rawName = string.IsNullOrWhiteSpace(candidateData.name) ?
            $"Generated_{candidateData.id}" :
            candidateData.name;
        char[] invalidCharArray = Path.GetInvalidFileNameChars();
        char[] resultArray = rawName.Trim().ToCharArray();

        for (int i = 0; i < resultArray.Length; i++)
        {
            if (char.IsWhiteSpace(resultArray[i]) || IsInvalidFileNameChar(resultArray[i], invalidCharArray))
            {
                resultArray[i] = '_';
            }
        }

        string result = new string(resultArray);
        return string.IsNullOrWhiteSpace(result) ? $"Generated_{candidateData.id}" : result;
    }

    private string GetSafeReportName(LevelGenerationCandidateImportReport report)
    {
        string sourcePath = report.InputPath;

        if (string.IsNullOrEmpty(sourcePath))
        {
            sourcePath = "GeneratedCandidateImport";
        }

        string fileName = Path.GetFileNameWithoutExtension(sourcePath);
        return GetSafeFileName($"{fileName}_ImportReport");
    }

    private string GetSafeFileName(string rawName)
    {
        char[] invalidCharArray = Path.GetInvalidFileNameChars();
        char[] resultArray = rawName.Trim().ToCharArray();

        for (int i = 0; i < resultArray.Length; i++)
        {
            if (char.IsWhiteSpace(resultArray[i]) || IsInvalidFileNameChar(resultArray[i], invalidCharArray))
            {
                resultArray[i] = '_';
            }
        }

        string result = new string(resultArray);
        return string.IsNullOrWhiteSpace(result) ? "GeneratedCandidate" : result;
    }

    private bool IsInvalidFileNameChar(char value, char[] invalidCharArray)
    {
        for (int i = 0; i < invalidCharArray.Length; i++)
        {
            if (value == invalidCharArray[i])
            {
                return true;
            }
        }

        return false;
    }
}
