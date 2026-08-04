using System.Collections.Generic;

public sealed class LevelPlayDTO
{
    public readonly int LevelId;
    public readonly ELevelPlayPhase Phase;
    public readonly bool CanStartSimulation;
    public readonly LevelPlayProcessDTO[] ProcessArray;
    public readonly LevelPlayResourceDTO[] ResourceArray;
    public readonly LevelPlayConnectionDTO[] ConnectionArray;
    public readonly LevelPlayRelayDTO[] RelayArray;

    public LevelPlayDTO(int levelId,
                        ELevelPlayPhase phase,
                        bool canStartSimulation,
                        IReadOnlyList<LevelPlayProcessDTO> processList,
                        IReadOnlyList<LevelPlayResourceDTO> resourceList,
                        IReadOnlyList<LevelPlayConnectionDTO> connectionList,
                        IReadOnlyList<LevelPlayRelayDTO> relayList)
    {
        LevelId = levelId;
        Phase = phase;
        CanStartSimulation = canStartSimulation;
        ProcessArray = new LevelPlayProcessDTO[processList.Count];
        ResourceArray = new LevelPlayResourceDTO[resourceList.Count];
        ConnectionArray = new LevelPlayConnectionDTO[connectionList.Count];
        RelayArray = new LevelPlayRelayDTO[relayList.Count];

        for (int i = 0; i < processList.Count; i++)
        {
            ProcessArray[i] = processList[i];
        }

        for (int i = 0; i < resourceList.Count; i++)
        {
            ResourceArray[i] = resourceList[i];
        }

        for (int i = 0; i < connectionList.Count; i++)
        {
            ConnectionArray[i] = connectionList[i];
        }

        for (int i = 0; i < relayList.Count; i++)
        {
            RelayArray[i] = relayList[i];
        }
    }
}
