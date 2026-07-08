using UnityEditor;
using UnityEngine;

public sealed class LegacyLevelMigrationWindow : EditorWindow
{
    private const string DefaultSourceAssetFolder = "Assets/Outdated/Levels";
    private const string DefaultOutputAssetFolder = "Assets/02.Scripts/02.Repository/Levels/Migrated";

    private string _sourceAssetFolder = DefaultSourceAssetFolder;
    private string _outputAssetFolder = DefaultOutputAssetFolder;
    private string _reportText = "No migration has been run in this window.";
    private Vector2 _scrollPosition;

    [MenuItem("Tools/DeadLock/Levels/Migrate Legacy Levels")]
    public static void Open()
    {
        LegacyLevelMigrationWindow window = GetWindow<LegacyLevelMigrationWindow>("Legacy Level Migration");
        window.minSize = new Vector2(520f, 420f);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Legacy Level Migration", EditorStyles.boldLabel);
        EditorGUILayout.Space(6f);

        _sourceAssetFolder = EditorGUILayout.TextField("Source Folder", _sourceAssetFolder);
        _outputAssetFolder = EditorGUILayout.TextField("Output Folder", _outputAssetFolder);

        EditorGUILayout.Space(8f);

        if (GUILayout.Button("Migrate Legacy Levels", GUILayout.Height(32f)))
        {
            RunMigration();
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Report", EditorStyles.boldLabel);
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
        EditorGUILayout.TextArea(_reportText, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    private void RunMigration()
    {
        LegacyLevelMigrationService service = new LegacyLevelMigrationService();
        LegacyLevelMigrationReport report = service.Migrate(_sourceAssetFolder, _outputAssetFolder);
        _reportText = report.ToText();
    }
}
