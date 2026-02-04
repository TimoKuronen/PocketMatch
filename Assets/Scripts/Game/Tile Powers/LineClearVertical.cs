using System.Linq;
using UnityEngine;

public class LineClearVertical : ITilePowerBehavior
{
    public void Apply(Vector2Int origin, GridContext context, TileType matchedWithTile)
    {
        // Spawn line clear effect at the tile position
        if (context.EffectService != null)
        {
            Vector3 worldPos = context.GetWorldPosition(origin);
            context.EffectService.PlayEffect(EffectKeys.LineClearVertical, worldPos);
        }

        var column = Enumerable.Range(0, context.Height)
               .Select(y => new Vector2Int(origin.x, y))
               .ToList();

        context.DamageTiles(column, 1, isFromPowerTile: true);
    }
}
