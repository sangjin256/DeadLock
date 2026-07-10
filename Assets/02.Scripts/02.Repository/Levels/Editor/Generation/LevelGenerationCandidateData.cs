using System;

[Serializable]
internal sealed class LevelGenerationCandidateData
{
    public int id;
    public string name;
    public int rowCount;
    public int columnCount;
    public ProcessData[] processDataList;
    public ResourceData[] resourceDataList;
    public RelayData[] relayDataList;
    public StarThresholdData starThresholdData;
    public TestCaseData[] testCaseDataList;
    public string[] generationNotes;

    [Serializable]
    public sealed class ProcessData
    {
        public int id;
        public int row;
        public int column;
        public ProcessSlotData[] slotDataList;
    }

    [Serializable]
    public sealed class ProcessSlotData
    {
        public int id;
        public int requiredColorId;
        public int selectionOrder;
    }

    [Serializable]
    public sealed class ResourceData
    {
        public int id;
        public int row;
        public int column;
        public int initialColorId;
        public int capacity;
        public ResourceRuleData[] ruleDataList;
    }

    [Serializable]
    public sealed class ResourceRuleData
    {
        public string ruleType;
        public int[] colorIdList;
        public string clockMode;
        public int clockRoundCount;
    }

    [Serializable]
    public sealed class RelayData
    {
        public int id;
        public string relayType;
        public int firstResourceId;
        public int secondResourceId;
        public int senderResourceId;
    }

    [Serializable]
    public sealed class StarThresholdData
    {
        public int threeStarRoundCount;
        public int twoStarRoundCount;
        public int oneStarRoundCount;
    }

    [Serializable]
    public sealed class TestCaseData
    {
        public string name;
        public int maxRoundCount;
        public string expectedEndState;
        public AssignedConnectionData[] assignedConnectionDataList;
    }

    [Serializable]
    public sealed class AssignedConnectionData
    {
        public int processId;
        public int slotId;
        public int resourceId;
    }
}
