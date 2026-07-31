using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// Destroys tiles in timed waves: each wave plays a mini explosion and starts shrink,
/// covering all waves within spreadDuration before grid data is cleared.
/// </summary>
public class StaggeredDestroyCommand : ICommand
{
    private readonly List<List<Vector2Int>> waves;
    private readonly TileView[,] gridViews;
    private readonly TileData[,] gridData;
    private readonly TilePoolManager pool;
    private readonly Action<TileData> tileDestroyed;
    private readonly Action onDestroyBatch;
    private readonly GridContext context;
    private readonly float spreadDuration;
    private readonly float destroyDuration;
    private readonly float scaleJitterMin;
    private readonly float scaleJitterMax;

    public StaggeredDestroyCommand(
        List<List<Vector2Int>> waves,
        TileView[,] views,
        TileData[,] data,
        TilePoolManager pool,
        Action<TileData> onDestroy,
        GridContext context,
        float spreadDuration,
        float destroyDuration,
        float scaleJitterMin = 0.85f,
        float scaleJitterMax = 1f,
        Action onDestroyBatch = null)
    {
        this.waves = waves ?? new List<List<Vector2Int>>();
        gridViews = views;
        gridData = data;
        this.pool = pool;
        tileDestroyed = onDestroy;
        this.context = context;
        this.spreadDuration = Mathf.Max(0f, spreadDuration);
        this.destroyDuration = Mathf.Max(0.01f, destroyDuration);
        this.scaleJitterMin = scaleJitterMin;
        this.scaleJitterMax = scaleJitterMax;
        this.onDestroyBatch = onDestroyBatch;
    }

    public async UniTask ExecuteAsync()
    {
        var matchPositions = FlattenWaves(waves);
        if (matchPositions.Count == 0)
            return;

        var powersToTrigger = new List<TileData>();
        var powerWorldPositions = new Dictionary<TileData, Vector3>();
        var positionsToVisuallyDestroy = new HashSet<Vector2Int>();

        // --- Phase 1: collect chain powers and visual targets ---
        foreach (var pos in matchPositions)
        {
            var data = gridData[pos.x, pos.y];
            if (data != null &&
                data.State != TileState.Blocked &&
                data.State != TileState.Destroyable &&
                data.Power != TilePower.None)
            {
                powersToTrigger.Add(data);
                if (context != null)
                    powerWorldPositions[data] = context.GetWorldPosition(pos);
            }

            if (data != null)
            {
                if (data.State == TileState.Normal)
                    positionsToVisuallyDestroy.Add(pos);
                else if (data.State == TileState.Destroyable &&
                         data is DestroyableTileData destroyableData &&
                         destroyableData.IsDestroyed)
                {
                    positionsToVisuallyDestroy.Add(pos);
                }
            }
        }

        if (positionsToVisuallyDestroy.Count > 0)
            onDestroyBatch?.Invoke();

        // --- Phase 2: staggered mini VFX + shrink ---
        int waveCount = waves.Count;
        float waveInterval = waveCount <= 1
            ? 0f
            : spreadDuration / (waveCount - 1);

        float elapsed = 0f;

        for (int i = 0; i < waveCount; i++)
        {
            float targetTime = i * waveInterval;
            float wait = targetTime - elapsed;
            if (wait > 0f)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(wait));
                elapsed = targetTime;
            }

            foreach (var pos in waves[i])
            {
                if (!positionsToVisuallyDestroy.Contains(pos))
                    continue;

                if (context?.EffectService != null)
                {
                    float scale = UnityEngine.Random.Range(scaleJitterMin, scaleJitterMax);
                    Vector3 worldPos = context.GetWorldPosition(pos);
                    context.EffectService.PlayEffect(EffectKeys.MiniExplosion, worldPos, default, scale);
                }

                var view = gridViews[pos.x, pos.y];
                if (view != null)
                {
                    view.transform.DOKill();
                    view.transform.DOScale(Vector3.zero, destroyDuration).SetEase(Ease.InBack);
                }
            }
        }

        await UniTask.Delay(TimeSpan.FromSeconds(destroyDuration));

        // --- Phase 3: clear grid data and release views ---
        foreach (var pos in matchPositions)
        {
            var view = gridViews[pos.x, pos.y];
            var data = gridData[pos.x, pos.y];

            if (data == null)
                continue;

            tileDestroyed?.Invoke(data);

            if (data.State == TileState.Blocked)
                continue;

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

            if (view != null)
            {
                pool.Release(view);
                gridViews[pos.x, pos.y] = null;
            }
        }

        // --- Phase 4: chain-trigger collected powers ---
        if (context != null && powersToTrigger.Count > 0)
        {
            foreach (var tile in powersToTrigger)
            {
                if (powerWorldPositions.TryGetValue(tile, out Vector3 cachedWorldPos))
                    context.TriggerPower(tile, TileType.None, cachedWorldPos);
                else
                    context.TriggerPower(tile, TileType.None);
            }
        }
    }

    private static List<Vector2Int> FlattenWaves(List<List<Vector2Int>> sourceWaves)
    {
        var result = new List<Vector2Int>();
        var seen = new HashSet<Vector2Int>();

        foreach (var wave in sourceWaves)
        {
            if (wave == null)
                continue;

            foreach (var pos in wave)
            {
                if (seen.Add(pos))
                    result.Add(pos);
            }
        }

        return result;
    }
}
