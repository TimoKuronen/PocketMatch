using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime board invariants to detect impossible or suspicious states.
/// </summary>
public static class BoardInvariants
{
    /// <summary>
    /// Checks for holes inside columns where a non-empty tile is above an empty cell.
    /// Ignores null/Empty cells at the very top of a column (they may be in the process of spawning).
    /// </summary>
    public static bool CheckNoColumnHoles(TileData[,] data, int width, int height, out string message, out List<Vector2Int> offendingCells)
    {
        offendingCells = new List<Vector2Int>();

        if (data == null)
        {
            message = "Grid data is null.";
            return false;
        }

        for (int x = 0; x < width; x++)
        {
            bool seenEmptyBelow = false;

            for (int y = 0; y < height; y++)
            {
                var cell = data[x, y];
                bool isEmpty = cell == null || cell.State == TileState.Empty;

                if (isEmpty)
                {
                    seenEmptyBelow = true;
                }
                else if (seenEmptyBelow && cell.State == TileState.Normal)
                {
                    offendingCells.Add(new Vector2Int(x, y));
                }
            }
        }

        if (offendingCells.Count > 0)
        {
            message = $"Found {offendingCells.Count} hole(s) where normal tiles sit above empty cells.";
            return false;
        }

        message = "No column holes detected.";
        return true;
    }
}

