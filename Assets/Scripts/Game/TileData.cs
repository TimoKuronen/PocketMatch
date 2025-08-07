using System;
using UnityEngine;

public enum TileType { Red, Blue, Green, Yellow, Purple }
public enum TilePower { None, RowClearer, ColumnClearer, Bomb, Rainbow }
public enum TileState { Normal, Blocked, Destroyable, Empty }

[Serializable]
public class TileData
{
    [field: SerializeField] public TileType Type { get; set; }
    [field: SerializeField] public TilePower Power { get; set; }
    [field: SerializeField] public Vector2Int GridPosition { get; set; }
    [field: SerializeField] public TileState State { get; set; }

    public TileData(TileType type, Vector2Int pos, TilePower power = TilePower.None, TileState state = TileState.Normal)
    {
        Type = type;
        GridPosition = pos;
        Power = power;
        State = state;
    }
}