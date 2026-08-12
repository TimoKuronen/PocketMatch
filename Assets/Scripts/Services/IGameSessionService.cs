/// <summary>
/// Holds the active level's loaded <see cref="MapData"/> and whether the player has reached the final level.
/// </summary>
public interface IGameSessionService
{
    /// <summary>Addressable map asset for the current play session.</summary>
    MapData CurrentMapData { get; }

    /// <summary>True when the active level is the last one in the catalog.</summary>
    bool IsLevelCapReached { get; }
}
