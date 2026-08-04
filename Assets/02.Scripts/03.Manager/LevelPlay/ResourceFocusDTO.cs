using System.Collections.Generic;

public sealed class ResourceFocusDTO
{
    public readonly bool Success;
    public readonly ELevelPlayCommandError Error;
    public readonly int FocusedResourceId;
    public readonly ELevelPlayFocusKind FocusKind;
    public readonly int[] HighlightedResourceIdArray;
    public readonly int[] HighlightedConnectionIdArray;
    public readonly int[] ActiveBoardRuleIdArray;

    private ResourceFocusDTO(bool success,
                             ELevelPlayCommandError error,
                             int focusedResourceId,
                             ELevelPlayFocusKind focusKind,
                             IReadOnlyList<int> highlightedResourceIdList,
                             IReadOnlyList<int> highlightedConnectionIdList,
                             IReadOnlyList<int> activeBoardRuleIdList)
    {
        Success = success;
        Error = error;
        FocusedResourceId = focusedResourceId;
        FocusKind = focusKind;
        HighlightedResourceIdArray = CopyIdArray(highlightedResourceIdList);
        HighlightedConnectionIdArray = CopyIdArray(highlightedConnectionIdList);
        ActiveBoardRuleIdArray = CopyIdArray(activeBoardRuleIdList);
    }

    public static ResourceFocusDTO Ok(int focusedResourceId,
                                      ELevelPlayFocusKind focusKind,
                                      IReadOnlyList<int> highlightedResourceIdList,
                                      IReadOnlyList<int> highlightedConnectionIdList,
                                      IReadOnlyList<int> activeBoardRuleIdList)
    {
        return new ResourceFocusDTO(true,
                                    ELevelPlayCommandError.None,
                                    focusedResourceId,
                                    focusKind,
                                    highlightedResourceIdList,
                                    highlightedConnectionIdList,
                                    activeBoardRuleIdList);
    }

    public static ResourceFocusDTO Fail(ELevelPlayCommandError error)
    {
        return new ResourceFocusDTO(false,
                                    error,
                                    -1,
                                    ELevelPlayFocusKind.None,
                                    new int[0],
                                    new int[0],
                                    new int[0]);
    }

    private static int[] CopyIdArray(IReadOnlyList<int> idList)
    {
        int[] idArray = new int[idList.Count];

        for (int i = 0; i < idList.Count; i++)
        {
            idArray[i] = idList[i];
        }

        return idArray;
    }
}
