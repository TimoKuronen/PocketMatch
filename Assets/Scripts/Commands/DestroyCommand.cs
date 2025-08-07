using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyCommand : ICommand
{
    private readonly List<Vector2Int> matchPositions;
    private readonly TileView[,] gridViews;
    private readonly TileData[,] gridData;
    private readonly TilePoolManager pool;
    private readonly Action TileDestroyed;
    private readonly GridContext context;

    public DestroyCommand(List<Vector2Int> positions, TileView[,] views, TileData[,] data,
        TilePoolManager pool, Action onDestroy, GridContext context = null)
    {
        matchPositions = positions;
        gridViews = views;
        gridData = data;
        this.pool = pool;
        TileDestroyed = onDestroy;
        this.context = context;
    }

    public IEnumerator Execute()
    {
        // Collect powers to trigger AFTER destroy finishes
        var powersToTrigger = new List<TileData>();

        foreach (var pos in matchPositions)
        {
            var data = gridData[pos.x, pos.y];
            if (data != null && data.State != TileState.Blocked && data.State != TileState.Destroyable && data.Power != TilePower.None)
            {
                powersToTrigger.Add(data);
            }
        }

        // Animate out any visuals
        foreach (var pos in matchPositions)
        {
            var view = gridViews[pos.x, pos.y];

            if (view != null)
            {
                if (view.Data.State == TileState.Blocked)
                    continue;

                view.transform.DOKill();
                view.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack);
            }
        }

        TileDestroyed?.Invoke();

        yield return new WaitForSeconds(0.5f);

        // Release visuals, mark slots as Empty
        foreach (var pos in matchPositions)
        {
            var view = gridViews[pos.x, pos.y];
            var data = gridData[pos.x, pos.y];

            if (data.State == TileState.Blocked || data.State == TileState.Destroyable)
                continue;

            if (view != null)
            {
                pool.Release(view, data.State);
                gridViews[pos.x, pos.y] = null;
            }

            if (data != null && data.State == TileState.Normal)
            {
                data.State = TileState.Empty; // Mark slot for refill
            }
        }

        //  trigger any powers
        if (context != null && powersToTrigger.Count > 0)
        {
            foreach (var tile in powersToTrigger)
            {
                context.TriggerPower(tile);
            }
        }
    }
}