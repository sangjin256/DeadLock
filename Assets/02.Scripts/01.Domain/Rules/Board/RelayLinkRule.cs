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

    public bool CanReserve(ConnectionContext context, Board board)
    {
        return context.Resource.Rule.CanReserve(context);
    }

    public bool CanOccupy(ConnectionContext context, Board board)
    {
        int otherId = _relation.GetOther(context.Resource.Id);
        ResourceNode otherResource = board.GetResource(otherId);

        return otherResource is not null && otherResource.OccupiedConnectionIdList.Count == 0;
    }

    public void OnOccupied(ConnectionContext context, Board board, RuleEffects effects)
    {
        effects.LockResource(_relation.GetOther(context.Resource.Id));
    }

    public void OnReleased(ConnectionContext context, Board board, RuleEffects effects)
    {
        effects.UnlockResource(_relation.GetOther(context.Resource.Id));
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

    public RelayStateSnapshot CreateStateSnapshot(Board board)
    {
        ResourceNode firstResource = board.GetResource(_relation.FirstResourceId);
        ResourceNode secondResource = board.GetResource(_relation.SecondResourceId);
        bool isActive = (firstResource is not null && firstResource.OccupiedConnectionIdList.Count > 0) ||
                        (secondResource is not null && secondResource.OccupiedConnectionIdList.Count > 0);

        return new RelayStateSnapshot(_relation.Id,
                                      _relation.FirstResourceId,
                                      _relation.SecondResourceId,
                                      _relation.RelayType,
                                      _relation.SenderResourceId,
                                      isActive);
    }
}
