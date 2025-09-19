using UnityEngine;

public class TileView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private ColorPalette colorPalette;
    [SerializeField] private TileIconCollection icons;
    [SerializeField] private Color damagedColor;
    public TileState ViewKind { get; set; }
    [field: SerializeField] public TileData Data { get; private set; }
    private Color originalColor;

    public void Init(TileData data)
    {
        Data = data;

        originalColor = spriteRenderer.color;
        spriteRenderer.sprite = icons.GetIcon(data.Type, data.Power, data.State);

        if (data.State != TileState.Normal)
        {
            if (data is DestroyableTileData destroyableData)
            {
                originalColor = spriteRenderer.color;
                destroyableData.OnTakeDamage += UpdateColorOnDamage;
            }
            return;
        }

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
        spriteRenderer.color = damagedColor;
    }

    public void DebugAsSpecialTile()
    {
        if (Data == null)
        {
            Debug.LogWarning("TileData is not initialized.");
            return;
        }

        Debug.Log($"Debugging special tile with power: {Data.Power}");

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
                break;
        }
        // Debug.Log($"Tile at {Data.GridPosition} has power: {Data.Power}");
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