using System.Collections.Generic;

internal static class LevelDefinitionSelectionOrderUtility
{
    public static LevelDefinition CreateWithAssignmentOrder(
        LevelDefinition definition,
        IReadOnlyList<LevelSolveAssignment> assignmentList)
    {
        if (definition is null || assignmentList is null)
        {
            return definition;
        }

        Dictionary<string, int> orderBySlotKeyDict = CreateOrderBySlotKeyDict(assignmentList);
        ProcessDefinition[] processArray = new ProcessDefinition[definition.ProcessList.Length];

        for (int i = 0; i < definition.ProcessList.Length; i++)
        {
            ProcessDefinition process = definition.ProcessList[i];

            if (process is null)
            {
                processArray[i] = null;
                continue;
            }

            ProcessSlotDefinition[] slotArray = new ProcessSlotDefinition[process.SlotList.Length];

            for (int j = 0; j < process.SlotList.Length; j++)
            {
                ProcessSlotDefinition slot = process.SlotList[j];

                if (slot is null)
                {
                    slotArray[j] = null;
                    continue;
                }

                string key = CreateSlotKey(process.Id, slot.Id);
                int selectionOrder = orderBySlotKeyDict.TryGetValue(key, out int assignedOrder) ?
                    assignedOrder :
                    slot.SelectionOrder;

                slotArray[j] = new ProcessSlotDefinition(slot.Id,
                                                          slot.RequiredColor,
                                                          selectionOrder);
            }

            processArray[i] = new ProcessDefinition(process.Id,
                                                    process.Position,
                                                    slotArray);
        }

        return new LevelDefinition(definition.Id,
                                   definition.RowCount,
                                   definition.ColumnCount,
                                   processArray,
                                   definition.ResourceList,
                                   definition.BoardRuleList);
    }

    private static Dictionary<string, int> CreateOrderBySlotKeyDict(IReadOnlyList<LevelSolveAssignment> assignmentList)
    {
        Dictionary<string, int> orderBySlotKeyDict = new Dictionary<string, int>();
        Dictionary<int, int> nextOrderByProcessIdDict = new Dictionary<int, int>();

        for (int i = 0; i < assignmentList.Count; i++)
        {
            LevelSolveAssignment assignment = assignmentList[i];

            if (assignment is null)
            {
                continue;
            }

            string key = CreateSlotKey(assignment.ProcessId, assignment.SlotId);

            if (orderBySlotKeyDict.ContainsKey(key))
            {
                continue;
            }

            if (!nextOrderByProcessIdDict.TryGetValue(assignment.ProcessId, out int nextOrder))
            {
                nextOrder = 0;
            }

            orderBySlotKeyDict.Add(key, nextOrder);
            nextOrderByProcessIdDict[assignment.ProcessId] = nextOrder + 1;
        }

        return orderBySlotKeyDict;
    }

    private static string CreateSlotKey(int processId, int slotId)
    {
        return processId + ":" + slotId;
    }
}
