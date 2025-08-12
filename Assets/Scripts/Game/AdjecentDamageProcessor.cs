using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class AdjacentDamageProcessor
{
    public static List<Vector2Int> GetAdjacentDestroyables(List<List<Vector2Int>> matchGroups, TileData[,] gridData)
    {
        var damagedPositions = new HashSet<Vector2Int>();

        foreach (var group in matchGroups)
        {
            foreach (var matchPos in group)
            {
                foreach (var dir in Directions)
                {
                    var neighbour = matchPos + dir;

                    if (!IsInside(gridData, neighbour)) 
                        continue;

                    var data = gridData[neighbour.x, neighbour.y];
                    if (data is DestroyableTileData destroyable && !destroyable.IsDestroyed)
                    {
                        destroyable.TakeDamage(1);
                        if (destroyable.IsDestroyed)
                        {
                            damagedPositions.Add(neighbour);
                        }
                    }
                }
            }
        }

        return damagedPositions.ToList();
    }

    private static readonly Vector2Int[] Directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

    private static bool IsInside(TileData[,] grid, Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < grid.GetLength(0) &&
               pos.y >= 0 && pos.y < grid.GetLength(1);
    }
}