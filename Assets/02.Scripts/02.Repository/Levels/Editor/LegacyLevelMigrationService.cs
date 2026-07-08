using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

internal sealed class LegacyLevelMigrationService
{
    private const int ProcessNodeType = 1;
    private const int ResourceNodeType = 2;
    private const int UnknownLevelIdStart = 9000;
    private const string ReportFileName = "LegacyLevelMigrationReport.txt";

    private static readonly Regex ColorRegex = new Regex(
        @"\{r:\s*(?<r>[^,]+),\s*g:\s*(?<g>[^,]+),\s*b:\s*(?<b>[^,]+),\s*a:\s*(?<a>[^}]+)\}",
        RegexOptions.Compiled);

    private readonly Dictionary<LegacyColorKey, int> _colorIdByKeyDict = new();
    private readonly HashSet<int> _usedLevelIdSet = new();
    private int _nextUnknownLevelId = UnknownLevelIdStart;

    public LegacyLevelMigrationReport Migrate(string sourceAssetFolder, string outputAssetFolder)
    {
        _colorIdByKeyDict.Clear();
        _usedLevelIdSet.Clear();
        _nextUnknownLevelId = UnknownLevelIdStart;

        LegacyLevelMigrationReport report = new LegacyLevelMigrationReport();
        List<LegacyLevelAssetData> levelDataList = LoadLegacyLevels(sourceAssetFolder, report);

        BuildColorMapping(levelDataList, report);
        EnsureAssetFolder(outputAssetFolder);

        LevelSOMapper mapper = new LevelSOMapper();
        LevelDefinitionValidator validator = new LevelDefinitionValidator();

        for (int i = 0; i < levelDataList.Count; i++)
        {
            TryCreateLevelAsset(levelDataList[i], outputAssetFolder, mapper, validator, report);
        }

        WriteReportAsset(outputAssetFolder, report);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return report;
    }

    private List<LegacyLevelAssetData> LoadLegacyLevels(string sourceAssetFolder, LegacyLevelMigrationReport report)
    {
        List<LegacyLevelAssetData> levelDataList = new List<LegacyLevelAssetData>();

        if (!Directory.Exists(sourceAssetFolder))
        {
            report.AddError($"Source folder does not exist: {sourceAssetFolder}");
            return levelDataList;
        }

        string[] filePathArray = Directory.GetFiles(sourceAssetFolder, "*.asset");
        Array.Sort(filePathArray, StringComparer.OrdinalIgnoreCase);
        report.SetSourceAssetCount(filePathArray.Length);

        for (int i = 0; i < filePathArray.Length; i++)
        {
            try
            {
                levelDataList.Add(ParseLegacyLevel(filePathArray[i]));
            }
            catch (Exception exception)
            {
                report.AddFailedAsset();
                report.AddError($"Failed to parse {filePathArray[i]}: {exception.Message}");
            }
        }

        return levelDataList;
    }

    private LegacyLevelAssetData ParseLegacyLevel(string filePath)
    {
        string[] lineArray = File.ReadAllLines(filePath);
        LegacyLevelAssetData levelData = new LegacyLevelAssetData();
        levelData.SetName(Path.GetFileNameWithoutExtension(filePath));

        LegacyLevelNodeData currentNodeData = null;

        for (int i = 0; i < lineArray.Length; i++)
        {
            string line = lineArray[i];
            string trimmedLine = line.Trim();

            if (trimmedLine.StartsWith("row:", StringComparison.Ordinal))
            {
                levelData.SetRowCount(ParseIntValue(trimmedLine));
                continue;
            }

            if (trimmedLine.StartsWith("col:", StringComparison.Ordinal))
            {
                levelData.SetColumnCount(ParseIntValue(trimmedLine));
                continue;
            }

            if (line.StartsWith("  - nodeTypes:", StringComparison.Ordinal))
            {
                currentNodeData = new LegacyLevelNodeData();
                currentNodeData.Init(levelData.NodeDataList.Count, ParseIntValue(trimmedLine));

                levelData.AddNode(currentNodeData);
                continue;
            }

            if (currentNodeData is null)
            {
                continue;
            }

            ParseNodeLine(currentNodeData, trimmedLine);
        }

        return levelData;
    }

    private void ParseNodeLine(LegacyLevelNodeData nodeData, string trimmedLine)
    {
        Match colorMatch = ColorRegex.Match(trimmedLine);

        if (colorMatch.Success)
        {
            nodeData.AddColor(new Color(ParseFloat(colorMatch.Groups["r"].Value),
                                        ParseFloat(colorMatch.Groups["g"].Value),
                                        ParseFloat(colorMatch.Groups["b"].Value),
                                        ParseFloat(colorMatch.Groups["a"].Value)));
            return;
        }

        if (trimmedLine.StartsWith("maxCount:", StringComparison.Ordinal))
        {
            nodeData.SetMaxCount(ParseIntValue(trimmedLine));
            return;
        }

        if (trimmedLine.StartsWith("fixedNum:", StringComparison.Ordinal))
        {
            nodeData.SetFixedNum(ParseIntValue(trimmedLine));
            return;
        }

        if (trimmedLine.StartsWith("isSimul:", StringComparison.Ordinal))
        {
            nodeData.SetSimultaneous(ParseBoolValue(trimmedLine));
            return;
        }

        if (trimmedLine.StartsWith("isSwitchColor:", StringComparison.Ordinal))
        {
            nodeData.SetColorSwitch(ParseBoolValue(trimmedLine));
            return;
        }

        if (trimmedLine.StartsWith("isStartWithEmptyColor:", StringComparison.Ordinal))
        {
            nodeData.SetStartWithEmptyColor(ParseBoolValue(trimmedLine));
            return;
        }

        if (trimmedLine.StartsWith("isClockOnToOff:", StringComparison.Ordinal))
        {
            nodeData.SetClockOnToOff(ParseBoolValue(trimmedLine));
            return;
        }

        if (trimmedLine.StartsWith("isClockOffToOn:", StringComparison.Ordinal))
        {
            nodeData.SetClockOffToOn(ParseBoolValue(trimmedLine));
            return;
        }

        if (trimmedLine.StartsWith("clockNum:", StringComparison.Ordinal))
        {
            nodeData.SetClockNum(ParseIntValue(trimmedLine));
            return;
        }

        if (trimmedLine.StartsWith("isBlock:", StringComparison.Ordinal))
        {
            nodeData.SetBlock(ParseBoolValue(trimmedLine));
            return;
        }

        if (trimmedLine.StartsWith("blockBinaryString:", StringComparison.Ordinal))
        {
            nodeData.SetBlockBinaryString(ParseStringValue(trimmedLine));
        }
    }

    private void BuildColorMapping(IReadOnlyList<LegacyLevelAssetData> levelDataList, LegacyLevelMigrationReport report)
    {
        _colorIdByKeyDict.Clear();

        for (int levelIndex = 0; levelIndex < levelDataList.Count; levelIndex++)
        {
            IReadOnlyList<LegacyLevelNodeData> nodeDataList = levelDataList[levelIndex].NodeDataList;

            for (int nodeIndex = 0; nodeIndex < nodeDataList.Count; nodeIndex++)
            {
                IReadOnlyList<Color> colorList = nodeDataList[nodeIndex].ColorList;

                for (int colorIndex = 0; colorIndex < colorList.Count; colorIndex++)
                {
                    LegacyColorKey colorKey = new LegacyColorKey(colorList[colorIndex]);

                    if (_colorIdByKeyDict.ContainsKey(colorKey))
                    {
                        continue;
                    }

                    int colorId = _colorIdByKeyDict.Count + 1;
                    _colorIdByKeyDict.Add(colorKey, colorId);
                    report.AddColorMapping(colorKey, colorId);
                }
            }
        }
    }

    private void TryCreateLevelAsset(
        LegacyLevelAssetData levelData,
        string outputAssetFolder,
        LevelSOMapper mapper,
        LevelDefinitionValidator validator,
        LegacyLevelMigrationReport report)
    {
        try
        {
            string assetPath = GetOutputAssetPath(levelData, outputAssetFolder, report);
            LevelSO levelSO = AssetDatabase.LoadAssetAtPath<LevelSO>(assetPath);

            if (levelSO == null)
            {
                levelSO = ScriptableObject.CreateInstance<LevelSO>();
                AssetDatabase.CreateAsset(levelSO, assetPath);
            }

            WriteLevelSO(levelSO, levelData, report);
            EditorUtility.SetDirty(levelSO);
            ValidateLevel(levelSO, mapper, validator, report);
            report.AddConvertedAsset();
        }
        catch (Exception exception)
        {
            report.AddFailedAsset();
            report.AddError($"Failed to migrate {levelData.Name}: {exception.Message}");
        }
    }

    private void WriteLevelSO(LevelSO levelSO, LegacyLevelAssetData levelData, LegacyLevelMigrationReport report)
    {
        SerializedObject serializedObject = new SerializedObject(levelSO);
        serializedObject.FindProperty("_id").intValue = GetLevelId(levelData.Name, report);
        serializedObject.FindProperty("_rowCount").intValue = levelData.RowCount;
        serializedObject.FindProperty("_columnCount").intValue = levelData.ColumnCount;

        WriteProcessList(serializedObject.FindProperty("_processDataList"), levelData, report);
        WriteResourceList(serializedObject.FindProperty("_resourceDataList"), levelData, report);
        serializedObject.FindProperty("_relayDataList").ClearArray();
        serializedObject.FindProperty("_testCaseDataList").ClearArray();
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private void WriteProcessList(SerializedProperty processListProperty, LegacyLevelAssetData levelData, LegacyLevelMigrationReport report)
    {
        processListProperty.ClearArray();

        for (int i = 0; i < levelData.NodeDataList.Count; i++)
        {
            LegacyLevelNodeData nodeData = levelData.NodeDataList[i];

            if (nodeData.NodeType != ProcessNodeType)
            {
                continue;
            }

            int arrayIndex = processListProperty.arraySize;
            processListProperty.InsertArrayElementAtIndex(arrayIndex);
            SerializedProperty processProperty = processListProperty.GetArrayElementAtIndex(arrayIndex);
            WriteCommonNodeProperties(processProperty, nodeData, levelData.ColumnCount);
            WriteProcessSlotList(processProperty.FindPropertyRelative("_slotDataList"), nodeData, levelData, report);
            AddUnsupportedFieldWarnings(levelData, nodeData, report);
        }
    }

    private void WriteProcessSlotList(
        SerializedProperty slotListProperty,
        LegacyLevelNodeData nodeData,
        LegacyLevelAssetData levelData,
        LegacyLevelMigrationReport report)
    {
        slotListProperty.ClearArray();

        if (nodeData.ColorList.Count == 0)
        {
            report.AddWarning($"{levelData.Name} node {nodeData.Index} process has no colors.");
        }

        for (int i = 0; i < nodeData.ColorList.Count; i++)
        {
            int arrayIndex = slotListProperty.arraySize;
            slotListProperty.InsertArrayElementAtIndex(arrayIndex);

            SerializedProperty slotProperty = slotListProperty.GetArrayElementAtIndex(arrayIndex);
            slotProperty.FindPropertyRelative("_id").intValue = i;
            slotProperty.FindPropertyRelative("_requiredColorId").intValue = GetColorId(nodeData.ColorList[i]);
            slotProperty.FindPropertyRelative("_selectionOrder").intValue = i;
        }
    }

    private void WriteResourceList(SerializedProperty resourceListProperty, LegacyLevelAssetData levelData, LegacyLevelMigrationReport report)
    {
        resourceListProperty.ClearArray();

        for (int i = 0; i < levelData.NodeDataList.Count; i++)
        {
            LegacyLevelNodeData nodeData = levelData.NodeDataList[i];

            if (nodeData.NodeType != ResourceNodeType)
            {
                continue;
            }

            int arrayIndex = resourceListProperty.arraySize;
            resourceListProperty.InsertArrayElementAtIndex(arrayIndex);
            SerializedProperty resourceProperty = resourceListProperty.GetArrayElementAtIndex(arrayIndex);
            WriteCommonNodeProperties(resourceProperty, nodeData, levelData.ColumnCount);
            resourceProperty.FindPropertyRelative("_initialColorId").intValue = GetInitialColorId(levelData, nodeData, report);
            resourceProperty.FindPropertyRelative("_capacity").intValue = nodeData.MaxCount;
            WriteRuleList(resourceProperty.FindPropertyRelative("_ruleDataList"), nodeData);
            AddUnsupportedFieldWarnings(levelData, nodeData, report);
        }
    }

    private void WriteCommonNodeProperties(SerializedProperty nodeProperty, LegacyLevelNodeData nodeData, int columnCount)
    {
        int safeColumnCount = Math.Max(1, columnCount);
        nodeProperty.FindPropertyRelative("_id").intValue = nodeData.Index;
        nodeProperty.FindPropertyRelative("_row").intValue = nodeData.Index / safeColumnCount;
        nodeProperty.FindPropertyRelative("_column").intValue = nodeData.Index % safeColumnCount;
    }

    private void WriteRuleList(
        SerializedProperty ruleListProperty,
        LegacyLevelNodeData nodeData)
    {
        ruleListProperty.ClearArray();

        bool hasRuleFlag = nodeData.IsSimultaneous ||
                           nodeData.IsColorSwitch ||
                           nodeData.IsStartWithEmptyColor ||
                           nodeData.IsClockOffToOn ||
                           nodeData.IsClockOnToOff;

        if (!hasRuleFlag)
        {
            AddRule(ruleListProperty, ELevelResourceRuleType.Basic, EClockMode.OnToOff, 0, null);
            return;
        }

        if (nodeData.IsSimultaneous)
        {
            AddRule(ruleListProperty, ELevelResourceRuleType.Simultaneous, EClockMode.OnToOff, 0, null);
        }

        if (nodeData.IsColorSwitch)
        {
            AddRule(ruleListProperty, ELevelResourceRuleType.ColorSwitch, EClockMode.OnToOff, 0, nodeData.ColorList);
        }

        if (nodeData.IsStartWithEmptyColor)
        {
            AddRule(ruleListProperty, ELevelResourceRuleType.EmptyColor, EClockMode.OnToOff, 0, null);
        }

        if (nodeData.IsClockOffToOn)
        {
            AddRule(ruleListProperty, ELevelResourceRuleType.Clock, EClockMode.OffToOn, nodeData.ClockNum, null);
        }

        if (nodeData.IsClockOnToOff)
        {
            AddRule(ruleListProperty, ELevelResourceRuleType.Clock, EClockMode.OnToOff, nodeData.ClockNum, null);
        }
    }

    private void AddRule(
        SerializedProperty ruleListProperty,
        ELevelResourceRuleType ruleType,
        EClockMode clockMode,
        int clockRoundCount,
        IReadOnlyList<Color> colorList)
    {
        int arrayIndex = ruleListProperty.arraySize;
        ruleListProperty.InsertArrayElementAtIndex(arrayIndex);

        SerializedProperty ruleProperty = ruleListProperty.GetArrayElementAtIndex(arrayIndex);
        ruleProperty.FindPropertyRelative("_ruleType").enumValueIndex = (int)ruleType;
        ruleProperty.FindPropertyRelative("_clockMode").enumValueIndex = (int)clockMode;
        ruleProperty.FindPropertyRelative("_clockRoundCount").intValue = clockRoundCount;

        SerializedProperty colorIdListProperty = ruleProperty.FindPropertyRelative("_colorIdList");
        colorIdListProperty.ClearArray();

        if (colorList is null)
        {
            return;
        }

        for (int i = 0; i < colorList.Count; i++)
        {
            int colorIndex = colorIdListProperty.arraySize;
            colorIdListProperty.InsertArrayElementAtIndex(colorIndex);
            colorIdListProperty.GetArrayElementAtIndex(colorIndex).intValue = GetColorId(colorList[i]);
        }
    }

    private void ValidateLevel(
        LevelSO levelSO,
        LevelSOMapper mapper,
        LevelDefinitionValidator validator,
        LegacyLevelMigrationReport report)
    {
        LevelDefinition definition = mapper.ToLevelDefinition(levelSO);
        LevelValidationResult result = validator.Validate(definition);

        if (result.IsValid)
        {
            report.AddInfo($"{levelSO.name} validation succeeded.");
            return;
        }

        for (int i = 0; i < result.ErrorList.Length; i++)
        {
            report.AddWarning($"{levelSO.name} validation: {result.ErrorList[i].Message}");
        }
    }

    private void AddUnsupportedFieldWarnings(
        LegacyLevelAssetData levelData,
        LegacyLevelNodeData nodeData,
        LegacyLevelMigrationReport report)
    {
        if (nodeData.FixedNum > 0)
        {
            report.AddWarning($"{levelData.Name} node {nodeData.Index} fixedNum {nodeData.FixedNum} is not migrated.");
        }

        if (nodeData.IsBlock || !string.IsNullOrEmpty(nodeData.BlockBinaryString))
        {
            report.AddWarning($"{levelData.Name} node {nodeData.Index} block fields are not migrated.");
        }
    }

    private int GetInitialColorId(LegacyLevelAssetData levelData, LegacyLevelNodeData nodeData, LegacyLevelMigrationReport report)
    {
        if (nodeData.ColorList.Count == 0)
        {
            report.AddWarning($"{levelData.Name} node {nodeData.Index} resource has no initial color.");
            return 0;
        }

        return GetColorId(nodeData.ColorList[0]);
    }

    private int GetColorId(Color color)
    {
        LegacyColorKey key = new LegacyColorKey(color);
        return _colorIdByKeyDict.TryGetValue(key, out int colorId) ? colorId : 0;
    }

    private int GetLevelId(string levelName, LegacyLevelMigrationReport report)
    {
        Match stageMatch = Regex.Match(levelName, @"^Stage(?<number>\d+)(?<suffix>_.*)?$", RegexOptions.IgnoreCase);

        if (stageMatch.Success)
        {
            int number = int.Parse(stageMatch.Groups["number"].Value, CultureInfo.InvariantCulture);
            int levelId = stageMatch.Groups["suffix"].Success ? 1000 + number : number;
            return ReserveLevelId(levelId, levelName, report);
        }

        Match tutorialMatch = Regex.Match(levelName, @"^Tutorial(?<number>\d+)$", RegexOptions.IgnoreCase);

        if (tutorialMatch.Success)
        {
            int number = int.Parse(tutorialMatch.Groups["number"].Value, CultureInfo.InvariantCulture);
            return ReserveLevelId(2000 + number, levelName, report);
        }

        int unknownLevelId = GetNextUnknownLevelId();
        report.AddWarning($"{levelName} uses fallback level id {unknownLevelId}.");
        return ReserveLevelId(unknownLevelId, levelName, report);
    }

    private int ReserveLevelId(int levelId, string levelName, LegacyLevelMigrationReport report)
    {
        if (_usedLevelIdSet.Add(levelId))
        {
            return levelId;
        }

        int fallbackId = GetNextUnknownLevelId();
        report.AddWarning($"{levelName} level id {levelId} is duplicated. Fallback id {fallbackId} was assigned.");
        _usedLevelIdSet.Add(fallbackId);
        return fallbackId;
    }

    private int GetNextUnknownLevelId()
    {
        while (_usedLevelIdSet.Contains(_nextUnknownLevelId))
        {
            _nextUnknownLevelId++;
        }

        return _nextUnknownLevelId++;
    }

    private string GetOutputAssetPath(LegacyLevelAssetData levelData, string outputAssetFolder, LegacyLevelMigrationReport report)
    {
        string assetPath = $"{outputAssetFolder}/{levelData.Name}.asset";
        UnityEngine.Object existingAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);

        if (existingAsset == null || existingAsset is LevelSO)
        {
            return assetPath;
        }

        string uniqueAssetPath = AssetDatabase.GenerateUniqueAssetPath($"{outputAssetFolder}/{levelData.Name}_Migrated.asset");
        report.AddWarning($"{assetPath} already exists with another type. Created {uniqueAssetPath}.");
        return uniqueAssetPath;
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

    private void WriteReportAsset(string outputAssetFolder, LegacyLevelMigrationReport report)
    {
        string reportAssetPath = $"{outputAssetFolder}/{ReportFileName}";
        File.WriteAllText(reportAssetPath, report.ToText());
        AssetDatabase.ImportAsset(reportAssetPath);
    }

    private static int ParseIntValue(string line)
    {
        string value = ParseStringValue(line);
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) ? result : 0;
    }

    private static bool ParseBoolValue(string line)
    {
        return ParseIntValue(line) == 1;
    }

    private static float ParseFloat(string value)
    {
        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result) ? result : 0f;
    }

    private static string ParseStringValue(string line)
    {
        int index = line.IndexOf(':');

        if (index < 0 || index + 1 >= line.Length)
        {
            return string.Empty;
        }

        return line.Substring(index + 1).Trim();
    }
}
