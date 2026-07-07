using System.Collections.Generic;

public sealed class LevelValidationResult
{
    public readonly LevelValidationError[] ErrorList;

    public bool IsValid => ErrorList.Length == 0;

    public LevelValidationResult(IReadOnlyList<LevelValidationError> errorList)
    {
        if (errorList is null)
        {
            ErrorList = new LevelValidationError[0];
            return;
        }

        ErrorList = new LevelValidationError[errorList.Count];

        for (int i = 0; i < errorList.Count; i++)
        {
            ErrorList[i] = errorList[i];
        }
    }
}
