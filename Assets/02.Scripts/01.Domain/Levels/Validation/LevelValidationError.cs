public sealed class LevelValidationError
{
    public readonly string Message;

    public LevelValidationError(string message)
    {
        Message = message ?? string.Empty;
    }
}
