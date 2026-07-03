public enum EProcessState
{
    Running,
    Waiting,
    Failed,
    Completed,
}

public enum EConnectionState
{
    Planned,
    Completed,
    Blocked,
}

public enum EFocusKind
{
    None,
    Single,
    Pair,
    Group,
    Board,
}

public enum ERelayType
{
    Link,
    Transfer,
}

public enum EAssignConnectionError
{
    None,
    NotFound,
    NotConnectable,
    RuleRejected,
}