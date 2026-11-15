using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class AdjacentDamageProcessor
{
    private static readonly Vector2Int[] Directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

    // Each destroyable takes one hit per match group that touches it, however many of the group's tiles are adjacent.
    public static List<Vector2Int> GetAdjacentDestroyables(List<List<Vector2Int>> matchGroups, TileData[,] gridData)
    {
        var destroyedPositions = new HashSet<Vector2Int>();
        var hitByGroup = new HashSet<Vector2Int>();

        foreach (var group in matchGroups)
        {
            hitByGroup.Clear();

            foreach (var matchPos in group)
            {
                foreach (var dir in Directions)
                {
                    var neighbour = matchPos + dir;

                    if (!IsInside(gridData, neighbour) || !hitByGroup.Add(neighbour))
                        continue;

                    if (gridData[neighbour.x, neighbour.y] is DestroyableTileData destroyable && !destroyable.IsDestroyed)
                    {
                        destroyable.TakeDamage(1);
                        if (destroyable.IsDestroyed)
                            destroyedPositions.Add(neighbour);
                    }
                }
            }
        }

        return destroyedPositions.ToList();
    }

    private static bool IsInside(TileData[,] grid, Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < grid.GetLength(0) &&
               pos.y >= 0 && pos.y < grid.GetLength(1);
    }
}
