using UnityEngine;

public enum TileType { Red, Blue, Green, Yellow, Purple }

public class TileData
{
    public TileType Type { get; set; }
    public Vector2Int GridPosition { get; set; }

    public TileData(TileType type, Vector2Int pos)
    {
        Type = type;
        GridPosition = pos;
    }
}