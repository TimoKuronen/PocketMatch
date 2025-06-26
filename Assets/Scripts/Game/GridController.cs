using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GridController : MonoBehaviour
{
    [SerializeField] private int width = 6;
    [SerializeField] private int height = 8;
    [SerializeField] private TileView tilePrefab;
    [SerializeField] private Sprite sharedTileSprite;
    [SerializeField] private float tileSize = 1f;
    [SerializeField] private Vector3 gridOffset;

    private TileData[,] gridData;
    private TileView[,] gridViews;

    private void Start()
    {
        GenerateGrid();
        CenterCameraOnGrid();
    }

    private void Update()
    {
        if (Keyboard.current.aKey.wasPressedThisFrame)
        {
            List<TileData> list = GetAllMatches(3);
            Debug.Log(list.Count);
        }
    }

    private void GenerateGrid()
    {
        gridData = new TileData[width, height];
        gridViews = new TileView[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var position = new Vector2Int(x, y);
                var type = (TileType)Random.Range(0, System.Enum.GetValues(typeof(TileType)).Length);
                var data = new TileData(type, position);

                var view = Instantiate(tilePrefab, GridToWorldPos(position), Quaternion.identity, transform);
                view.Init(data, sharedTileSprite);

                gridData[x, y] = data;
                gridViews[x, y] = view;
            }
        }
        tileSize = tilePrefab.GetComponent<SpriteRenderer>().bounds.size.x;
        Debug.Log($"Grid generated with size: {width}x{height}, Tile Size: {tileSize}");
    }

    private Vector3 GridToWorldPos(Vector2Int gridPosition)
    {
        return new Vector3(gridPosition.x * tileSize, gridPosition.y * tileSize, 0f) + gridOffset;
    }

    private void CenterCameraOnGrid()
    {
        float gridWidth = width * tileSize;
        float gridHeight = height * tileSize;
        Vector3 centerPos = new Vector3(gridWidth / 2f - tileSize / 2f, gridHeight / 2f - tileSize / 2f, -10f);

        Camera.main.transform.position = centerPos;
        Camera.main.orthographic = true;
        Camera.main.orthographicSize = Mathf.Max(gridWidth, gridHeight) - 1;
    }

    // Detect all matches on the board, returns list of matching TileData
    public List<TileData> GetAllMatches(int matchLength = 3)
    {
        Debug.Log($"Searching for matches of length {matchLength} in a grid of size {width}x{height}.");
        List<TileData> matchedTiles = new List<TileData>();

        // Horizontal matches
        for (int y = 0; y < height; y++)
        {
            int matchCount = 1;
            for (int x = 1; x < width; x++)
            {
                if (gridData[x, y].Type == gridData[x - 1, y].Type)
                {
                    matchCount++;
                }
                else
                {
                    if (matchCount >= matchLength)
                    {
                        for (int i = 0; i < matchCount; i++)
                        {
                            matchedTiles.Add(gridData[x - 1 - i, y]);
                        }
                    }
                    matchCount = 1;
                }
            }
            // Check end of row
            if (matchCount >= matchLength)
            {
                for (int i = 0; i < matchCount; i++)
                {
                    matchedTiles.Add(gridData[width - 1 - i, y]);
                }
            }
        }

        // Vertical matches
        for (int x = 0; x < width; x++)
        {
            int matchCount = 1;
            for (int y = 1; y < height; y++)
            {
                if (gridData[x, y].Type == gridData[x, y - 1].Type)
                {
                    matchCount++;
                }
                else
                {
                    if (matchCount >= matchLength)
                    {
                        for (int i = 0; i < matchCount; i++)
                        {
                            matchedTiles.Add(gridData[x, y - 1 - i]);
                        }
                    }
                    matchCount = 1;
                }
            }
            // Check end of column
            if (matchCount >= matchLength)
            {
                for (int i = 0; i < matchCount; i++)
                {
                    matchedTiles.Add(gridData[x, height - 1 - i]);
                }
            }
        }

        return matchedTiles;
    }
}