public sealed class DebugContext
{
    public ISaveService Save { get; }
    public IEconomyService Economy { get; }
    public IAdsService Ads { get; }
    public IDebugToolsService DebugTools { get; }

    public DebugContext(
        ISaveService save,
        IEconomyService economy,
        IAdsService ads,
        IDebugToolsService debugTools)
    {
        Save = save;
        Economy = economy;
        Ads = ads;
        DebugTools = debugTools;
    }
}