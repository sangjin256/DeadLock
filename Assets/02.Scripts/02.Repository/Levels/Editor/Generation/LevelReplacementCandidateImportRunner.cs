using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class LevelReplacementCandidateImportRunner
{
    private const string CandidateRelativeDirectory = "Assets/02.Scripts/02.Repository/Levels/GeneratedCandidates/Replacement";
    private const string OutputAssetDirectory = "Assets/02.Scripts/02.Repository/Levels/Generated";
    private const string ReportAssetDirectory = "Assets/02.Scripts/02.Repository/Levels/GeneratedCandidates/ImportReports";
    private const string MigratedAssetDirectory = "Assets/02.Scripts/02.Repository/Levels/Migrated";

    [MenuItem("Tools/DeadLock/Levels/Import Replacement Candidates")]
    public static void ImportAll()
    {
        string candidateDirectoryPath = Path.GetFullPath(CandidateRelativeDirectory);

        if (!Directory.Exists(candidateDirectoryPath))
        {
            throw new DirectoryNotFoundException($"Replacement candidate directory was not found: {candidateDirectoryPath}");
        }

        string[] candidatePathArray = Directory.GetFiles(candidateDirectoryPath, "*.json");
        Array.Sort(candidatePathArray, StringComparer.Ordinal);

        if (candidatePathArray.Length == 0)
        {
            Debug.Log("[LevelReplacementImport] No replacement candidate JSON files were found.");
            return;
        }

        LevelGenerationCandidateImportService service = new LevelGenerationCandidateImportService();

        for (int i = 0; i < candidatePathArray.Length; i++)
        {
            if (HasImportedAsset(candidatePathArray[i]))
            {
                Debug.Log($"[LevelReplacementImport] Skipped existing candidate: {Path.GetFileName(candidatePathArray[i])}");
                continue;
            }

            LevelGenerationCandidateImportReport report = service.Import(candidatePathArray[i], OutputAssetDirectory);
            service.WriteReport(report, ReportAssetDirectory);
            Debug.Log($"[LevelReplacementImport] {Path.GetFileName(candidatePathArray[i])}\n{report.ToText()}");
        }
    }

    public static void ImportAllFromBatchMode()
    {
        try
        {
            ImportAll();
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    private static bool HasImportedAsset(string candidatePath)
    {
        string json = File.ReadAllText(candidatePath);
        LevelGenerationCandidateData candidateData = JsonUtility.FromJson<LevelGenerationCandidateData>(json);

        if (candidateData == null || string.IsNullOrWhiteSpace(candidateData.name))
        {
            return false;
        }

        string generatedAssetPath = $"{OutputAssetDirectory}/{candidateData.name}.asset";
        string migratedAssetPath = $"{MigratedAssetDirectory}/{candidateData.name}.asset";
        return AssetDatabase.LoadAssetAtPath<LevelSO>(generatedAssetPath) is not null ||
               AssetDatabase.LoadAssetAtPath<LevelSO>(migratedAssetPath) is not null;
    }
}
