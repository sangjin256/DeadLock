using System;
using UnityEditor;

internal sealed class LevelGenerationCandidateLevelSOWriter
{
    public void Write(
        LevelSO levelSO,
        LevelGenerationCandidateData candidateData,
        LevelGenerationCandidateImportReport report)
    {
        if (levelSO == null)
        {
            throw new ArgumentNullException(nameof(levelSO));
        }

        if (candidateData == null)
        {
            throw new ArgumentNullException(nameof(candidateData));
        }

        SerializedObject serializedObject = new SerializedObject(levelSO);
        serializedObject.FindProperty("_id").intValue = candidateData.id;
        serializedObject.FindProperty("_rowCount").intValue = candidateData.rowCount;
        serializedObject.FindProperty("_columnCount").intValue = candidateData.columnCount;

        WriteProcessList(serializedObject.FindProperty("_processDataList"), candidateData.processDataList);
        WriteResourceList(serializedObject.FindProperty("_resourceDataList"), candidateData.resourceDataList);
        WriteRelayList(serializedObject.FindProperty("_relayDataList"), candidateData.relayDataList);
        WriteStarThreshold(serializedObject.FindProperty("_starThresholdData"), candidateData.starThresholdData);
        WriteTestCaseList(serializedObject.FindProperty("_testCaseDataList"), candidateData.testCaseDataList, report);

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private void WriteProcessList(
        SerializedProperty processListProperty,
        LevelGenerationCandidateData.ProcessData[] processDataArray)
    {
        processListProperty.ClearArray();

        if (processDataArray is null)
        {
            return;
        }

        for (int i = 0; i < processDataArray.Length; i++)
        {
            LevelGenerationCandidateData.ProcessData processData = processDataArray[i];

            if (processData is null)
            {
                continue;
            }

            int arrayIndex = processListProperty.arraySize;
            processListProperty.InsertArrayElementAtIndex(arrayIndex);
            SerializedProperty processProperty = processListProperty.GetArrayElementAtIndex(arrayIndex);
            processProperty.FindPropertyRelative("_id").intValue = processData.id;
            processProperty.FindPropertyRelative("_row").intValue = processData.row;
            processProperty.FindPropertyRelative("_column").intValue = processData.column;
            WriteProcessSlotList(processProperty.FindPropertyRelative("_slotDataList"), processData.slotDataList);
        }
    }

    private void WriteProcessSlotList(
        SerializedProperty slotListProperty,
        LevelGenerationCandidateData.ProcessSlotData[] slotDataArray)
    {
        slotListProperty.ClearArray();

        if (slotDataArray is null)
        {
            return;
        }

        for (int i = 0; i < slotDataArray.Length; i++)
        {
            LevelGenerationCandidateData.ProcessSlotData slotData = slotDataArray[i];

            if (slotData is null)
            {
                continue;
            }

            int arrayIndex = slotListProperty.arraySize;
            slotListProperty.InsertArrayElementAtIndex(arrayIndex);
            SerializedProperty slotProperty = slotListProperty.GetArrayElementAtIndex(arrayIndex);
            slotProperty.FindPropertyRelative("_id").intValue = slotData.id;
            slotProperty.FindPropertyRelative("_requiredColorId").intValue = slotData.requiredColorId;
            slotProperty.FindPropertyRelative("_selectionOrder").intValue = slotData.selectionOrder;
        }
    }

    private void WriteResourceList(
        SerializedProperty resourceListProperty,
        LevelGenerationCandidateData.ResourceData[] resourceDataArray)
    {
        resourceListProperty.ClearArray();

        if (resourceDataArray is null)
        {
            return;
        }

        for (int i = 0; i < resourceDataArray.Length; i++)
        {
            LevelGenerationCandidateData.ResourceData resourceData = resourceDataArray[i];

            if (resourceData is null)
            {
                continue;
            }

            int arrayIndex = resourceListProperty.arraySize;
            resourceListProperty.InsertArrayElementAtIndex(arrayIndex);
            SerializedProperty resourceProperty = resourceListProperty.GetArrayElementAtIndex(arrayIndex);
            resourceProperty.FindPropertyRelative("_id").intValue = resourceData.id;
            resourceProperty.FindPropertyRelative("_row").intValue = resourceData.row;
            resourceProperty.FindPropertyRelative("_column").intValue = resourceData.column;
            resourceProperty.FindPropertyRelative("_initialColorId").intValue = resourceData.initialColorId;
            resourceProperty.FindPropertyRelative("_capacity").intValue = resourceData.capacity;
            WriteRuleList(resourceProperty.FindPropertyRelative("_ruleDataList"), resourceData.ruleDataList);
        }
    }

    private void WriteRuleList(
        SerializedProperty ruleListProperty,
        LevelGenerationCandidateData.ResourceRuleData[] ruleDataArray)
    {
        ruleListProperty.ClearArray();

        if (ruleDataArray is null)
        {
            return;
        }

        for (int i = 0; i < ruleDataArray.Length; i++)
        {
            LevelGenerationCandidateData.ResourceRuleData ruleData = ruleDataArray[i];

            if (ruleData is null)
            {
                continue;
            }

            ELevelResourceRuleType ruleType = ParseRequiredEnum<ELevelResourceRuleType>(ruleData.ruleType, "resource rule type");
            EClockMode clockMode = ParseOptionalEnum(ruleData.clockMode, EClockMode.OnToOff, "clock mode");
            int arrayIndex = ruleListProperty.arraySize;
            ruleListProperty.InsertArrayElementAtIndex(arrayIndex);

            SerializedProperty ruleProperty = ruleListProperty.GetArrayElementAtIndex(arrayIndex);
            ruleProperty.FindPropertyRelative("_ruleType").enumValueIndex = (int)ruleType;
            ruleProperty.FindPropertyRelative("_clockMode").enumValueIndex = (int)clockMode;
            ruleProperty.FindPropertyRelative("_clockRoundCount").intValue = ruleData.clockRoundCount;
            WriteIntList(ruleProperty.FindPropertyRelative("_colorIdList"), ruleData.colorIdList);
        }
    }

    private void WriteRelayList(
        SerializedProperty relayListProperty,
        LevelGenerationCandidateData.RelayData[] relayDataArray)
    {
        relayListProperty.ClearArray();

        if (relayDataArray is null)
        {
            return;
        }

        for (int i = 0; i < relayDataArray.Length; i++)
        {
            LevelGenerationCandidateData.RelayData relayData = relayDataArray[i];

            if (relayData is null)
            {
                continue;
            }

            ERelayType relayType = ParseRequiredEnum<ERelayType>(relayData.relayType, "relay type");
            int arrayIndex = relayListProperty.arraySize;
            relayListProperty.InsertArrayElementAtIndex(arrayIndex);

            SerializedProperty relayProperty = relayListProperty.GetArrayElementAtIndex(arrayIndex);
            relayProperty.FindPropertyRelative("_id").intValue = relayData.id;
            relayProperty.FindPropertyRelative("_relayType").enumValueIndex = (int)relayType;
            relayProperty.FindPropertyRelative("_firstResourceId").intValue = relayData.firstResourceId;
            relayProperty.FindPropertyRelative("_secondResourceId").intValue = relayData.secondResourceId;
            relayProperty.FindPropertyRelative("_senderResourceId").intValue = relayData.senderResourceId;
        }
    }

    private void WriteStarThreshold(
        SerializedProperty starThresholdProperty,
        LevelGenerationCandidateData.StarThresholdData starThresholdData)
    {
        int threeStarRoundCount = 0;
        int twoStarRoundCount = 0;
        int oneStarRoundCount = 0;

        if (starThresholdData is not null)
        {
            threeStarRoundCount = starThresholdData.threeStarRoundCount;
            twoStarRoundCount = starThresholdData.twoStarRoundCount;
            oneStarRoundCount = starThresholdData.oneStarRoundCount;
        }

        starThresholdProperty.FindPropertyRelative("_threeStarRoundCount").intValue = threeStarRoundCount;
        starThresholdProperty.FindPropertyRelative("_twoStarRoundCount").intValue = twoStarRoundCount;
        starThresholdProperty.FindPropertyRelative("_oneStarRoundCount").intValue = oneStarRoundCount;
    }

    private void WriteTestCaseList(
        SerializedProperty testCaseListProperty,
        LevelGenerationCandidateData.TestCaseData[] testCaseDataArray,
        LevelGenerationCandidateImportReport report)
    {
        testCaseListProperty.ClearArray();

        if (testCaseDataArray is null)
        {
            return;
        }

        for (int i = 0; i < testCaseDataArray.Length; i++)
        {
            LevelGenerationCandidateData.TestCaseData testCaseData = testCaseDataArray[i];

            if (testCaseData is null)
            {
                continue;
            }

            int arrayIndex = testCaseListProperty.arraySize;
            testCaseListProperty.InsertArrayElementAtIndex(arrayIndex);
            SerializedProperty testCaseProperty = testCaseListProperty.GetArrayElementAtIndex(arrayIndex);
            testCaseProperty.FindPropertyRelative("_name").stringValue = testCaseData.name ?? string.Empty;
            testCaseProperty.FindPropertyRelative("_maxRoundCount").intValue = testCaseData.maxRoundCount;
            testCaseProperty.FindPropertyRelative("_expectedEndState").enumValueIndex = (int)ParseExpectedEndState(testCaseData.expectedEndState, report);
            WriteAssignedConnectionList(testCaseProperty.FindPropertyRelative("_assignedConnectionDataList"), testCaseData.assignedConnectionDataList);
        }
    }

    private void WriteAssignedConnectionList(
        SerializedProperty assignedConnectionListProperty,
        LevelGenerationCandidateData.AssignedConnectionData[] assignedConnectionDataArray)
    {
        assignedConnectionListProperty.ClearArray();

        if (assignedConnectionDataArray is null)
        {
            return;
        }

        for (int i = 0; i < assignedConnectionDataArray.Length; i++)
        {
            LevelGenerationCandidateData.AssignedConnectionData assignedConnectionData = assignedConnectionDataArray[i];

            if (assignedConnectionData is null)
            {
                continue;
            }

            int arrayIndex = assignedConnectionListProperty.arraySize;
            assignedConnectionListProperty.InsertArrayElementAtIndex(arrayIndex);
            SerializedProperty assignedConnectionProperty = assignedConnectionListProperty.GetArrayElementAtIndex(arrayIndex);
            assignedConnectionProperty.FindPropertyRelative("_processId").intValue = assignedConnectionData.processId;
            assignedConnectionProperty.FindPropertyRelative("_slotId").intValue = assignedConnectionData.slotId;
            assignedConnectionProperty.FindPropertyRelative("_resourceId").intValue = assignedConnectionData.resourceId;
        }
    }

    private void WriteIntList(SerializedProperty intListProperty, int[] valueArray)
    {
        intListProperty.ClearArray();

        if (valueArray is null)
        {
            return;
        }

        for (int i = 0; i < valueArray.Length; i++)
        {
            int arrayIndex = intListProperty.arraySize;
            intListProperty.InsertArrayElementAtIndex(arrayIndex);
            intListProperty.GetArrayElementAtIndex(arrayIndex).intValue = valueArray[i];
        }
    }

    private ESimulationEndState ParseExpectedEndState(
        string value,
        LevelGenerationCandidateImportReport report)
    {
        if (string.Equals(value, "MaxRoundExceeded", StringComparison.OrdinalIgnoreCase))
        {
            report?.AddWarning("expectedEndState MaxRoundExceeded는 현재 Domain enum에 없어 Failed로 저장했습니다.");
            return ESimulationEndState.Failed;
        }

        return ParseRequiredEnum<ESimulationEndState>(value, "expected simulation end state");
    }

    private TEnum ParseRequiredEnum<TEnum>(string value, string label)
        where TEnum : struct
    {
        if (string.IsNullOrWhiteSpace(value) ||
            !Enum.TryParse(value, true, out TEnum result))
        {
            throw new InvalidOperationException($"Invalid {label}: {value}");
        }

        return result;
    }

    private TEnum ParseOptionalEnum<TEnum>(string value, TEnum defaultValue, string label)
        where TEnum : struct
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        if (!Enum.TryParse(value, true, out TEnum result))
        {
            throw new InvalidOperationException($"Invalid {label}: {value}");
        }

        return result;
    }
}
