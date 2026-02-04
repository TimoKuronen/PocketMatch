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

    private float destroyDuration = 0.2f;

    public DestroyCommand(
        List<Vector2Int> positions,
        TileView[,] views,
        TileData[,] data,
        TilePoolManager pool,
        Action<TileData> onDestroy,
        GridContext context = null,
        bool isFromPowerTile = false)
    {
        matchPositions = positions;
        gridViews = views;
        gridData = data;
        this.pool = pool;
        TileDestroyed = onDestroy;
        this.context = context;
        
        // Double duration if this destruction is from a power tile activation
        destroyDuration = isFromPowerTile ? 0.4f : 0.2f;
    }

    public IEnumerator Execute()
    {
        var powersToTrigger = new List<TileData>();
        var powerWorldPositions = new Dictionary<TileData, Vector3>();

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
                // Capture world position while view still exists
                if (context != null)
                {
                    Vector3 worldPos = context.GetWorldPosition(pos);
                    powerWorldPositions[data] = worldPos;
                }
            }
        }

        // --- Phase 2: play shrink animation for visuals and spawn effects ---
        foreach (var pos in matchPositions)
        {
            var view = gridViews[pos.x, pos.y];
            if (view != null && view.Data.State != TileState.Blocked)
            {
                view.transform.DOKill();
                view.transform.DOScale(Vector3.zero, destroyDuration).SetEase(Ease.InBack);

                // Spawn destroy effect for regular matches (not power tiles, as they have their own effects)
                // For future implementation, no effects now
                //if (context != null && context.EffectService != null && !isFromPowerTile)
                //{
                //    Vector3 worldPos = context.GetWorldPosition(pos);
                //    context.EffectService.PlayEffect(EffectKeys.TileDestroy, worldPos);
                //}
            }
        }

        yield return new WaitForSeconds(destroyDuration);

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
                    continue;

                gridData[pos.x, pos.y] = GridHelperMethods.CreateEmptyTile(pos);
            }
            else if (data.State == TileState.Normal)
            {
                gridData[pos.x, pos.y] = GridHelperMethods.CreateEmptyTile(pos);
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
            {
                // Use cached world position if available (view was destroyed)
                if (powerWorldPositions.TryGetValue(tile, out Vector3 cachedWorldPos))
                {
                    context.TriggerPower(tile, TileType.None, cachedWorldPos);
                }
                else
                {
                    context.TriggerPower(tile, TileType.None);
                }
            }
        }
    }
}
