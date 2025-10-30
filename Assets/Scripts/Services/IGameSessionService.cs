public interface IGameSessionService
{
    public MapData CurrentMapData { get; }
    public bool IsLevelDataLoaded { get; }
    public bool IsLevelCapReached { get; }
}
