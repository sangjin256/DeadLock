using System.Collections.Generic;
using System.Linq;

public sealed class ResourceFocusInfoBuilder
{
    private readonly int _focusedResourceId;
    private readonly HashSet<int> _highlightedResourceIdSet = new();
    private readonly HashSet<int> _highlightedConnectionIdSet = new();
    private readonly HashSet<int> _activeBoardRuleIdSet = new();

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
        _highlightedResourceIdSet.Add(resourceId);
    }

    public void HighlightConnection(int connectionId)
    {
        _highlightedConnectionIdSet.Add(connectionId);
    }

    public void ActivateBoardRule(int boardRuleId)
    {
        _activeBoardRuleIdSet.Add(boardRuleId);
    }

    public ResourceFocusInfo Build()
    {
        return new ResourceFocusInfo(_focusedResourceId, 
                                     _focusKind, 
                                     _highlightedResourceIdSet.ToArray(), 
                                     _highlightedConnectionIdSet.ToArray(), 
                                     _activeBoardRuleIdSet.ToArray());
    }
}
