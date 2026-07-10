using System.IO;
using UnityEditor;
using UnityEngine;

public static class LevelGenerationCandidateImportMenu
{
    internal const string DefaultOutputAssetFolder = "Assets/02.Scripts/02.Repository/Levels/Generated";
    internal const string DefaultCandidateFolder = "Assets/02.Scripts/02.Repository/Levels/GeneratedCandidates";
    internal const string DefaultReportFolder = "Assets/02.Scripts/02.Repository/Levels/GeneratedCandidates/ImportReports";

    internal static string GetDefaultCandidateFolderPath()
    {
        return Path.GetFullPath(DefaultCandidateFolder);
    }

    [MenuItem("Tools/DeadLock/Levels/Import Generated Candidate")]
    public static void Import()
    {
        string jsonFilePath = EditorUtility.OpenFilePanel("Import Generated Candidate", GetDefaultCandidateFolderPath(), "json");

        if (string.IsNullOrEmpty(jsonFilePath))
        {
            return;
        }

        LevelGenerationCandidateImportService service = new LevelGenerationCandidateImportService();
        LevelGenerationCandidateImportReport report = service.Import(jsonFilePath, DefaultOutputAssetFolder);
        service.WriteReport(report, DefaultReportFolder);
        Debug.Log(report.ToText());

        if (report.IsSaved)
        {
            LevelSO levelSO = AssetDatabase.LoadAssetAtPath<LevelSO>(report.CreatedAssetPath);
            Selection.activeObject = levelSO;
            EditorGUIUtility.PingObject(levelSO);
        }

        EditorUtility.DisplayDialog("Generated Candidate Import", report.GetDialogMessage(), "OK");
    }
}
