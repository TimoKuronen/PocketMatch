using System;
using UnityEngine;

[CreateAssetMenu(fileName = "TileIconCollection", menuName = "ScriptableObjects/TileIconCollection", order = 1)]
public class TileIconCollection : ScriptableObject
{
    public MatchedTileIcon[] TileIcons;

    public Sprite GetIcon(TileType type, TilePower power, TileState state)
    {
        foreach (var icon in TileIcons)
        {
             if (icon.TileType == type && icon.TilePower == power && icon.TileState == state)
            {
                return icon.Icon;
            }
        }  
        
        Debug.LogWarning($"No icon found for Type: {type}, Power: {power}, State: {state}");
        return null;
    }
}

[Serializable]
public class MatchedTileIcon
{
    public TileType TileType;
    public TilePower TilePower;
    public TileState TileState;
    public Sprite Icon;
}
