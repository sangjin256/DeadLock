using UnityEngine;

public interface IBoardRule
{
    public int Id { get; }

    public bool HandlesResource(int resourceId);
    public bool CanConnect(ConnectionContext context, Board board);
    public void OnConnected(ConnectionContext context, Board board, RuleEffects effects);
    public void AddFocusInfo(int resourceId, ResourceFocusInfoBuilder builder);
    public IBoardRule Snapshot();
}