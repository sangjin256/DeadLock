public sealed class ResourceFocusInfo
{
    public readonly int FocusResourceId;
    public readonly EFocusKind FocusKind;
    public readonly int[] HighlightedResourceIdList;
    public readonly int[] HighlightedConnectionIdList;
    public readonly int[] ActivateBoardRuleIdList;

    public ResourceFocusInfo(int focusResourceId, 
                             EFocusKind focusKind, 
                             int[] highlightedResourceIdList, 
                             int[] highlightedConnectionIdList, 
                             int[] activateBoardRuleIdList)
    {
        FocusResourceId = focusResourceId;
        FocusKind = focusKind;
        HighlightedResourceIdList = highlightedResourceIdList;
        HighlightedConnectionIdList = highlightedConnectionIdList;
        ActivateBoardRuleIdList = activateBoardRuleIdList;
    }
}