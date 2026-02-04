using System.Collections.Generic;
using UnityEngine;

public class BombTile : ITilePowerBehavior
{
    public void Apply(Vector2Int origin, GridContext context, TileType matchedWithTile)
    {
        // Spawn explosion effect at the bomb position
        if (context.EffectService != null)
        {
            Vector3 worldPos = context.GetWorldPosition(origin);
            context.EffectService.PlayEffect(EffectKeys.BombExplosion, worldPos);
        }

        var area = new List<Vector2Int>();
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                var pos = origin + new Vector2Int(dx, dy);
                if (context.IsInside(pos))
                    area.Add(pos);
            }
        }

        context.DamageTiles(area, 1, isFromPowerTile: true);
        BoardShaker.Instance.RequestShake(new BoardShakeData
        {
            Intensity = 4f,
            Duration = 0.3f
        });
    }
}