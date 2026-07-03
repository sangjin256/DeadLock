using System.Collections.Generic;

public sealed class ResourceFocusInfoBuilder
{
    private readonly int _focusedResourceId;
    private readonly List<int> _highlightedResourceIdList = new();
    private readonly List<int> _highlightedConnectionIdList = new();
    private readonly List<int> _activeBoardRuleIdList = new();

    private EFocusKind _focusKind = EFocusKind.Single;

    public ResourceFocusInfoBuilder(int focusedResourceId)
    {
        _focusedResourceId = focusedResourceId;
        HighlightResource(focusedResourceId);
    }

    public void SetKind(EFocusKind focusKind)
    {
        _focusKind = focusKind;
    }

    public void HighlightResource(int resourceId)
    {
        if (!_highlightedResourceIdList.Contains(resourceId))
        {
            _highlightedResourceIdList.Add(resourceId);
        }
    }

    public void HighlightConnection(int connectionId)
    {
        if (!_highlightedConnectionIdList.Contains(connectionId))
        {
            _highlightedConnectionIdList.Add(connectionId);
        }
    }

    public void ActivateBoardRule(int boardRuleId)
    {
        if (!_activeBoardRuleIdList.Contains(boardRuleId))
        {
            _activeBoardRuleIdList.Add(boardRuleId);
        }
    }

    public ResourceFocusInfo Build()
    {
        return new ResourceFocusInfo(_focusedResourceId, 
                                     _focusKind, 
                                     _highlightedResourceIdList.ToArray(), 
                                     _highlightedConnectionIdList.ToArray(), 
                                     _activeBoardRuleIdList.ToArray());
    }
}