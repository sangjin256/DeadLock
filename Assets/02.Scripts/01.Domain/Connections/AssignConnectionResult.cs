public sealed class AssignConnectionResult
{
    public readonly bool Success;
    public readonly int ConnectionId;
    public readonly EAssignConnectionError Error;

    private AssignConnectionResult(bool success, int connectionId, EAssignConnectionError error)
    {
        Success = success;
        ConnectionId = connectionId;
        Error = error;
    }

    public static AssignConnectionResult Ok(int connectionId)
    {
        return new AssignConnectionResult(true, connectionId, EAssignConnectionError.None);
    }

    public static AssignConnectionResult Fail(EAssignConnectionError error)
    {
        return new AssignConnectionResult(false, -1, error);
    }
}