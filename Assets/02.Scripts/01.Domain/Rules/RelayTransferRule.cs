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

    public bool CanConnect(ConnectionContext context, Board board)
    {
        return true;
    }

    public void OnConnected(ConnectionContext context, Board board, RuleEffects effects)
    {
        if(context.Resource.Id != _relation.SenderResourceId)
        {
            return;
        }

        effects.ActivateResource(_relation.GetOther(context.Resource.Id));
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
}