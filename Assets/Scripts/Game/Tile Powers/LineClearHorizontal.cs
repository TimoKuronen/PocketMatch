using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LineClearHorizontal : ITilePowerBehavior
{
    public void Apply(Vector2Int origin, GridContext context)
    {
        var row = Enumerable.Range(0, context.Width)
                   .Select(x => new Vector2Int(x, origin.y))
                   .ToList();

        context.CommandInvoker.AddCommand(
            new DestroyCommand(row, context.Views, context.Data, context.Pool, context.OnDestroy, context));
    }
}
