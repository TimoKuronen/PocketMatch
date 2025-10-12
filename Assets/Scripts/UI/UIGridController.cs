using UnityEngine;
using UnityEngine.UI;

public class UIGridController : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int width = 6;
    [SerializeField] private int height = 8;
    [SerializeField] private UITileView tilePrefab;
    [SerializeField] private RectTransform tileGridContainer;
    [SerializeField] private float tileSpacing = 5f;

    private UITileView[,] uiTiles;
    private Vector2 tileSize;

    private void Start()
    {
        GenerateGrid();
    }

    private void GenerateGrid()
    {
        if (tilePrefab == null || tileGridContainer == null)
        {
            Debug.LogError("Missing references for UI grid generation.");
            return;
        }

        // Determine tile size based on container
        float gridWidth = tileGridContainer.rect.width;
        float gridHeight = tileGridContainer.rect.height;

        float availableWidth = gridWidth - ((width - 1) * tileSpacing);
        float availableHeight = gridHeight - ((height - 1) * tileSpacing);

        float tileWidth = availableWidth / width;
        float tileHeight = availableHeight / height;
        tileSize = new Vector2(tileWidth, tileHeight);

        uiTiles = new UITileView[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var tile = Instantiate(tilePrefab, tileGridContainer);
                uiTiles[x, y] = tile;

                var rt = tile.RectTransform;
                rt.sizeDelta = tileSize;

                Vector2 anchoredPos = new Vector2(
                    (x - width / 2f + 0.5f) * (tileSize.x + tileSpacing),
                    (y - height / 2f + 0.5f) * (tileSize.y + tileSpacing)
                );

                rt.anchoredPosition = anchoredPos;

                //tile.Initialize($"({x},{y})", Random.ColorHSV());
            }
        }
    }
}
