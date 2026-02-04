using System.Linq;
using UnityEngine;

public class LineClearHorizontal : ITilePowerBehavior
{
    public void Apply(Vector2Int origin, GridContext context, TileType matchedWithTile)
    {
        // Spawn line clear effect at the tile position
        if (context.EffectService != null)
        {
            Vector3 worldPos = context.GetWorldPosition(origin);
            context.EffectService.PlayEffect(EffectKeys.LineClearHorizontal, worldPos);
        }

        var row = Enumerable.Range(0, context.Width)
                   .Select(x => new Vector2Int(x, origin.y))
                   .ToList();

        context.DamageTiles(row, 1, isFromPowerTile: true);
    }
}
