using System.Collections.Generic;
using UnityEngine;

public class RainbowTile : ITilePowerBehavior
{
    private readonly TileType? overrideType;

    public RainbowTile(TileType? forcedType = null)
    {
        overrideType = forcedType;
    }

    public void Apply(Vector2Int origin, GridContext context, TileType matchedWithTile)
    {
        // Spawn rainbow activation effect at the rainbow tile position
        if (context.EffectService != null)
        {
            Vector3 worldPos = context.GetWorldPosition(origin);
            context.EffectService.PlayEffect(EffectKeys.RainbowActivation, worldPos);
        }

        TileType targetType;

        if (matchedWithTile == TileType.None)
            targetType = overrideType ?? GetMostCommonType(context.Data, context.Width, context.Height);
        else targetType = matchedWithTile;

        Debug.Log($"RainbowTile activated, targeting {targetType}");

        var toDestroy = new List<Vector2Int>();

        for (int x = 0; x < context.Width; x++)
        {
            for (int y = 0; y < context.Height; y++)
            {
                var data = context.Data[x, y];
                if (data != null && data.State == TileState.Normal && data.Type == targetType)
                {
                    toDestroy.Add(new Vector2Int(x, y));
                }
            }
        }

        toDestroy.Add(origin); // Also destroy the rainbow tile itself

        context.CommandInvoker.AddCommand(
            new DestroyCommand(toDestroy, context.Views, context.Data, context.Pool, context.OnDestroy, context, isFromPowerTile: true));
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
                    if (tile.State == TileState.Blocked || tile.State == TileState.Destroyable)
                        continue;

                    if (counter.TryGetValue(tile.Type, out int count))
                        counter[tile.Type] = count + 1;
                    else
                        counter[tile.Type] = 1;
                }
            }
        }

        TileType mostCommon = TileType.Red;
        int maxCount = 0;
        foreach (var kvp in counter)
        {
            if (kvp.Value > maxCount)
            {
                maxCount = kvp.Value;
                mostCommon = kvp.Key;
            }
        }
        return mostCommon;
    }
}
