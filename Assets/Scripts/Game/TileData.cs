using UnityEngine;

public enum TileType { Red, Blue, Green, Yellow, Purple }
public enum TileSpecialType { None, RowClear, ColumnClear, Bomb, ColorClear }

public class TileData
{
    public TileType Type { get; set; }
    public TileSpecialType SpecialType { get; set; }
    public Vector2Int GridPosition { get; set; }

    public TileData(TileType type, Vector2Int pos)
    {
        Type = type;
        GridPosition = pos;
    }
}