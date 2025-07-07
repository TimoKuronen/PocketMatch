using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RainbowTile : ITilePowerBehavior
{
    private readonly TileType? overrideType;

    public RainbowTile(TileType? forcedType = null)
    {
        overrideType = forcedType;
    }

    public void Apply(Vector2Int origin, GridContext context)
    {
        TileType targetType = overrideType ?? GetMostCommonType(context.Data, context.Width, context.Height);

        var toDestroy = new List<Vector2Int>();

        for (int x = 0; x < context.Width; x++)
        {
            for (int y = 0; y < context.Height; y++)
            {
                var data = context.Data[x, y];
                if (data != null && data.Type == targetType)
                {
                    toDestroy.Add(new Vector2Int(x, y));
                }
            }
        }

        context.CommandInvoker.AddCommand(
            new DestroyCommand(toDestroy, context.Views, context.Data, context.Pool, context.OnDestroy, context));
    }

    private TileType GetMostCommonType(TileData[,] data, int width, int height)
    {
        var counter = new Dictionary<TileType, int>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var tile = data[x, y];
                if (tile != null)
                {
                    if (!counter.ContainsKey(tile.Type))
                        counter[tile.Type] = 0;
                    counter[tile.Type]++;
                }
            }
        }

        return counter.OrderByDescending(kv => kv.Value).First().Key;
    }
}
