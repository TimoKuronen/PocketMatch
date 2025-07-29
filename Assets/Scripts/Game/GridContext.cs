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

    public void TriggerPowersIn(IEnumerable<Vector2Int> positions)
    {
        foreach (var pos in positions)
            TriggerTilePower(pos);
    }

    public void TriggerTilePower(Vector2Int pos)
    {
        if (!IsInside(pos)) 
            return;

        var data = Data[pos.x, pos.y];
        if (data == null || data.Power == TilePower.None)
            return;

        var behavior = TilePowerFactory.Get(data.Power);
        behavior?.Apply(pos, this);
    }
}