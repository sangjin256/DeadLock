using UnityEngine;

internal sealed class LevelAssignmentLineData
{
    private readonly Vector2 _startPosition;
    public Vector2 StartPosition => _startPosition;

    private readonly Vector2 _endPosition;
    public Vector2 EndPosition => _endPosition;

    private readonly int _order;
    public int Order => _order;

    private readonly bool _isFocused;
    public bool IsFocused => _isFocused;

    private readonly bool _isConnectionVisible;
    public bool IsConnectionVisible => _isConnectionVisible;

    private readonly bool _isProcessCompleted;
    public bool IsProcessCompleted => _isProcessCompleted;

    private readonly ELevelTestConnectionVisualState _visualState;
    public ELevelTestConnectionVisualState VisualState => _visualState;

    public LevelAssignmentLineData(Vector2 startPosition,
                                   Vector2 endPosition,
                                   int order,
                                   bool isFocused,
                                   bool isConnectionVisible,
                                   bool isProcessCompleted,
                                   ELevelTestConnectionVisualState visualState)
    {
        _startPosition = startPosition;
        _endPosition = endPosition;
        _order = order;
        _isFocused = isFocused;
        _isConnectionVisible = isConnectionVisible;
        _isProcessCompleted = isProcessCompleted;
        _visualState = visualState;
    }
}
