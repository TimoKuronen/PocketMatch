using System;
using System.Collections.Generic;
using UnityEngine;

public class GridContext
{
    public TileData[,] Data { get; }
    public TileView[,] Views { get; }
    public int Width { get; }
    public int Height { get; }
    public TilePoolManager Pool { get; }
    public CommandInvoker CommandInvoker { get; }
    public Action<TileData> OnDestroy { get; set; }
    public Action<TileData> OnSpecialTileTriggered;
    public IGridController GridController { get; set; }

    public GridContext(
        TileData[,] data,
        TileView[,] views,
        int width,
        int height,
        TilePoolManager pool,
        CommandInvoker invoker,
        Action<TileData> onDestroy)
    {
        Data = data;
        Views = views;
        Width = width;
        Height = height;
        Pool = pool;
        CommandInvoker = invoker;
        OnDestroy = onDestroy;
    }

    public bool IsInside(Vector2Int pos)
    {
        return GridHelperMethods.IsInBounds(Width, Height, pos);
    }

    public void TriggerPower(TileData tile, TileType matchedWithTile)
    {
        if (tile == null || tile.Power == TilePower.None)
            return;

        var behavior = TilePowerFactory.Get(tile.Power);
        behavior?.Apply(tile.GridPosition, this, matchedWithTile);

        OnSpecialTileTriggered?.Invoke(tile);

        // Clear power after use to prevent repeat
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
                new DestroyCommand(toDestroy, Views, Data, Pool, OnDestroy, this, isFromPowerTile));
        }
    }

    /// <summary>
    /// Gets the world position for a grid position. Useful for spawning particle effects.
    /// </summary>
    public Vector3 GetWorldPosition(Vector2Int gridPos)
    {
        if (GridController != null)
        {
            // Convert UI position to world position
            Vector2 uiPos = GridController.GridToUIPos(gridPos);
            var view = Views[gridPos.x, gridPos.y];
            
            if (view != null)
            {
                // Use the tile's world position if available
                RectTransform rect = view.transform as RectTransform;
                if (rect != null)
                {
                    // Convert UI anchored position to world position
                    return rect.position;
                }
            }
            
            // Fallback: convert UI position assuming Canvas is Screen Space - Overlay
            // This might need adjustment based on your Canvas setup
            return new Vector3(uiPos.x, uiPos.y, 0f);
        }
        
        // Fallback if GridController is not set
        return new Vector3(gridPos.x, gridPos.y, 0f);
    }
}