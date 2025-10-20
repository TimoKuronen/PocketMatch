using UnityEngine;
using UnityEngine.UI;

public class TileView : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private ColorPalette colorPalette;
    [SerializeField] private TileIconCollection icons;

    public TileState ViewKind { get; set; }
    [field: SerializeField] public TileData Data { get; private set; }

    private Color originalColor;
    private RectTransform rectTransform;

    public void Init(TileData data)
    {
        Data = data;

        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        originalColor = image.color;
        image.sprite = icons.GetIcon(data.Type, data.Power, data.State);

        // Unnormal tiles aren’t assigned a color
        if (data.State != TileState.Normal)
        {
            if (data is DestroyableTileData destroyableData)
            {
                destroyableData.OnTakeDamage += UpdateColorOnDamage;
            }
            return;
        }

        // Power tiles have their own color
        //if (data.Power != TilePower.None)
        //{
        //    image.color = colorPalette.PowerTileColor;
        //    return;
        //}

        // Normal tiles are colored by type
        //int colorIndex = (int)data.Type;
        //if (colorIndex < 0 || colorIndex >= colorPalette.TileColors.Length)
        //{
        //    Debug.LogWarning($"Invalid tile type index: {colorIndex}");
        //    return;
        //}

        //image.color = colorPalette.TileColors[colorIndex].Color;
    }

    private void UpdateColorOnDamage(int healthLeft)
    {
        image.color = colorPalette.DamagedColor;
    }

    private void OnDisable()
    {
        if (Data is DestroyableTileData destroyableData)
        {
            destroyableData.OnTakeDamage -= UpdateColorOnDamage;
        }

        image.color = originalColor;
    }

    public void SetAnchoredPosition(Vector2 pos)
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        rectTransform.anchoredPosition = pos;
    }

    public Vector2 GetAnchoredPosition()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        return rectTransform.anchoredPosition;
    }
}
