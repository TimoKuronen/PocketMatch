using UnityEngine;

public static class GridHelperMethods
{
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

    public static bool IsInBounds(int width, int height, int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    public static bool IsInsideGrid(Vector2Int pos, int width, int height) => pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
}
