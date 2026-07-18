using System;
using System.Collections.Generic;
using UnityEngine;

public class GridContext
{
    #region Properties

    public TileData[,] Data { get; }
    public TileView[,] Views { get; }
    public int Width { get; }
    public int Height { get; }
    public TilePoolManager Pool { get; }
    public CommandInvoker CommandInvoker { get; }
    public Action<TileData> OnDestroy { get; set; }
    public Action OnDestroyBatch { get; set; }
    public Action<TileData> OnSpecialTileTriggered;
    public IGridController GridController { get; set; }
    public IEffectService EffectService { get; set; }

    #endregion

    #region Fields

    private Dictionary<Vector2Int, Vector3> cachedPositions = new Dictionary<Vector2Int, Vector3>();

    #endregion

    #region Constructor

    public GridContext(
        TileData[,] data,
        TileView[,] views,
        int width,
        int height,
        TilePoolManager pool,
        CommandInvoker invoker,
        Action<TileData> onDestroy,
        Action onDestroyBatch = null)
    {
        Data = data;
        Views = views;
        Width = width;
        Height = height;
        Pool = pool;
        CommandInvoker = invoker;
        OnDestroy = onDestroy;
        OnDestroyBatch = onDestroyBatch;
    }

    #endregion

    #region Public Methods

    public bool IsInside(Vector2Int pos)
    {
        return GridHelperMethods.IsInBounds(Width, Height, pos);
    }

    public void TriggerPower(TileData tile, TileType matchedWithTile, Vector3? cachedWorldPosition = null)
    {
        if (tile == null || tile.Power == TilePower.None)
            return;

        var behavior = TilePowerFactory.Get(tile.Power);
        
        if (cachedWorldPosition.HasValue)
        {
            if (!cachedPositions.TryGetValue(tile.GridPosition, out _))
            {
                cachedPositions[tile.GridPosition] = cachedWorldPosition.Value;
            }
        }
        
        behavior?.Apply(tile.GridPosition, this, matchedWithTile);

        if (cachedWorldPosition.HasValue)
        {
            cachedPositions.Remove(tile.GridPosition);
        }

        OnSpecialTileTriggered?.Invoke(tile);
        tile.Power = TilePower.None;
    }

    public void TriggerTilePower(Vector2Int pos, TileType matchedWithTile)
    {
        if (!IsInside(pos))
            return;

        var data = Data[pos.x, pos.y];
        TriggerPower(data, matchedWithTile);
    }

    public void DamageTiles(IEnumerable<Vector2Int> positions, int damage, bool isFromPowerTile = false)
    {
        var toDestroy = new List<Vector2Int>();

        foreach (var pos in positions)
        {
            if (!IsInside(pos))
                continue;

            var data = Data[pos.x, pos.y];

            if (data == null)
                continue;

            if (data is IDamageableTile damageable)
            {
                damageable.TakeDamage(damage);

                if (damageable.IsDestroyed)
                    toDestroy.Add(pos);
            }
            else
            {
                toDestroy.Add(pos);
            }
        }

        if (toDestroy.Count > 0)
        {
            CommandInvoker.AddCommand(
                new DestroyCommand(toDestroy, Views, Data, Pool, OnDestroy, this, isFromPowerTile, OnDestroyBatch));
        }
    }

    public Vector3 GetWorldPosition(Vector2Int gridPos)
    {
        if (cachedPositions.TryGetValue(gridPos, out Vector3 cachedPos))
        {
            return cachedPos;
        }
        
        if (GridController != null)
        {
            var view = Views[gridPos.x, gridPos.y];
            
            if (view != null)
            {
                RectTransform rect = view.RectTransform;
                if (rect != null)
                {
                    Vector3[] corners = new Vector3[4];
                    rect.GetWorldCorners(corners);
                    
                    return new Vector3(
                        (corners[0].x + corners[2].x) / 2f,
                        (corners[0].y + corners[2].y) / 2f,
                        0f
                    );
                }
            }
            
            Vector2 uiPos = GridController.GridToUIPos(gridPos);
            return new Vector3(uiPos.x, uiPos.y, 0f);
        }
        
        return new Vector3(gridPos.x, gridPos.y, 0f);
    }

    #endregion
}
