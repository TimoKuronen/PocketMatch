using System;
using System.Collections.Generic;
using UnityEngine;

public static class GridHelperMethods
{
    #region Bounds Checking

    public static bool IsInBounds(int width, int height, int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    public static bool IsInBounds(int width, int height, Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
    }

    public static bool IsInsideGrid(Vector2Int pos, int width, int height) => IsInBounds(width, height, pos);

    #endregion

    #region Tile State Checking

    public static bool IsCellEmpty(TileData[,] gridData, int x, int y, int width, int height)
    {
        if (!IsInBounds(width, height, x, y))
            return false;

        var data = gridData[x, y];

        if (data == null || data.State == TileState.Empty)
            return true;
        if (data.State == TileState.Destroyable && data is DestroyableTileData d && d.IsDestroyed)
            return true;

        return false;
    }

    public static bool IsMovable(TileData[,] gridData, int x, int y, int width, int height)
    {
        if (!IsInBounds(width, height, x, y))
            return false;

        var data = gridData[x, y];
        return data != null && data.State == TileState.Normal;
    }

    #endregion

    #region Tile Creation

    public static TileData CreateEmptyTile(Vector2Int position)
    {
        return new TileData(TileType.Red, position, TilePower.None, TileState.Empty);
    }

    public static TileType GetRandomTileType(MapData mapData)
    {
        return mapData.AllowedTileColors[UnityEngine.Random.Range(0, mapData.AllowedTileColors.Length)];
    }

    #endregion

    #region Shuffle Logic

    /// <summary>
    /// Shuffles tile types in the grid until there are no matches and at least one potential move exists.
    /// </summary>
    public static bool ShuffleTypesUntilPlayable(
        TileData[,] gridData,
        int width,
        int height,
        MatchFinder matchFinder,
        System.Random rng = null,
        int maxAttempts = 150)
    {
        List<TileData> normalTiles = new List<TileData>();
        List<Vector2Int> tilePositions = new List<Vector2Int>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                TileData tile = gridData[x, y];
                if (tile != null && tile.State == TileState.Normal)
                {
                    normalTiles.Add(tile);
                    tilePositions.Add(new Vector2Int(x, y));
                }
            }
        }

        List<TileType> originalTypes = new List<TileType>();
        foreach (var tile in normalTiles)
        {
            originalTypes.Add(tile.Type);
        }

        int attempts = 0;
        bool hasMatches = true;
        bool hasPotentialMoves = false;

        while ((hasMatches || !hasPotentialMoves) && attempts < maxAttempts)
        {
            for (int i = 0; i < normalTiles.Count; i++)
            {
                normalTiles[i].Type = originalTypes[i];
            }

            // Shuffle
            for (int i = normalTiles.Count - 1; i > 0; i--)
            {
                int j;
                if (rng != null)
                    j = rng.Next(0, i + 1);
                else
                    j = UnityEngine.Random.Range(0, i + 1);
                
                (normalTiles[i].Type, normalTiles[j].Type) = (normalTiles[j].Type, normalTiles[i].Type);
            }

            // Apply shuffled types to grid
            for (int i = 0; i < normalTiles.Count; i++)
            {
                Vector2Int pos = tilePositions[i];
                gridData[pos.x, pos.y].Type = normalTiles[i].Type;
            }

            hasMatches = matchFinder.GetMatchGroups(gridData).Count > 0;
            hasPotentialMoves = HasPotentialMoves(gridData, width, height);
            attempts++;
        }

        return attempts < maxAttempts;
    }

    #endregion

    #region Potential Moves

    /// <summary>
    /// Returns true if the board has at least one possible move: any power tile present,
    /// or any pattern indicating a potential match-3 can be formed with a single swap.
    /// </summary>
    public static bool HasPotentialMoves(TileData[,] grid, int width, int height)
    {
        // Power tiles always count as a possible move
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var tile = grid[x, y];
                if (tile != null && tile.Power != TilePower.None)
                    return true;
            }
        }

        // Check for potential match patterns
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var tile = grid[x, y];
                if (tile == null || tile.State != TileState.Normal)
                    continue;

                TileType type = tile.Type;

                // Check horizontal patterns
                if (CheckHorizontalPotential(grid, width, height, x, y, type))
                    return true;

                // Check vertical patterns
                if (CheckVerticalPotential(grid, width, height, x, y, type))
                    return true;
            }
        }

        return false;
    }

    private static bool CheckHorizontalPotential(TileData[,] grid, int width, int height, int x, int y, TileType type)
    {
        // Pattern 1: Two in a row, check the gap at the end (x+2)
        if (IsInBounds(width, height, x + 1, y) && IsMatchable(grid[x + 1, y], type))
        {
            int gapX = x + 2;
            int gapY = y;
            if (IsInBounds(width, height, gapX, gapY))
            {
                // Only check neighbors that aren't the ones we already have
                if (IsMatchableAt(grid, width, height, gapX + 1, gapY, type)) return true;
                if (IsMatchableAt(grid, width, height, gapX, gapY + 1, type)) return true; 
                if (IsMatchableAt(grid, width, height, gapX, gapY - 1, type)) return true;
            }

            // Also check the OTHER end (x-1)
            int backGapX = x - 1;
            if (IsInBounds(width, height, backGapX, gapY))
            {
                if (IsMatchableAt(grid, width, height, backGapX - 1, gapY, type)) return true;
                if (IsMatchableAt(grid, width, height, backGapX, gapY + 1, type)) return true;
                if (IsMatchableAt(grid, width, height, backGapX, gapY - 1, type)) return true;
            }
        }

        // Pattern 2 - Gap in the middle
        if (IsInBounds(width, height, x + 2, y) && IsMatchable(grid[x + 2, y], type))
        {
            int gapX = x + 1;
            int gapY = y;
            // Only check above and below (checking left/right is redundant or wrong)
            if (IsMatchableAt(grid, width, height, gapX, gapY + 1, type)) return true;
            if (IsMatchableAt(grid, width, height, gapX, gapY - 1, type)) return true;
        }

        return false;
    }

    private static bool CheckVerticalPotential(TileData[,] grid, int width, int height, int x, int y, TileType type)
    {
        // Pattern 1: Vertical Pair
        if (IsInBounds(width, height, x, y + 1) && IsMatchable(grid[x, y + 1], type))
        {
            // Check gap ABOVE the pair
            int gapY = y + 2;
            if (IsInBounds(width, height, x, gapY))
            {
                if (IsMatchableAt(grid, width, height, x, gapY + 1, type)) return true; 
                if (IsMatchableAt(grid, width, height, x + 1, gapY, type)) return true;
                if (IsMatchableAt(grid, width, height, x - 1, gapY, type)) return true;
            }

            // Check gap BELOW the pair
            int backGapY = y - 1;
            if (IsInBounds(width, height, x, backGapY))
            {
                if (IsMatchableAt(grid, width, height, x, backGapY - 1, type)) return true;
                if (IsMatchableAt(grid, width, height, x + 1, backGapY, type)) return true; 
                if (IsMatchableAt(grid, width, height, x - 1, backGapY, type)) return true; 
            }
        }

        // Pattern 2: Vertical Gap
        if (IsInBounds(width, height, x, y + 2) && IsMatchable(grid[x, y + 2], type))
        {
            int gapY = y + 1;
            // Only need to check horizontal neighbors for the middle gap
            if (IsMatchableAt(grid, width, height, x + 1, gapY, type)) return true;
            if (IsMatchableAt(grid, width, height, x - 1, gapY, type)) return true;
        }

        return false;
    }

    /// <summary>
    /// Helper to check if a tile matches the given type and is matchable (normal state).
    /// </summary>
    private static bool IsMatchable(TileData tile, TileType type)
    {
        return tile != null && tile.State == TileState.Normal && tile.Type == type;
    }

    private static bool IsMatchableAt(TileData[,] grid, int w, int h, int x, int y, TileType type)
    {
        return IsInBounds(w, h, x, y) && IsMatchable(grid[x, y], type);
    }

    #endregion

    #region Other

    public static void UpdateTilePosition(TileData data, TileView view, Vector2Int newPosition)
    {
        data.GridPosition = newPosition;
        if (view.Data != null)
            view.Data.GridPosition = newPosition;
    }

    #endregion
}
