using UnityEngine;

public class TileView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    private static readonly Color[] TileColors = new Color[]
    {
        Color.red,
        Color.blue,
        Color.green,
        Color.yellow,
        new Color(0.5f, 0f, 1f) // Purple
    };

    public void Init(TileData data, Sprite sharedSprite)
    {
        spriteRenderer.sprite = sharedSprite;

        int colorIndex = (int)data.Type;
        if (colorIndex < 0 || colorIndex >= TileColors.Length)
        {
            Debug.LogWarning($"Invalid tile type index: {colorIndex}");
            return;
        }

        spriteRenderer.color = TileColors[colorIndex];
    }
}