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
    public Action OnDestroy { get; }
    public Action<TileData> OnSpecialTileTriggered;

    public GridContext(
        TileData[,] data,
        TileView[,] views,
        int width,
        int height,
        TilePoolManager pool,
        CommandInvoker invoker,
        Action onDestroy)
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
        return pos.x >= 0 && pos.x < Width && pos.y >= 0 && pos.y < Height;
    }

    public void TriggerPower(TileData tile)
    {
        if (tile == null || tile.Power == TilePower.None)
            return;

        var behavior = TilePowerFactory.Get(tile.Power);
        behavior?.Apply(tile.GridPosition, this);

        OnSpecialTileTriggered?.Invoke(tile);

        // Clear power after use to prevent repeat
        tile.Power = TilePower.None;
    }

    public void TriggerPowersIn(IEnumerable<Vector2Int> positions)
    {
        foreach (var pos in positions)
        {
            if (!IsInside(pos))
                continue;

            var data = Data[pos.x, pos.y];
            TriggerPower(data);
        }
    }

    public void TriggerTilePower(Vector2Int pos)
    {
        if (!IsInside(pos))
            return;

        var data = Data[pos.x, pos.y];
        TriggerPower(data);
    }

    public void DamageTiles(IEnumerable<Vector2Int> positions, int damage)
    {
        var toDestroy = new List<Vector2Int>();

        foreach (var pos in positions)
        {
            if (!IsInside(pos)) continue;

            var data = Data[pos.x, pos.y];
            if (data == null) continue;

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
                new DestroyCommand(toDestroy, Views, Data, Pool, OnDestroy, this)
            );
        }
    }
}