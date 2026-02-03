using UnityEngine;

public static class GridHelperMethods
{
    #region Debug

    public static string DebugBoard(TileData[,] grid)
    {
        int width = grid.GetLength(0);
        int height = grid.GetLength(1);
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        for (int y = height - 1; y >= 0; y--)
        {
            for (int x = 0; x < width; x++)
            {
                if (grid[x, y] == null)
                    sb.Append(" . ");
                else
                    sb.Append($"{(int)grid[x, y].Type} ");
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }

    #endregion

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

    #region Other

    public static void UpdateTilePosition(TileData data, TileView view, Vector2Int newPosition)
    {
        data.GridPosition = newPosition;
        if (view.Data != null)
            view.Data.GridPosition = newPosition;
    }

    public static RectTransform GetRectTransform(TileView view)
    {
        return (RectTransform)view.transform;
    }

    #endregion
}
