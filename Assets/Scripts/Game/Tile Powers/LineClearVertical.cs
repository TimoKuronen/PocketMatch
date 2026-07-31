using System.Linq;
using UnityEngine;

public class LineClearVertical : ITilePowerBehavior
{
    public void Apply(Vector2Int origin, GridContext context, TileType matchedWithTile)
    {
        var column = Enumerable.Range(0, context.Height)
               .Select(y => new Vector2Int(origin.x, y))
               .ToList();

        var targets = context.ResolveDamageTargets(column, 1);
        context.EnqueueStaggeredDestroy(targets, origin, StaggerOrderUtility.LineAxis.Vertical);
    }
}
