using System;
using UnityEngine;

[CreateAssetMenu(fileName = "MapData", menuName = "ScriptableObjects/MapData", order = 1)]
public class MapData : ScriptableObject
{
    public int MoveLimit;
    public TileType[] AllowedTileColors;
    
    public VictoryConditions victoryConditions;
}

[Serializable]
public class VictoryConditions
{
    public TileMatch[] RequiredColorMatchCount;
    public int DestroyableTileCount;
}

[Serializable]
public class TileMatch
{
    public TileType TileColor;
    public int TileCount;
}