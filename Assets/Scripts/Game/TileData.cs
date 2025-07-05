using UnityEngine;

public enum TileType { Red, Blue, Green, Yellow, Purple }
public enum TilePower { None, RowClearer, ColumnClearer, Bomb, Rainbow }

public class TileData
{
    public TileType Type { get; set; }
    public TilePower Power { get; set; }
    public Vector2Int GridPosition { get; set; }

    public TileData(TileType type, Vector2Int pos, TilePower power = TilePower.None)
    {
        Type = type;
        GridPosition = pos;
        Power = power;
    }
}