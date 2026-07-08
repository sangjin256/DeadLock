using System.Collections.Generic;

public sealed class LevelSOMapper
{
    public LevelDefinition ToLevelDefinition(LevelSO levelSO)
    {
        if (levelSO == null)
        {
            return null;
        }

        ProcessDefinition[] processList = ToProcessDefinitionArray(levelSO.ProcessDataList);
        ResourceDefinition[] resourceList = ToResourceDefinitionArray(levelSO.ResourceDataList);
        BoardRuleDefinition[] boardRuleList = ToBoardRuleDefinitionArray(levelSO.RelayDataList);

        return new LevelDefinition(levelSO.Id,
                                   levelSO.RowCount,
                                   levelSO.ColumnCount,
                                   processList,
                                   resourceList,
                                   boardRuleList);
    }

    private ProcessDefinition[] ToProcessDefinitionArray(IReadOnlyList<LevelProcessData> processDataList)
    {
        if (processDataList is null)
        {
            return new ProcessDefinition[0];
        }

        ProcessDefinition[] processList = new ProcessDefinition[processDataList.Count];

        for (int i = 0; i < processDataList.Count; i++)
        {
            LevelProcessData processData = processDataList[i];

            if (processData is null)
            {
                processList[i] = null;
                continue;
            }

            BoardPosition position = new BoardPosition(processData.Row, processData.Column);
            ProcessSlotDefinition[] slotList = ToProcessSlotDefinitionArray(processData.SlotDataList);
            processList[i] = new ProcessDefinition(processData.Id, position, slotList);
        }

        return processList;
    }

    private ProcessSlotDefinition[] ToProcessSlotDefinitionArray(IReadOnlyList<LevelProcessSlotData> slotDataList)
    {
        if (slotDataList is null)
        {
            return new ProcessSlotDefinition[0];
        }

        ProcessSlotDefinition[] slotList = new ProcessSlotDefinition[slotDataList.Count];

        for (int i = 0; i < slotDataList.Count; i++)
        {
            LevelProcessSlotData slotData = slotDataList[i];

            if (slotData is null)
            {
                slotList[i] = null;
                continue;
            }

            ColorId requiredColor = new ColorId(slotData.RequiredColorId);
            slotList[i] = new ProcessSlotDefinition(slotData.Id, requiredColor, slotData.SelectionOrder);
        }

        return slotList;
    }

    private ResourceDefinition[] ToResourceDefinitionArray(IReadOnlyList<LevelResourceData> resourceDataList)
    {
        if (resourceDataList is null)
        {
            return new ResourceDefinition[0];
        }

        ResourceDefinition[] resourceList = new ResourceDefinition[resourceDataList.Count];

        for (int i = 0; i < resourceDataList.Count; i++)
        {
            LevelResourceData resourceData = resourceDataList[i];

            if (resourceData is null)
            {
                resourceList[i] = null;
                continue;
            }

            BoardPosition position = new BoardPosition(resourceData.Row, resourceData.Column);
            ColorId initialColor = new ColorId(resourceData.InitialColorId);
            ResourceRuleDefinition[] ruleDefinitionList = ToResourceRuleDefinitionArray(resourceData.RuleDataList);
            resourceList[i] = new ResourceDefinition(resourceData.Id,
                                                     position,
                                                     initialColor,
                                                     resourceData.Capacity,
                                                     ruleDefinitionList);
        }

        return resourceList;
    }

    private ResourceRuleDefinition[] ToResourceRuleDefinitionArray(IReadOnlyList<LevelResourceRuleData> ruleDataList)
    {
        if (ruleDataList is null)
        {
            return new ResourceRuleDefinition[0];
        }

        ResourceRuleDefinition[] ruleDefinitionArray = new ResourceRuleDefinition[ruleDataList.Count];

        for (int i = 0; i < ruleDataList.Count; i++)
        {
            ruleDefinitionArray[i] = ToResourceRuleDefinition(ruleDataList[i]);
        }

        return ruleDefinitionArray;
    }

    private ResourceRuleDefinition ToResourceRuleDefinition(LevelResourceRuleData ruleData)
    {
        if (ruleData is null)
        {
            return null;
        }

        switch (ruleData.RuleType)
        {
            case ELevelResourceRuleType.Basic:
                return new NoResourceRuleDefinition();

            case ELevelResourceRuleType.ColorSwitch:
                return new ColorSwitchRuleDefinition(ToColorIdArray(ruleData.ColorIdList));

            case ELevelResourceRuleType.EmptyColor:
                return new EmptyColorRuleDefinition();

            case ELevelResourceRuleType.Clock:
                return new ClockRuleDefinition(ruleData.ClockMode, ruleData.ClockRoundCount);

            case ELevelResourceRuleType.Simultaneous:
                return new SimultaneousRuleDefinition();

            default:
                return null;
        }
    }

    private ColorId[] ToColorIdArray(IReadOnlyList<int> colorIdList)
    {
        if (colorIdList is null)
        {
            return new ColorId[0];
        }

        ColorId[] colorList = new ColorId[colorIdList.Count];

        for (int i = 0; i < colorIdList.Count; i++)
        {
            colorList[i] = new ColorId(colorIdList[i]);
        }

        return colorList;
    }

    private BoardRuleDefinition[] ToBoardRuleDefinitionArray(IReadOnlyList<LevelRelayData> relayDataList)
    {
        if (relayDataList is null)
        {
            return new BoardRuleDefinition[0];
        }

        BoardRuleDefinition[] boardRuleList = new BoardRuleDefinition[relayDataList.Count];

        for (int i = 0; i < relayDataList.Count; i++)
        {
            LevelRelayData relayData = relayDataList[i];

            if (relayData is null)
            {
                boardRuleList[i] = null;
                continue;
            }

            boardRuleList[i] = new RelayRuleDefinition(relayData.Id,
                                                       relayData.RelayType,
                                                       relayData.FirstResourceId,
                                                       relayData.SecondResourceId,
                                                       relayData.SenderResourceId);
        }

        return boardRuleList;
    }
}
