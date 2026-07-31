using System.Linq;
using UnityEngine;

public class LineClearHorizontal : ITilePowerBehavior
{
    public void Apply(Vector2Int origin, GridContext context, TileType matchedWithTile)
    {
        var row = Enumerable.Range(0, context.Width)
                   .Select(x => new Vector2Int(x, origin.y))
                   .ToList();

        var targets = context.ResolveDamageTargets(row, 1);
        context.EnqueueStaggeredDestroy(targets, origin, StaggerOrderUtility.LineAxis.Horizontal);
    }
}
