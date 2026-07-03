public interface IResourceRule
{
    public bool CanConnect(ConnectionContext context);
    public void OnConnected(ConnectionContext context, RuleEffects effects);
    public bool CanFinish(ResourceNode resource);
    public void OnRoundEnded(ResourceNode resource, int round, RuleEffects effects);
    public IResourceRule Snapshot();
}