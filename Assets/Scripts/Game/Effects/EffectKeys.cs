/// <summary>
/// Centralized constants for Addressable effect keys.
/// These must match the addresses assigned in the Addressables Groups window.
/// </summary>
public static class EffectKeys
{
    // Tile Power Effects
    public const string RainbowActivation = "Effects/RainbowActivation";
    public const string BombExplosion = "Effects/BombExplosion";
    public const string LineClearHorizontal = "Effects/LineClearHorizontal";
    public const string LineClearVertical = "Effects/LineClearVertical";
    
    // General Effects
    public const string TileDestroy = "Effects/TileDestroy";
    public const string TileMatch = "Effects/TileMatch";
    
    // Label for bulk preloading
    public const string EffectsLabel = "Effects";
    
    // Array of all effect keys for easy preloading
    public static readonly string[] AllEffects = new[]
    {
        RainbowActivation,
        BombExplosion,
        LineClearHorizontal,
        LineClearVertical,
        TileDestroy,
        TileMatch
    };
}
