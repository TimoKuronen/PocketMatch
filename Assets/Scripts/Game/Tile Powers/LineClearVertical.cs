using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LineClearVertical : ITilePowerBehavior
{
    public void Apply(Vector2Int origin, GridContext context)
    {
        var column = Enumerable.Range(0, context.Height)
               .Select(y => new Vector2Int(origin.x, y))
               .ToList();

        context.CommandInvoker.AddCommand(
            new DestroyCommand(column, context.Views, context.Data, context.Pool, context.OnDestroy));
    }
}
