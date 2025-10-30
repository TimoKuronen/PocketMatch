public interface IGameSessionService
{
    public MapData CurrentMapData { get; }
    public bool IsLevelCapReached { get; }
}
