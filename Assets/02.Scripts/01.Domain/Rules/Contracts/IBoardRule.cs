public interface IBoardRule
{
    public int Id { get; }

    public bool HandlesResource(int resourceId);
    public bool CanReserve(ConnectionContext context, Board board);
    public bool CanOccupy(ConnectionContext context, Board board);
    public void OnOccupied(ConnectionContext context, Board board, RuleEffects effects);
    public void OnReleased(ConnectionContext context, Board board, RuleEffects effects);
    public void AddFocusInfo(int resourceId, ResourceFocusInfoBuilder builder);
    public IBoardRule Snapshot();
}
