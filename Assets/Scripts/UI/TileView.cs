using UnityEngine;
using UnityEngine.UI;

public class TileView : MonoBehaviour
{
    [SerializeField] private Image image;
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
    }

    private void UpdateColorOnDamage(int healthLeft)
    {
        image.color = Color.grey;
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
