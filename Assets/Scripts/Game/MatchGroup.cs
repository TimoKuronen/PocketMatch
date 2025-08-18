using System.Collections.Generic;
using UnityEngine;

public class MatchGroup
{
    public TileType Type;
    public List<Vector2Int> Positions = new();
    public MatchGroup(TileType type) { Type = type; }
}

public enum MatchShape
{
    None,
    ThreeLine,
    FourHorizontal,
    FourVertical,
    FiveLine,
    TOrL
}
