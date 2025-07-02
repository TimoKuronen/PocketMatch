using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchGroup
{
    public List<TileData> Tiles;
    public MatchShape Shape;

    public MatchGroup(List<TileData> tiles, MatchShape shape)
    {
        Tiles = tiles;
        Shape = shape;
    }
}

public enum MatchShape
{
    None,
    ThreeLine,
    FourLine,
    FiveLine,
    TOrL
}
