using System.Collections.Generic;

internal sealed class LevelEditorValidationUtility
{
    public const int MaximumProcessSlotCount = 6;
    public const int MaximumResourceCapacity = 4;
    public const int MaximumColorSwitchColorCount = 5;

    public string[] Validate(LevelSO levelSO)
    {
        List<string> messageList = new List<string>();

        if (levelSO == null)
        {
            messageList.Add("LevelSO를 선택해 주세요.");
            return messageList.ToArray();
        }

        ValidateBoardSize(levelSO, messageList);
        ValidateProcessList(levelSO, messageList);
        Dictionary<int, LevelResourceData> resourceByIdDict = ValidateResourceList(levelSO, messageList);
        ValidateRelayList(levelSO, resourceByIdDict, messageList);
        ValidateStarThreshold(levelSO, messageList);

        if (messageList.Count == 0)
        {
            messageList.Add("즉시 검증 문제 없음.");
        }

        return messageList.ToArray();
    }

    private void ValidateBoardSize(LevelSO levelSO, List<string> messageList)
    {
        if (levelSO.RowCount <= 0)
        {
            messageList.Add("보드 행 수는 1 이상이어야 합니다.");
        }

        if (levelSO.ColumnCount <= 0)
        {
            messageList.Add("보드 열 수는 1 이상이어야 합니다.");
        }
    }

    private void ValidateProcessList(LevelSO levelSO, List<string> messageList)
    {
        HashSet<int> processIdSet = new HashSet<int>();
        HashSet<string> occupiedCellSet = new HashSet<string>();

        for (int i = 0; i < levelSO.ProcessDataList.Count; i++)
        {
            LevelProcessData processData = levelSO.ProcessDataList[i];

            if (processData == null)
            {
                messageList.Add($"프로세스 항목 {i}가 비어 있습니다.");
                continue;
            }

            if (!processIdSet.Add(processData.Id))
            {
                messageList.Add($"중복 프로세스 ID: {processData.Id}.");
            }

            ValidatePosition("프로세스", processData.Id, processData.Row, processData.Column, levelSO, messageList);
            string cellKey = GetCellKey(processData.Row, processData.Column);

            if (!occupiedCellSet.Add(cellKey))
            {
                messageList.Add($"여러 프로세스가 같은 칸에 있습니다. 행 {processData.Row}, 열 {processData.Column}.");
            }

            ValidateProcessSlotList(processData, messageList);
        }

        ValidateProcessResourceOverlap(levelSO, occupiedCellSet, messageList);
    }

    private void ValidateProcessResourceOverlap(LevelSO levelSO,
                                                HashSet<string> processCellSet,
                                                List<string> messageList)
    {
        for (int i = 0; i < levelSO.ResourceDataList.Count; i++)
        {
            LevelResourceData resourceData = levelSO.ResourceDataList[i];

            if (resourceData == null)
            {
                continue;
            }

            string cellKey = GetCellKey(resourceData.Row, resourceData.Column);

            if (processCellSet.Contains(cellKey))
            {
                messageList.Add($"프로세스와 리소스가 같은 칸에 있습니다. 행 {resourceData.Row}, 열 {resourceData.Column}.");
            }
        }
    }

    private void ValidateProcessSlotList(LevelProcessData processData, List<string> messageList)
    {
        HashSet<int> slotIdSet = new HashSet<int>();

        if (processData.SlotDataList.Count > MaximumProcessSlotCount)
        {
            messageList.Add($"Process {processData.Id} supports at most {MaximumProcessSlotCount} slots for the LevelPlay prefab contract.");
        }

        for (int i = 0; i < processData.SlotDataList.Count; i++)
        {
            LevelProcessSlotData slotData = processData.SlotDataList[i];

            if (slotData == null)
            {
                messageList.Add($"프로세스 {processData.Id}의 슬롯 항목 {i}가 비어 있습니다.");
                continue;
            }

            if (!slotIdSet.Add(slotData.Id))
            {
                messageList.Add($"프로세스 {processData.Id} 안에 중복 슬롯 ID {slotData.Id}가 있습니다.");
            }

            if (slotData.RequiredColorId <= 0)
            {
                messageList.Add($"프로세스 {processData.Id} 슬롯 {slotData.Id}의 ColorId가 올바르지 않습니다: {slotData.RequiredColorId}.");
            }
        }
    }

    private Dictionary<int, LevelResourceData> ValidateResourceList(LevelSO levelSO, List<string> messageList)
    {
        Dictionary<int, LevelResourceData> resourceByIdDict = new Dictionary<int, LevelResourceData>();
        HashSet<string> resourceCellSet = new HashSet<string>();

        for (int i = 0; i < levelSO.ResourceDataList.Count; i++)
        {
            LevelResourceData resourceData = levelSO.ResourceDataList[i];

            if (resourceData == null)
            {
                messageList.Add($"리소스 항목 {i}가 비어 있습니다.");
                continue;
            }

            if (resourceByIdDict.ContainsKey(resourceData.Id))
            {
                messageList.Add($"중복 리소스 ID: {resourceData.Id}.");
            }
            else
            {
                resourceByIdDict.Add(resourceData.Id, resourceData);
            }

            ValidatePosition("리소스", resourceData.Id, resourceData.Row, resourceData.Column, levelSO, messageList);
            string cellKey = GetCellKey(resourceData.Row, resourceData.Column);

            if (!resourceCellSet.Add(cellKey))
            {
                messageList.Add($"여러 리소스가 같은 칸에 있습니다. 행 {resourceData.Row}, 열 {resourceData.Column}.");
            }

            if (resourceData.InitialColorId <= 0)
            {
                messageList.Add($"리소스 {resourceData.Id}의 초기 ColorId가 올바르지 않습니다: {resourceData.InitialColorId}.");
            }

            if (resourceData.Capacity <= 0)
            {
                messageList.Add($"리소스 {resourceData.Id}의 수용량은 1 이상이어야 합니다.");
            }

            if (resourceData.Capacity > MaximumResourceCapacity)
            {
                messageList.Add($"Resource {resourceData.Id} supports at most {MaximumResourceCapacity} capacity slots for the LevelPlay prefab contract.");
            }

            ValidateResourceRuleList(resourceData, messageList);
        }

        return resourceByIdDict;
    }

    private void ValidateResourceRuleList(LevelResourceData resourceData, List<string> messageList)
    {
        if (resourceData.RuleDataList.Count == 0)
        {
            messageList.Add($"리소스 {resourceData.Id}의 규칙 목록은 비어 있을 수 없습니다.");
            return;
        }

        int clockRuleCount = 0;

        for (int i = 0; i < resourceData.RuleDataList.Count; i++)
        {
            LevelResourceRuleData ruleData = resourceData.RuleDataList[i];

            if (ruleData == null)
            {
                messageList.Add($"리소스 {resourceData.Id}의 규칙 항목 {i}가 비어 있습니다.");
                continue;
            }

            if (ruleData.RuleType == ELevelResourceRuleType.ColorSwitch)
            {
                ValidateColorSwitchRule(resourceData, ruleData, messageList);
            }

            if (ruleData.RuleType == ELevelResourceRuleType.Clock)
            {
                clockRuleCount++;
                ValidateClockRule(resourceData, ruleData, messageList);
            }
        }

        if (clockRuleCount > 1)
        {
            messageList.Add($"리소스 {resourceData.Id}에 Clock 규칙이 여러 개 있습니다.");
        }
    }

    private void ValidateColorSwitchRule(LevelResourceData resourceData,
                                         LevelResourceRuleData ruleData,
                                         List<string> messageList)
    {
        if (ruleData.ColorIdList.Count == 0)
        {
            messageList.Add($"리소스 {resourceData.Id}의 ColorSwitch 색 목록은 비어 있을 수 없습니다.");
            return;
        }

        if (ruleData.ColorIdList.Count > MaximumColorSwitchColorCount)
        {
            messageList.Add($"Resource {resourceData.Id} ColorSwitch supports at most {MaximumColorSwitchColorCount} colors for the LevelPlay prefab contract.");
        }

        for (int i = 0; i < ruleData.ColorIdList.Count; i++)
        {
            if (ruleData.ColorIdList[i] <= 0)
            {
                messageList.Add($"리소스 {resourceData.Id}의 ColorSwitch에 올바르지 않은 ColorId가 있습니다: {ruleData.ColorIdList[i]}.");
            }
        }
    }

    private void ValidateClockRule(LevelResourceData resourceData,
                                   LevelResourceRuleData ruleData,
                                   List<string> messageList)
    {
        if (ruleData.ClockRoundCount <= 0)
        {
            messageList.Add($"리소스 {resourceData.Id}의 Clock 라운드 수는 1 이상이어야 합니다.");
        }
    }

    private void ValidateRelayList(LevelSO levelSO,
                                   Dictionary<int, LevelResourceData> resourceByIdDict,
                                   List<string> messageList)
    {
        for (int i = 0; i < levelSO.RelayDataList.Count; i++)
        {
            LevelRelayData relayData = levelSO.RelayDataList[i];

            if (relayData == null)
            {
                messageList.Add($"Relay 항목 {i}가 비어 있습니다.");
                continue;
            }

            if (!resourceByIdDict.ContainsKey(relayData.FirstResourceId))
            {
                messageList.Add($"Relay {relayData.Id}의 첫 번째 리소스가 존재하지 않습니다: {relayData.FirstResourceId}.");
            }

            if (!resourceByIdDict.ContainsKey(relayData.SecondResourceId))
            {
                messageList.Add($"Relay {relayData.Id}의 두 번째 리소스가 존재하지 않습니다: {relayData.SecondResourceId}.");
            }

            if (relayData.RelayType != ERelayType.Transfer)
            {
                continue;
            }

            if (relayData.SenderResourceId != relayData.FirstResourceId &&
                relayData.SenderResourceId != relayData.SecondResourceId)
            {
                messageList.Add($"RelayTransfer {relayData.Id}의 sender는 연결된 두 리소스 중 하나여야 합니다.");
                continue;
            }

            if (resourceByIdDict.TryGetValue(relayData.SenderResourceId, out LevelResourceData senderResource) &&
                senderResource.Capacity != 1)
            {
                messageList.Add($"RelayTransfer {relayData.Id}의 sender 리소스 수용량은 1이어야 합니다.");
            }
        }
    }

    private void ValidateStarThreshold(LevelSO levelSO, List<string> messageList)
    {
        LevelStarThresholdData starThresholdData = levelSO.StarThresholdData;

        if (starThresholdData == null)
        {
            return;
        }

        int threeStarRoundCount = starThresholdData.ThreeStarRoundCount;
        int twoStarRoundCount = starThresholdData.TwoStarRoundCount;
        int oneStarRoundCount = starThresholdData.OneStarRoundCount;

        if (threeStarRoundCount == 0 &&
            twoStarRoundCount == 0 &&
            oneStarRoundCount == 0)
        {
            return;
        }

        if (threeStarRoundCount <= 0 ||
            twoStarRoundCount < threeStarRoundCount ||
            oneStarRoundCount < twoStarRoundCount)
        {
            messageList.Add("별 기준은 0이 아니어야 하며 3별 <= 2별 <= 1별 라운드 순서를 지켜야 합니다.");
        }
    }

    private void ValidatePosition(string owner,
                                  int id,
                                  int row,
                                  int column,
                                  LevelSO levelSO,
                                  List<string> messageList)
    {
        if (row < 0 ||
            row >= levelSO.RowCount ||
            column < 0 ||
            column >= levelSO.ColumnCount)
        {
            messageList.Add($"{owner} {id} 위치가 보드 범위를 벗어났습니다.");
        }
    }

    private static string GetCellKey(int row, int column)
    {
        return $"{row}:{column}";
    }
}
