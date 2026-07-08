using System;
using System.Collections.Generic;

public sealed class LevelBoardFactory
{
    public Board CreateBoard(LevelDefinition definition)
    {
        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        ProcessNode[] processArray = CreateProcessArray(definition.ProcessList);
        ResourceNode[] resourceArray = CreateResourceArray(definition.ResourceList);
        IBoardRule[] boardRuleArray = CreateBoardRuleArray(definition.BoardRuleList);

        return new Board(processArray, resourceArray, boardRuleArray);
    }

    private ProcessNode[] CreateProcessArray(ProcessDefinition[] definitionArray)
    {
        ProcessNode[] processArray = new ProcessNode[definitionArray.Length];

        for (int i = 0; i < definitionArray.Length; i++)
        {
            ProcessDefinition definition = definitionArray[i];
            List<ProcessColorSlot> slotList = new List<ProcessColorSlot>(definition.SlotList.Length);

            for (int slotIndex = 0; slotIndex < definition.SlotList.Length; slotIndex++)
            {
                ProcessSlotDefinition slotDefinition = definition.SlotList[slotIndex];
                slotList.Add(new ProcessColorSlot(slotDefinition.Id,
                                                  slotDefinition.RequiredColor,
                                                  slotDefinition.SelectionOrder));
            }

            processArray[i] = new ProcessNode(definition.Id, slotList, definition.Position);
        }

        return processArray;
    }

    private ResourceNode[] CreateResourceArray(ResourceDefinition[] definitionArray)
    {
        ResourceNode[] resourceArray = new ResourceNode[definitionArray.Length];

        for (int i = 0; i < definitionArray.Length; i++)
        {
            ResourceDefinition definition = definitionArray[i];
            resourceArray[i] = new ResourceNode(definition.Id,
                                                definition.InitialColor,
                                                definition.Capacity,
                                                CreateResourceRule(definition.RuleDefinitionList),
                                                definition.Position);
        }

        return resourceArray;
    }

    private IResourceRule CreateResourceRule(ResourceRuleDefinition[] definitionArray)
    {
        if (definitionArray is null || definitionArray.Length == 0)
        {
            return NoResourceRule.Instance;
        }

        List<IResourceRule> ruleList = new List<IResourceRule>();

        for (int i = 0; i < definitionArray.Length; i++)
        {
            IResourceRule rule = CreateSingleResourceRule(definitionArray[i]);

            if (rule != NoResourceRule.Instance)
            {
                ruleList.Add(rule);
            }
        }

        if (ruleList.Count == 0)
        {
            return NoResourceRule.Instance;
        }

        if (ruleList.Count == 1)
        {
            return ruleList[0];
        }

        return new CompositeResourceRule(ruleList);
    }

    private IResourceRule CreateSingleResourceRule(ResourceRuleDefinition definition)
    {
        if (definition is null || definition is NoResourceRuleDefinition)
        {
            return NoResourceRule.Instance;
        }

        if (definition is ColorSwitchRuleDefinition colorSwitchDefinition)
        {
            return new ColorSwitchRule(colorSwitchDefinition.ColorList);
        }

        if (definition is EmptyColorRuleDefinition)
        {
            return new EmptyColorRule();
        }

        if (definition is ClockRuleDefinition clockDefinition)
        {
            return new ClockRule(clockDefinition.Mode, clockDefinition.RoundCount);
        }

        if (definition is SimultaneousRuleDefinition)
        {
            return new SimultaneousRule();
        }

        throw new NotSupportedException($"Unsupported resource rule definition: {definition.GetType().Name}");
    }

    private IBoardRule[] CreateBoardRuleArray(BoardRuleDefinition[] definitionArray)
    {
        IBoardRule[] boardRuleArray = new IBoardRule[definitionArray.Length];

        for (int i = 0; i < definitionArray.Length; i++)
        {
            boardRuleArray[i] = CreateBoardRule(definitionArray[i]);
        }

        return boardRuleArray;
    }

    private IBoardRule CreateBoardRule(BoardRuleDefinition definition)
    {
        if (definition is RelayRuleDefinition relayDefinition)
        {
            RelayRelation relation = new RelayRelation(relayDefinition.Id,
                                                       relayDefinition.FirstResourceId,
                                                       relayDefinition.SecondResourceId,
                                                       relayDefinition.RelayType,
                                                       relayDefinition.SenderResourceId);

            if (relayDefinition.RelayType == ERelayType.Link)
            {
                return new RelayLinkRule(relation);
            }

            if (relayDefinition.RelayType == ERelayType.Transfer)
            {
                return new RelayTransferRule(relation);
            }
        }

        throw new NotSupportedException($"Unsupported board rule definition: {definition?.GetType().Name ?? "null"}");
    }
}
