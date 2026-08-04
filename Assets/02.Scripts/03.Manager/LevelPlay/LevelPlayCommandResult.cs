using System.Collections.Generic;

public sealed class LevelPlayCommandResult
{
    public readonly bool Success;
    public readonly ELevelPlayCommandError Error;
    public readonly int ConnectionId;
    public readonly LevelPlayDTO Level;
    public readonly LevelSimulationDTO Simulation;
    public readonly string[] ValidationMessageArray;

    private LevelPlayCommandResult(bool success,
                                   ELevelPlayCommandError error,
                                   int connectionId,
                                   LevelPlayDTO level,
                                   LevelSimulationDTO simulation,
                                   IReadOnlyList<string> validationMessageList)
    {
        Success = success;
        Error = error;
        ConnectionId = connectionId;
        Level = level;
        Simulation = simulation;
        ValidationMessageArray = new string[validationMessageList.Count];

        for (int i = 0; i < validationMessageList.Count; i++)
        {
            ValidationMessageArray[i] = validationMessageList[i];
        }
    }

    public static LevelPlayCommandResult Ok(LevelPlayDTO level,
                                            int connectionId = -1,
                                            LevelSimulationDTO simulation = null)
    {
        return new LevelPlayCommandResult(true,
                                          ELevelPlayCommandError.None,
                                          connectionId,
                                          level,
                                          simulation,
                                          new string[0]);
    }

    public static LevelPlayCommandResult Fail(ELevelPlayCommandError error,
                                              LevelPlayDTO level = null,
                                              IReadOnlyList<string> validationMessageList = null)
    {
        return new LevelPlayCommandResult(false,
                                          error,
                                          -1,
                                          level,
                                          null,
                                          validationMessageList ?? new string[0]);
    }
}
