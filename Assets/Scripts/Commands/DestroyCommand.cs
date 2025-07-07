using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class DestroyCommand : ICommand
{
    private readonly List<Vector2Int> matchPositions;
    private readonly TileView[,] gridViews;
    private readonly TileData[,] gridData;
    private readonly ObjectPool<TileView> pool;
    private readonly Action TileDestroyed;
    private readonly GridContext context;

    public DestroyCommand(List<Vector2Int> positions, TileView[,] views, TileData[,] data, 
        ObjectPool<TileView> pool, Action onDestroy, GridContext context = null)
    {
        matchPositions = positions;
        gridViews = views;
        gridData = data;
        this.pool = pool;
        TileDestroyed = onDestroy;

    //    Debug.Log($"DestroyCommand: Created with {matchPositions.Count} positions to destroy.");
    }

    public IEnumerator Execute()
    {
        context?.TriggerPowersIn(matchPositions);

        foreach (var pos in matchPositions)
        {
            var view = gridViews[pos.x, pos.y];
            if (view != null)
            {
                view.transform.DOKill();
                view.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack);
            }
        }

        TileDestroyed?.Invoke();

        yield return new WaitForSeconds(0.5f);

        foreach (var pos in matchPositions)
        {
            if (gridViews[pos.x, pos.y] != null)
            {
                pool.Release(gridViews[pos.x, pos.y]);
                gridViews[pos.x, pos.y] = null;
            }
           // Debug.Log($"DestroyCommand: Releasing tile to pool.");
            gridData[pos.x, pos.y] = null;
        }
    }
}

