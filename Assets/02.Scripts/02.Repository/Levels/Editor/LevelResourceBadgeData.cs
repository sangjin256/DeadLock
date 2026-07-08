using UnityEngine;

internal sealed class LevelResourceBadgeData
{
    private readonly string _label;
    public string Label => _label;

    private readonly string _tooltip;
    public string Tooltip => _tooltip;

    private readonly Color _backgroundColor;
    public Color BackgroundColor => _backgroundColor;

    public LevelResourceBadgeData(string label, string tooltip, Color backgroundColor)
    {
        _label = label;
        _tooltip = tooltip;
        _backgroundColor = backgroundColor;
    }
}
