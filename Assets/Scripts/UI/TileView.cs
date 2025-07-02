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
}