public sealed class RelayTransferRule : IBoardRule
{
    private readonly RelayRelation _relation;

    public int Id => _relation.Id;

    public RelayTransferRule(RelayRelation relation)
    {
        _relation = relation;
    }

    public bool HandlesResource(int resourceId)
    {
        return _relation.Contains(resourceId);
    }

    public bool CanReserve(ConnectionContext context, Board board)
    {
        if (context.Resource.Id == _relation.SenderResourceId)
        {
            return context.Resource.Rule.CanReserve(context);
        }

        return true;
    }

    public bool CanOccupy(ConnectionContext context, Board board)
    {
        return true;
    }

    public void OnOccupied(ConnectionContext context, Board board, RuleEffects effects)
    {
        if (context.Resource.Id != _relation.SenderResourceId)
        {
            return;
        }

        effects.SetRelayColor(_relation.GetOther(context.Resource.Id), context.Slot.RequiredColor);
    }

    public void OnReleased(ConnectionContext context, Board board, RuleEffects effects)
    {
        if (context.Resource.Id != _relation.SenderResourceId)
        {
            return;
        }

        effects.ClearRelayColor(_relation.GetOther(context.Resource.Id));
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
        return new RelayTransferRule(_relation);
    }

    public RelayStateSnapshot CreateStateSnapshot(Board board)
    {
        ResourceNode receiver = board.GetResource(_relation.GetOther(_relation.SenderResourceId));
        bool isActive = receiver is not null && receiver.HasRelayColor;

        return new RelayStateSnapshot(_relation.Id,
                                      _relation.FirstResourceId,
                                      _relation.SecondResourceId,
                                      _relation.RelayType,
                                      _relation.SenderResourceId,
                                      isActive);
    }
}
