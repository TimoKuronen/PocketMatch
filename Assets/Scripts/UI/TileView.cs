using UnityEngine;

public class TileView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private ColorPalette colorPalette;
    public TileData Data { get; private set; }

    public void Init(TileData data, Sprite sharedSprite)
    {
        spriteRenderer.sprite = sharedSprite;
        Data = data;

        int colorIndex = (int)data.Type;
        if (colorIndex < 0 || colorIndex >= colorPalette.TileColors.Length)
        {
            Debug.LogWarning($"Invalid tile type index: {colorIndex}");
            return;
        }

        spriteRenderer.color = colorPalette.TileColors[colorIndex].Color;
    }

    public void DebugAsSpecialTile()
    {
        if (Data == null)
        {
            Debug.LogWarning("TileData is not initialized.");
            return;
        }
        switch (Data.Power)
        {
            case TilePower.RowClearer:
                spriteRenderer.color = Color.yellow;
                break;
            case TilePower.ColumnClearer:
                spriteRenderer.color = Color.cyan;
                break;
            case TilePower.Bomb:
                spriteRenderer.color = Color.red;
                break;
            case TilePower.Rainbow:
                spriteRenderer.color = Color.magenta;
                break;
            default:
                //Debug.LogWarning("Tile does not have a special power.");
                break;
        }
       // Debug.Log($"Tile at {Data.GridPosition} has power: {Data.Power}");
    }
}