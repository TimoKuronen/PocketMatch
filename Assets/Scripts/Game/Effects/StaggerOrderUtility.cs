using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Builds distance-based waves of grid positions for staggered power VFX.
/// </summary>
public static class StaggerOrderUtility
{
    public enum LineAxis
    {
        Horizontal,
        Vertical
    }

    /// <summary>
    /// Groups line-clear targets by distance from origin along the clear axis.
    /// Origin (distance 0) fires first, then expanding rings outward.
    /// </summary>
    public static List<List<Vector2Int>> BuildLineWaves(
        IEnumerable<Vector2Int> targets,
        Vector2Int origin,
        LineAxis axis)
    {
        var buckets = new Dictionary<int, List<Vector2Int>>();

        foreach (var pos in targets)
        {
            int distance = axis == LineAxis.Horizontal
                ? Mathf.Abs(pos.x - origin.x)
                : Mathf.Abs(pos.y - origin.y);

            if (!buckets.TryGetValue(distance, out var list))
            {
                list = new List<Vector2Int>();
                buckets[distance] = list;
            }

            list.Add(pos);
        }

        var waves = new List<List<Vector2Int>>();

        foreach (var distance in buckets.Keys.OrderBy(d => d))
            waves.Add(buckets[distance]);

        return waves;
    }

    /// <summary>
    /// Groups rainbow targets by Chebyshev distance from origin.
    /// Origin (distance 0) fires first so activation reads immediately, then expanding rings.
    /// </summary>
    public static List<List<Vector2Int>> BuildRainbowWaves(
        IEnumerable<Vector2Int> targets,
        Vector2Int origin)
    {
        var buckets = new Dictionary<int, List<Vector2Int>>();

        foreach (var pos in targets)
        {
            int distance = Mathf.Max(Mathf.Abs(pos.x - origin.x), Mathf.Abs(pos.y - origin.y));

            if (!buckets.TryGetValue(distance, out var list))
            {
                list = new List<Vector2Int>();
                buckets[distance] = list;
            }

            list.Add(pos);
        }

        var waves = new List<List<Vector2Int>>();

        foreach (var distance in buckets.Keys.OrderBy(d => d))
            waves.Add(buckets[distance]);

        return waves;
    }
}
