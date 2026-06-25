using System;

/// <summary>
/// Runtime hooks raised by game code. Editor assembly subscribes and handles logging.
/// No-op in player builds when nothing is subscribed.
/// </summary>
public static class BoardDebugHooks
{
    public static event Action<GridController, TileData[,]> BoardInitialized;
    public static event Action<GridController, TileData[,]> BoardUpdated;
    public static event Action<GridController, TileData[,]> BoardShuffled;

    public static void NotifyBoardInitialized(GridController controller, TileData[,] data)
    {
        BoardInitialized?.Invoke(controller, data);
    }

    public static void NotifyBoardUpdated(GridController controller, TileData[,] data)
    {
        BoardUpdated?.Invoke(controller, data);
    }

    public static void NotifyBoardShuffled(GridController controller, TileData[,] data)
    {
        BoardShuffled?.Invoke(controller, data);
    }
}
