using System;
using UnityEngine;
using Cysharp.Threading.Tasks;

public interface IGridController
{
    // Properties
    bool IsBoardInitialized { get; }
    bool IsProcessingTiles { get; }
    MatchFinder MatchFinder { get; }
    GridContext GridContext { get; }
    BoardStateEvaluator BoardEvaluator { get; }

    // Events
    //event Action TileDrop;
    event Action ActionTaken;
    event Action TileMoved;
    event Action TileSwapped;
    event Action TileSwapError;
    event Action<TileData> TileDestroyed;
    event Action<TileData[,]> BoardUpdated;
    event Action<TileData> PowerTileCreated;
    event Action OnBoardShuffle;

    // Methods
    void TrySwapTiles(Vector2Int origin, Vector2Int dir);
    void AttemptPowerTrigger(TileView tileView);
    void SwapTilesInData(Vector2Int origin, Vector2Int target, TileData tileA, TileData tileB);
    UniTask MatchCycleAsync();
    Vector2 GridToUIPos(Vector2Int gridPos);
    void DestroyTargetTile(Vector2Int origin);
}