using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class VisualSettingsDefaultAssetGenerator
{
    private const string MenuPath = "Tools/DeadLock/Visuals/Create Default Visual Settings";
    private const string LegacyReportPath = "Assets/02.Scripts/02.Repository/Levels/Migrated/LegacyLevelMigrationReport.txt";
    private const string SettingsFolderPath = "Assets/05.Visual Resources/Settings";
    private const string DefaultAssetPath = SettingsFolderPath + "/VisualSettings_Default.asset";

    private static readonly Regex ColorLineRegex = new Regex(
        @"^ColorId\s+(?<id>\d+):\s+(?<hex>#[0-9A-Fa-f]{8})$",
        RegexOptions.Compiled);

    [MenuItem(MenuPath)]
    public static void CreateDefaultVisualSettings()
    {
        VisualSettingsSO existingSettings = AssetDatabase.LoadAssetAtPath<VisualSettingsSO>(DefaultAssetPath);

        if (existingSettings != null)
        {
            FocusAsset(existingSettings, "[VisualSettings] The default asset already exists.");
            return;
        }

        List<LegacyColorEntry> colorEntryList = LoadLegacyColorEntryList();

        if (colorEntryList.Count == 0)
        {
            Debug.LogError("[VisualSettings] No valid ColorId entries were found in the legacy migration report.");
            return;
        }

        EnsureSettingsFolder();

        VisualSettingsSO visualSettings = ScriptableObject.CreateInstance<VisualSettingsSO>();
        PopulateColorEntryList(visualSettings, colorEntryList);
        AssetDatabase.CreateAsset(visualSettings, DefaultAssetPath);
        EditorUtility.SetDirty(visualSettings);
        AssetDatabase.SaveAssets();
        FocusAsset(visualSettings, "[VisualSettings] Created the default asset with " + colorEntryList.Count + " palette entries.");
    }

    private static List<LegacyColorEntry> LoadLegacyColorEntryList()
    {
        List<LegacyColorEntry> colorEntryList = new List<LegacyColorEntry>();

        if (!File.Exists(LegacyReportPath))
        {
            Debug.LogError("[VisualSettings] The legacy migration report was not found: " + LegacyReportPath);
            return colorEntryList;
        }

        string[] lineArray = File.ReadAllLines(LegacyReportPath);
        HashSet<int> colorIdSet = new HashSet<int>();

        for (int i = 0; i < lineArray.Length; i++)
        {
            Match match = ColorLineRegex.Match(lineArray[i].Trim());

            if (!match.Success)
            {
                continue;
            }

            int colorId = int.Parse(match.Groups["id"].Value);

            if (!colorIdSet.Add(colorId) || !ColorUtility.TryParseHtmlString(match.Groups["hex"].Value, out Color color))
            {
                continue;
            }

            colorEntryList.Add(new LegacyColorEntry(colorId, color));
        }

        colorEntryList.Sort((left, right) => left.ColorId.CompareTo(right.ColorId));
        return colorEntryList;
    }

    private static void EnsureSettingsFolder()
    {
        if (!AssetDatabase.IsValidFolder(SettingsFolderPath))
        {
            AssetDatabase.CreateFolder("Assets/05.Visual Resources", "Settings");
        }
    }

    private static void PopulateColorEntryList(VisualSettingsSO visualSettings, IReadOnlyList<LegacyColorEntry> colorEntryList)
    {
        SerializedObject serializedObject = new SerializedObject(visualSettings);
        SerializedProperty colorEntryListProperty = serializedObject.FindProperty("_colorEntryList");
        colorEntryListProperty.ClearArray();

        for (int i = 0; i < colorEntryList.Count; i++)
        {
            colorEntryListProperty.InsertArrayElementAtIndex(i);
            SerializedProperty colorEntryProperty = colorEntryListProperty.GetArrayElementAtIndex(i);
            colorEntryProperty.FindPropertyRelative("_colorId").intValue = colorEntryList[i].ColorId;
            colorEntryProperty.FindPropertyRelative("_color").colorValue = colorEntryList[i].Color;
        }

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void FocusAsset(Object asset, string message)
    {
        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);
        Debug.Log(message, asset);
    }

    private readonly struct LegacyColorEntry
    {
        public readonly int ColorId;
        public readonly Color Color;

        public LegacyColorEntry(int colorId, Color color)
        {
            ColorId = colorId;
            Color = color;
        }
    }
}
