using UnityEngine;

internal sealed class LevelRelayLineData
{
    private readonly Vector2 _startPosition;
    public Vector2 StartPosition => _startPosition;

    private readonly Vector2 _endPosition;
    public Vector2 EndPosition => _endPosition;

    private readonly ERelayType _relayType;
    public ERelayType RelayType => _relayType;

    private readonly bool _isHighlighted;
    public bool IsHighlighted => _isHighlighted;

    private readonly bool _isFocused;
    public bool IsFocused => _isFocused;

    private readonly bool _isDraft;
    public bool IsDraft => _isDraft;

    public LevelRelayLineData(Vector2 startPosition,
                              Vector2 endPosition,
                              ERelayType relayType,
                              bool isHighlighted,
                              bool isFocused,
                              bool isDraft)
    {
        _startPosition = startPosition;
        _endPosition = endPosition;
        _relayType = relayType;
        _isHighlighted = isHighlighted;
        _isFocused = isFocused;
        _isDraft = isDraft;
    }
}
