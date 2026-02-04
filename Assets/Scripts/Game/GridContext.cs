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
    public IEffectService EffectService { get; set; }

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

    public void TriggerPower(TileData tile, TileType matchedWithTile, Vector3? cachedWorldPosition = null)
    {
        if (tile == null || tile.Power == TilePower.None)
            return;

        var behavior = TilePowerFactory.Get(tile.Power);
        
        // If cached world position is provided, temporarily store it for GetWorldPosition
        if (cachedWorldPosition.HasValue)
        {
            // Store the cached position temporarily
            if (!cachedPositions.ContainsKey(tile.GridPosition))
            {
                cachedPositions[tile.GridPosition] = cachedWorldPosition.Value;
            }
        }
        
        behavior?.Apply(tile.GridPosition, this, matchedWithTile);

        // Clear cached position after use
        if (cachedWorldPosition.HasValue)
        {
            cachedPositions.Remove(tile.GridPosition);
        }

        OnSpecialTileTriggered?.Invoke(tile);

        // Clear power after use to prevent repeat
        tile.Power = TilePower.None;
    }
    
    private Dictionary<Vector2Int, Vector3> cachedPositions = new Dictionary<Vector2Int, Vector3>();

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
    /// Gets the screen position for a grid position. Returns screen coordinates
    /// which will be converted to world space by EffectService based on Canvas render mode.
    /// </summary>
    public Vector3 GetWorldPosition(Vector2Int gridPos)
    {
        // Check for cached position first (used when view is destroyed but position is needed)
        if (cachedPositions.TryGetValue(gridPos, out Vector3 cachedPos))
        {
            return cachedPos;
        }
        
        if (GridController != null)
        {
            var view = Views[gridPos.x, gridPos.y];
            
            if (view != null)
            {
                RectTransform rect = view.transform as RectTransform;
                if (rect != null)
                {
                    // Get the center of the rect in screen space
                    Vector3[] corners = new Vector3[4];
                    rect.GetWorldCorners(corners);
                    
                    // Return center position in screen coordinates
                    return new Vector3(
                        (corners[0].x + corners[2].x) / 2f,
                        (corners[0].y + corners[2].y) / 2f,
                        0f
                    );
                }
            }
            
            // Fallback: use UI position directly
            Vector2 uiPos = GridController.GridToUIPos(gridPos);
            return new Vector3(uiPos.x, uiPos.y, 0f);
        }
        
        return new Vector3(gridPos.x, gridPos.y, 0f);
    }
}