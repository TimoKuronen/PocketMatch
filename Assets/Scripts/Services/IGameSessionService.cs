public interface IGameSessionService : IService
{
    public MapData CurrentMapData { get; }
    public bool IsLevelDataLoaded { get; }
}
