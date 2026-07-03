public sealed class ConnectionContext
{
    public readonly ProcessNode Process;
    public readonly ProcessColorSlot Slot;
    public readonly ResourceNode Resource;

    public ConnectionContext(ProcessNode process, ProcessColorSlot slot, ResourceNode resource)
    {
        Process = process;
        Slot = slot;
        Resource = resource;
    }
}