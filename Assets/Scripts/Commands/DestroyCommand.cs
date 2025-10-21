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
    private readonly Action<TileData> TileDestroyed;
    private readonly GridContext context;

    public DestroyCommand(
        List<Vector2Int> positions,
        TileView[,] views,
        TileData[,] data,
        TilePoolManager pool,
        Action<TileData> onDestroy,
        GridContext context = null)
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
        var powersToTrigger = new List<TileData>();

        // --- Phase 1: collect any power tiles that will trigger after destruction ---
        foreach (var pos in matchPositions)
        {
            var data = gridData[pos.x, pos.y];
            if (data != null &&
                data.State != TileState.Blocked &&
                data.State != TileState.Destroyable &&
                data.Power != TilePower.None)
            {
                powersToTrigger.Add(data);
            }
        }

        // --- Phase 2: play shrink animation for visuals ---
        foreach (var pos in matchPositions)
        {
            var view = gridViews[pos.x, pos.y];
            if (view != null && view.Data.State != TileState.Blocked)
            {
                view.transform.DOKill();
                view.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack);
            }
        }

        yield return new WaitForSeconds(0.5f);

        // --- Phase 3: actually destroy tiles and update data ---
        foreach (var pos in matchPositions)
        {
            var view = gridViews[pos.x, pos.y];
            var data = gridData[pos.x, pos.y];

            if (data == null)
                continue;

            TileDestroyed?.Invoke(data);

            // Skip intact blockers
            if (data.State == TileState.Blocked)
                continue;

            // Handle destroyables properly
            if (data.State == TileState.Destroyable && data is DestroyableTileData destroyable)
            {
                if (!destroyable.IsDestroyed)
                    continue; // not destroyed yet, skip this one

                gridData[pos.x, pos.y] = new TileData(TileType.Red, pos, TilePower.None, TileState.Empty);
            }
            else if (data.State == TileState.Normal)
            {
                // Normal tiles become empty too
                gridData[pos.x, pos.y] = new TileData(TileType.Red, pos, TilePower.None, TileState.Empty);
            }

            // Release the visual
            if (view != null)
            {
                pool.Release(view);
                gridViews[pos.x, pos.y] = null;
            }
        }

        // --- Phase 4: trigger powers if any were destroyed ---
        if (context != null && powersToTrigger.Count > 0)
        {
            foreach (var tile in powersToTrigger)
                context.TriggerPower(tile);
        }
    }
}
