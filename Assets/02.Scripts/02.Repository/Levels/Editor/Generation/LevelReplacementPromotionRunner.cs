using System;
using UnityEditor;
using UnityEngine;

public static class LevelReplacementPromotionRunner
{
    private const string GeneratedAssetDirectory = "Assets/02.Scripts/02.Repository/Levels/Generated";
    private const string MigratedAssetDirectory = "Assets/02.Scripts/02.Repository/Levels/Migrated";
    private const string AutoOptimalTestCaseName = "Auto Optimal";

    private static readonly ReplacementSpec[] ReplacementSpecArray =
    {
        new ReplacementSpec("Level_C03_S08", 3.0f, 3.4f),
        new ReplacementSpec("Level_C03_S09", 3.3f, 3.8f),
        new ReplacementSpec("Level_C04_S09", 3.1f, 3.5f),
        new ReplacementSpec("Level_C04_S10", 3.4f, 3.9f),
        new ReplacementSpec("Level_C05_S02", 1.4f, 1.7f),
        new ReplacementSpec("Level_C05_S03", 1.5f, 1.9f),
        new ReplacementSpec("Level_C05_S04", 2.3f, 2.5f),
        new ReplacementSpec("Level_C05_S05", 2.7f, 3.0f),
        new ReplacementSpec("Level_C05_S06", 2.8f, 3.0f),
        new ReplacementSpec("Level_C05_S07", 3.1f, 3.3f),
        new ReplacementSpec("Level_C05_S08", 3.4f, 3.6f),
        new ReplacementSpec("Level_C05_S09", 3.6f, 3.8f),
        new ReplacementSpec("Level_C05_S10", 3.8f, 3.9f),
    };

    [MenuItem("Tools/DeadLock/Levels/Promote Verified Replacement Candidates")]
    public static void PromoteAvailableCandidates()
    {
        LevelSolutionFinder solutionFinder = new LevelSolutionFinder();
        LevelDifficultyAnalyzer difficultyAnalyzer = new LevelDifficultyAnalyzer();

        for (int i = 0; i < ReplacementSpecArray.Length; i++)
        {
            PromoteCandidate(ReplacementSpecArray[i], solutionFinder, difficultyAnalyzer);
        }
    }

    public static void PromoteAvailableCandidatesFromBatchMode()
    {
        try
        {
            PromoteAvailableCandidates();
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    private static void PromoteCandidate(ReplacementSpec spec,
                                         LevelSolutionFinder solutionFinder,
                                         LevelDifficultyAnalyzer difficultyAnalyzer)
    {
        string sourceAssetPath = $"{GeneratedAssetDirectory}/{spec.LevelName}.asset";
        string destinationAssetPath = $"{MigratedAssetDirectory}/{spec.LevelName}.asset";
        LevelSO levelSO = AssetDatabase.LoadAssetAtPath<LevelSO>(sourceAssetPath);

        if (levelSO == null || AssetDatabase.LoadAssetAtPath<LevelSO>(destinationAssetPath) != null)
        {
            return;
        }

        LevelSolveReport solveReport = solutionFinder.FindBestSolution(levelSO, CreateEditorEquivalentSettings(levelSO));

        if (solveReport.BestCandidate == null)
        {
            Debug.LogWarning($"[LevelReplacementPromotion] {spec.LevelName} was not promoted because no successful solution was found.");
            return;
        }

        LevelDifficultyReport difficultyReport = difficultyAnalyzer.Analyze(levelSO, solveReport);

        if (!difficultyReport.IsAvailable ||
            difficultyReport.Score < spec.MinimumDifficultyScore ||
            difficultyReport.Score > spec.MaximumDifficultyScore ||
            difficultyReport.Score >= 4.0f)
        {
            Debug.LogWarning($"[LevelReplacementPromotion] {spec.LevelName} was not promoted because difficulty {difficultyReport.Score:0.0} is outside {spec.MinimumDifficultyScore:0.0}-{spec.MaximumDifficultyScore:0.0}.");
            return;
        }

        ApplySolveMetadata(levelSO, solveReport);
        string moveError = AssetDatabase.MoveAsset(sourceAssetPath, destinationAssetPath);

        if (!string.IsNullOrEmpty(moveError))
        {
            throw new InvalidOperationException($"Failed to promote {spec.LevelName}: {moveError}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[LevelReplacementPromotion] Promoted {spec.LevelName}: {difficultyReport.Score:0.0} / 5.0, {solveReport.BestCandidate.ClearRoundCount} rounds.");
    }

    private static LevelSolveSettings CreateEditorEquivalentSettings(LevelSO levelSO)
    {
        int maxRoundCount = LevelSolveSettings.DefaultMaxRoundCount;

        if (levelSO.StarThresholdData != null)
        {
            maxRoundCount = Math.Max(maxRoundCount, levelSO.StarThresholdData.OneStarRoundCount);
        }

        return new LevelSolveSettings(maxRoundCount,
                                      LevelSolveSettings.DefaultMaxSearchNodeCount,
                                      LevelSolveSettings.DefaultMaxEvaluatedCandidateCount,
                                      false);
    }

    private static void ApplySolveMetadata(LevelSO levelSO, LevelSolveReport solveReport)
    {
        SerializedObject serializedObject = new SerializedObject(levelSO);
        serializedObject.Update();
        WriteStarThreshold(serializedObject.FindProperty("_starThresholdData"), solveReport.StarThresholdRecommendation);
        WriteAutoOptimalTestCase(serializedObject.FindProperty("_testCaseDataList"),
                                 solveReport.BestCandidate,
                                 solveReport.StarThresholdRecommendation);
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(levelSO);
    }

    private static void WriteStarThreshold(SerializedProperty starThresholdProperty,
                                           LevelStarThresholdRecommendation recommendation)
    {
        if (starThresholdProperty == null || recommendation == null)
        {
            return;
        }

        starThresholdProperty.FindPropertyRelative("_threeStarRoundCount").intValue = recommendation.ThreeStarRoundCount;
        starThresholdProperty.FindPropertyRelative("_twoStarRoundCount").intValue = recommendation.TwoStarRoundCount;
        starThresholdProperty.FindPropertyRelative("_oneStarRoundCount").intValue = recommendation.OneStarRoundCount;
    }

    private static void WriteAutoOptimalTestCase(SerializedProperty testCaseListProperty,
                                                 LevelSolveCandidate candidate,
                                                 LevelStarThresholdRecommendation recommendation)
    {
        if (testCaseListProperty == null || candidate == null || recommendation == null)
        {
            return;
        }

        testCaseListProperty.ClearArray();
        testCaseListProperty.InsertArrayElementAtIndex(0);
        SerializedProperty testCaseProperty = testCaseListProperty.GetArrayElementAtIndex(0);
        testCaseProperty.FindPropertyRelative("_name").stringValue = AutoOptimalTestCaseName;
        testCaseProperty.FindPropertyRelative("_maxRoundCount").intValue = recommendation.OneStarRoundCount;
        testCaseProperty.FindPropertyRelative("_expectedEndState").enumValueIndex = (int)ESimulationEndState.Succeeded;

        SerializedProperty assignmentListProperty = testCaseProperty.FindPropertyRelative("_assignedConnectionDataList");
        assignmentListProperty.ClearArray();

        for (int i = 0; i < candidate.AssignmentArray.Length; i++)
        {
            LevelSolveAssignment assignment = candidate.AssignmentArray[i];
            assignmentListProperty.InsertArrayElementAtIndex(i);
            SerializedProperty assignmentProperty = assignmentListProperty.GetArrayElementAtIndex(i);
            assignmentProperty.FindPropertyRelative("_processId").intValue = assignment.ProcessId;
            assignmentProperty.FindPropertyRelative("_slotId").intValue = assignment.SlotId;
            assignmentProperty.FindPropertyRelative("_resourceId").intValue = assignment.ResourceId;
        }
    }

    private sealed class ReplacementSpec
    {
        public readonly string LevelName;
        public readonly float MinimumDifficultyScore;
        public readonly float MaximumDifficultyScore;

        public ReplacementSpec(string levelName, float minimumDifficultyScore, float maximumDifficultyScore)
        {
            LevelName = levelName;
            MinimumDifficultyScore = minimumDifficultyScore;
            MaximumDifficultyScore = maximumDifficultyScore;
        }
    }
}
