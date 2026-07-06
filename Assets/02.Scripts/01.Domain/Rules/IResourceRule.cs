public interface IResourceRule
{
    public bool CanReserve(ConnectionContext context);
    public bool CanOccupy(ConnectionContext context);
    public void OnOccupied(ConnectionContext context, RuleEffects effects);
    public bool CanFinish(ResourceNode resource);
    public void OnReleased(ResourceNode resource, RuleEffects effects);
    public void OnRoundEnded(ResourceNode resource, int round, RuleEffects effects);
    public IResourceRule Snapshot();
}
