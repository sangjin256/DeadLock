using System.Collections.Generic;

public sealed class LevelDefinitionValidator
{
    public LevelValidationResult Validate(LevelDefinition definition)
    {
        List<LevelValidationError> errorList = new List<LevelValidationError>();

        if (definition is null)
        {
            errorList.Add(new LevelValidationError("LevelDefinition is null."));
            return new LevelValidationResult(errorList);
        }

        ValidateBoardSize(definition, errorList);
        ValidateProcessList(definition, errorList);
        Dictionary<int, ResourceDefinition> resourceByIdDict = ValidateResourceList(definition, errorList);
        ValidateBoardRuleList(definition, resourceByIdDict, errorList);

        return new LevelValidationResult(errorList);
    }

    private void ValidateBoardSize(LevelDefinition definition, List<LevelValidationError> errorList)
    {
        if (definition.RowCount <= 0)
        {
            errorList.Add(new LevelValidationError("Level row count must be greater than 0."));
        }

        if (definition.ColumnCount <= 0)
        {
            errorList.Add(new LevelValidationError("Level column count must be greater than 0."));
        }
    }

    private void ValidateProcessList(LevelDefinition definition, List<LevelValidationError> errorList)
    {
        HashSet<int> processIdSet = new HashSet<int>();

        foreach (ProcessDefinition process in definition.ProcessList)
        {
            if (process is null)
            {
                errorList.Add(new LevelValidationError("Process definition must not be null."));
                continue;
            }

            if (!processIdSet.Add(process.Id))
            {
                errorList.Add(new LevelValidationError($"Duplicate process id: {process.Id}."));
            }

            ValidatePosition(process.Position,
                             definition.RowCount,
                             definition.ColumnCount,
                             $"Process {process.Id}",
                             errorList);
            ValidateProcessSlotList(process, errorList);
        }
    }

    private void ValidateProcessSlotList(ProcessDefinition process, List<LevelValidationError> errorList)
    {
        HashSet<int> slotIdSet = new HashSet<int>();

        foreach (ProcessSlotDefinition slot in process.SlotList)
        {
            if (slot is null)
            {
                errorList.Add(new LevelValidationError($"Process {process.Id} slot definition must not be null."));
                continue;
            }

            if (!slotIdSet.Add(slot.Id))
            {
                errorList.Add(new LevelValidationError($"Duplicate slot id {slot.Id} in process {process.Id}."));
            }

            if (slot.RequiredColor == ColorId.None)
            {
                errorList.Add(new LevelValidationError($"Process {process.Id} slot {slot.Id} requires ColorId.None."));
            }
        }
    }

    private Dictionary<int, ResourceDefinition> ValidateResourceList(LevelDefinition definition,
                                                                     List<LevelValidationError> errorList)
    {
        Dictionary<int, ResourceDefinition> resourceByIdDict = new Dictionary<int, ResourceDefinition>();

        foreach (ResourceDefinition resource in definition.ResourceList)
        {
            if (resource is null)
            {
                errorList.Add(new LevelValidationError("Resource definition must not be null."));
                continue;
            }

            if (resourceByIdDict.ContainsKey(resource.Id))
            {
                errorList.Add(new LevelValidationError($"Duplicate resource id: {resource.Id}."));
            }
            else
            {
                resourceByIdDict.Add(resource.Id, resource);
            }

            ValidatePosition(resource.Position,
                             definition.RowCount,
                             definition.ColumnCount,
                             $"Resource {resource.Id}",
                             errorList);
            ValidateResourceRule(resource, errorList);
        }

        return resourceByIdDict;
    }

    private void ValidateResourceRule(ResourceDefinition resource, List<LevelValidationError> errorList)
    {
        if (resource.Capacity <= 0)
        {
            errorList.Add(new LevelValidationError($"Resource {resource.Id} capacity must be greater than 0."));
        }

        if (resource.RuleDefinitionList is null ||
            resource.RuleDefinitionList.Length == 0)
        {
            errorList.Add(new LevelValidationError($"Resource {resource.Id} rule definition list must not be empty."));
            return;
        }

        int clockRuleCount = 0;

        for (int i = 0; i < resource.RuleDefinitionList.Length; i++)
        {
            ResourceRuleDefinition ruleDefinition = resource.RuleDefinitionList[i];

            if (ruleDefinition is null)
            {
                errorList.Add(new LevelValidationError($"Resource {resource.Id} rule definition must not be null."));
                continue;
            }

            if (ruleDefinition is ColorSwitchRuleDefinition colorSwitchDefinition)
            {
                ValidateColorSwitchRule(resource, colorSwitchDefinition, errorList);
            }

            if (ruleDefinition is ClockRuleDefinition clockDefinition)
            {
                clockRuleCount++;
                ValidateClockRule(resource, clockDefinition, errorList);
            }
        }

        if (clockRuleCount > 1)
        {
            errorList.Add(new LevelValidationError($"Resource {resource.Id} must not have multiple clock rule definitions."));
        }
    }

    private void ValidateColorSwitchRule(ResourceDefinition resource,
                                         ColorSwitchRuleDefinition definition,
                                         List<LevelValidationError> errorList)
    {
        if (definition.ColorList.Length == 0)
        {
            errorList.Add(new LevelValidationError($"Resource {resource.Id} ColorSwitch color list must not be empty."));
            return;
        }

        for (int i = 0; i < definition.ColorList.Length; i++)
        {
            if (definition.ColorList[i] == ColorId.None)
            {
                errorList.Add(new LevelValidationError($"Resource {resource.Id} ColorSwitch color list contains ColorId.None."));
            }
        }
    }

    private void ValidateClockRule(ResourceDefinition resource,
                                   ClockRuleDefinition definition,
                                   List<LevelValidationError> errorList)
    {
        if (definition.RoundCount <= 0)
        {
            errorList.Add(new LevelValidationError($"Resource {resource.Id} clock round count must be greater than 0."));
        }
    }

    private void ValidateBoardRuleList(LevelDefinition definition,
                                       Dictionary<int, ResourceDefinition> resourceByIdDict,
                                       List<LevelValidationError> errorList)
    {
        foreach (BoardRuleDefinition boardRule in definition.BoardRuleList)
        {
            if (boardRule is null)
            {
                errorList.Add(new LevelValidationError("Board rule definition must not be null."));
                continue;
            }

            if (boardRule is RelayRuleDefinition relayDefinition)
            {
                ValidateRelayRule(relayDefinition, resourceByIdDict, errorList);
            }
        }
    }

    private void ValidateRelayRule(RelayRuleDefinition definition,
                                   Dictionary<int, ResourceDefinition> resourceByIdDict,
                                   List<LevelValidationError> errorList)
    {
        bool hasFirstResource = resourceByIdDict.ContainsKey(definition.FirstResourceId);
        bool hasSecondResource = resourceByIdDict.ContainsKey(definition.SecondResourceId);

        if (!hasFirstResource)
        {
            errorList.Add(new LevelValidationError($"Relay {definition.Id} first resource does not exist: {definition.FirstResourceId}."));
        }

        if (!hasSecondResource)
        {
            errorList.Add(new LevelValidationError($"Relay {definition.Id} second resource does not exist: {definition.SecondResourceId}."));
        }

        if (definition.RelayType != ERelayType.Transfer)
        {
            return;
        }

        if (definition.SenderResourceId != definition.FirstResourceId &&
            definition.SenderResourceId != definition.SecondResourceId)
        {
            errorList.Add(new LevelValidationError($"RelayTransfer {definition.Id} sender must be one of the paired resources."));
            return;
        }

        if (resourceByIdDict.TryGetValue(definition.SenderResourceId, out ResourceDefinition senderResource) &&
            senderResource.Capacity != 1)
        {
            errorList.Add(new LevelValidationError($"RelayTransfer {definition.Id} sender resource capacity must be 1."));
        }
    }

    private void ValidatePosition(BoardPosition position,
                                  int rowCount,
                                  int columnCount,
                                  string owner,
                                  List<LevelValidationError> errorList)
    {
        if (position.Row < 0 ||
            position.Row >= rowCount ||
            position.Column < 0 ||
            position.Column >= columnCount)
        {
            errorList.Add(new LevelValidationError($"{owner} position is out of board bounds."));
        }
    }
}
