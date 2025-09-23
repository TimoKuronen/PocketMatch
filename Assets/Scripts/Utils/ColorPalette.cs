using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ColorPalette", menuName = "ScriptableObjects/ColorPalette", order = 1)]
public class ColorPalette : ScriptableObject
{
    [field: SerializeField] public TileColor[] TileColors { get; private set; }
    [field: SerializeField] public Color DamagedColor;
    [field: SerializeField] public Color PowerTileColor;
}

[Serializable]
public class TileColor
{
    public enum TileType
    {
        Red,
        Blue,
        Green,
        Yellow,
        Purple,
    }

    public TileType Type;
    public Color Color;
}