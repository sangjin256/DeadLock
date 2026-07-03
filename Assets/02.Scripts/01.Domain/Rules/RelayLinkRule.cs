public sealed class RelayLinkRule : IBoardRule
{
    private readonly RelayRelation _relation;
    public int Id => _relation.Id;

    public RelayLinkRule(RelayRelation relation)
    {
        _relation = relation;
    }

    public bool HandlesResource(int resourceId)
    {
        return _relation.Contains(resourceId);
    }

    public bool CanConnect(ConnectionContext context, Board board)
    {
        int otherId = _relation.GetOther(context.Resource.Id);
        return board.GetResource(otherId).ConnectionIdList.Count == 0;
    }

    public void OnConnected(ConnectionContext context, Board board, RuleEffects effects)
    {
        effects.LockResource(_relation.GetOther(context.Resource.Id));
    }

    public void AddFocusInfo(int resourceId, ResourceFocusInfoBuilder builder)
    {
        builder.SetKind(EFocusKind.Pair);
        builder.HighlightResource(_relation.FirstResourceId);
        builder.HighlightResource(_relation.SecondResourceId);
        builder.ActivateBoardRule(Id);
    }

    public IBoardRule Snapshot()
    {
        return new RelayLinkRule(_relation);
    }
}