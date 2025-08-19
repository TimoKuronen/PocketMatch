using System;
using UnityEngine;

[CreateAssetMenu(fileName = "MapData", menuName = "ScriptableObjects/MapData", order = 1)]
public class MapData : ScriptableObject
{
    [Header("Map Layout")]
    public int width = 6;
    public int height = 8;
    public TileDataEditorView[] tiles;

    public TileDataEditorView GetTile(int x, int y) => tiles[y * width + x];
    public void SetTile(int x, int y, TileDataEditorView data) => tiles[y * width + x] = data;

    [Header("Victory Related Data")]
    public int MoveLimit;
    public TileType[] AllowedTileColors;
    
    public VictoryConditions VictoryConditions;
}

[Serializable]
public class VictoryConditions
{
    public int MoveLimit;
    public TileMatch[] RequiredColorMatchCount;
    public int DestroyableTileCount;
}

[Serializable]
public class TileMatch
{
    public TileType TileColor;
    public int TileCount;
}


[Serializable]
public class TileDataEditorView
{
    public TilePower tilePower;
    public bool isDestroyable;
    public bool isBlocked;
    public int hitPoints = 1;
}