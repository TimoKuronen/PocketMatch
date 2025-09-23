using UnityEngine;

public class TileView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private ColorPalette colorPalette;
    [SerializeField] private TileIconCollection icons;

    public TileState ViewKind { get; set; }
    [field: SerializeField] public TileData Data { get; private set; }

    private Color originalColor;

    public void Init(TileData data)
    {
        Data = data;

        originalColor = spriteRenderer.color;
        spriteRenderer.sprite = icons.GetIcon(data.Type, data.Power, data.State);

        // Unnormal tiles aren't assigned a color
        if (data.State != TileState.Normal)
        {
            if (data is DestroyableTileData destroyableData)
            {
                destroyableData.OnTakeDamage += UpdateColorOnDamage;
            }
            return;
        }

        // power tiles have their own color
        if (data.Power != TilePower.None)
        {
            spriteRenderer.color = colorPalette.PowerTileColor;
            return;
        }

        // Normal tiles are colored by type
        int colorIndex = (int)data.Type;
        if (colorIndex < 0 || colorIndex >= colorPalette.TileColors.Length)
        {
            Debug.LogWarning($"Invalid tile type index: {colorIndex}");
            return;
        }
        
        spriteRenderer.color = colorPalette.TileColors[colorIndex].Color;
    }

    private void UpdateColorOnDamage(int healthLeft)
    {
        spriteRenderer.color = colorPalette.DamagedColor;
    }

    private void OnDisable()
    {
        if (Data is DestroyableTileData destroyableData)
        {
            destroyableData.OnTakeDamage -= UpdateColorOnDamage;
        }

        spriteRenderer.color = originalColor;
    }
}