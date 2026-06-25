using UnityEngine;
/// <summary>
/// Stub implementation of IGridController for testing CountPotentialMoves.
/// Only implements SwapTilesInData and MatchFinder - the two things CountPotentialMoves needs.
/// </summary>
public class StubGridController : IGridController
{
    private TileData[,] gridData;
    public MatchFinder MatchFinder { get; }

    public StubGridController(TileData[,] gridData, MatchFinder matchFinder)
    {
        this.gridData = gridData;
        this.MatchFinder = matchFinder;
    }

    public void SwapTilesInData(Vector2Int origin, Vector2Int target, TileData tileA, TileData tileB)
    {
        gridData[origin.x, origin.y] = tileB;
        gridData[target.x, target.y] = tileA;
        
        tileA.GridPosition = target;
        tileB.GridPosition = origin;
    }

    // Unused interface members - not needed for CountPotentialMoves test
    public bool IsBoardInitialized => false;
    public bool IsProcessingTiles => false;
    public GridContext GridContext => null;
    public BoardStateEvaluator BoardEvaluator => null;
    public event System.Action ActionTaken;
    public event System.Action TileMoved;
    public event System.Action TileSwapped;
    public event System.Action TileSwapError;
    public event System.Action<TileData> TileDestroyed;
    public event System.Action<TileData[,]> BoardUpdated;
    public event System.Action<TileData> PowerTileCreated;
    public event System.Action OnBoardShuffle;
    public void TrySwapTiles(Vector2Int origin, Vector2Int dir) { }
    public void AttemptPowerTrigger(TileView tileView) { }
    public Cysharp.Threading.Tasks.UniTask MatchCycleAsync() => Cysharp.Threading.Tasks.UniTask.CompletedTask;
    public Vector2 GridToUIPos(Vector2Int gridPos) => Vector2.zero;
    public void DestroyTargetTile(Vector2Int origin) { }
}
