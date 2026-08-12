using System;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// Match-3 board orchestrator: input, swap validation, match resolution, and gravity cycles.
/// </summary>
public interface IGridController
{
    bool IsBoardInitialized { get; }

    /// <summary>True while matches, gravity, or power resolution is in flight; menus should stay closed.</summary>
    bool IsProcessingTiles { get; }

    MatchFinder MatchFinder { get; }
    GridContext GridContext { get; }
    BoardStateEvaluator BoardEvaluator { get; }

    event Action TileDrop;
    event Action ActionTaken;
    event Action TileMoved;
    event Action TileSwapped;
    event Action TileSwapError;
    event Action<TileData> TileDestroyed;
    event Action TilesDestroyed;
    event Action<TileData[,]> BoardUpdated;
    event Action<TileData> PowerTileCreated;
    event Action OnBoardShuffle;

    void TrySwapTiles(Vector2Int origin, Vector2Int dir);
    void AttemptPowerTrigger(TileView tileView);
    void SwapTilesInData(Vector2Int origin, Vector2Int target, TileData tileA, TileData tileB);

    /// <summary>Runs match detection and resolution until the board reaches a stable state.</summary>
    UniTask MatchCycleAsync();

    Vector2 GridToUIPos(Vector2Int gridPos);
    void DestroyTargetTile(Vector2Int origin);
}
