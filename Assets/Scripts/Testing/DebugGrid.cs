using System.Collections;
using UnityEngine;

public class DebugGrid : MonoBehaviour
{
    [SerializeField] private GameObject debugTilePrefab;

    private DebugTile[,] debugTiles;

    private TileData[,] gridData;

    private int width = 6;
    private int height = 8;

    [SerializeField] private float tileSize = 1f;
    [SerializeField] private Vector3 gridOffset;

    private IEnumerator Start()
    {
        yield return new WaitUntil(() => Services.Get<IGameSessionService>().IsLevelDataLoaded);

        GridController.Instance.BoardUpdated += OnBoardUpdated;
        CreateDebugBoard();
    }

    private void OnBoardUpdated(TileData[,] board)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var data = board[x, y];
                var debugTile = debugTiles[x, y];

                debugTile.text.text = GetTileLetter(data);
            }
        }

        //Debug.Log("Debug board updated with new tile data.");
    }

    void CreateDebugBoard()
    {
        debugTiles = new DebugTile[width, height];
        gridOffset = new Vector3(-width - 1, tileSize / 4f, 0f);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var position = new Vector2Int(x, y);
                var tileGO = Instantiate(debugTilePrefab, transform);
                tileGO.name = $"DebugTile_{x}_{y}";
                tileGO.transform.position = GridToWorldPos(position);

                var text = tileGO.GetComponentInChildren<TextMesh>();
                debugTiles[x, y] = new DebugTile { tileObject = tileGO, text = text };
            }
        }
    }

    private Vector3 GridToWorldPos(Vector2Int gridPosition)
    {
        return new Vector3(gridPosition.x * tileSize, gridPosition.y * tileSize, 0f) + gridOffset;
    }

    private string GetTileLetter(TileData tileData)
    {
        if (tileData == null)
        {
            Debug.Log("TileData is null, returning default value.");
            return ".";
        }

        if (tileData.State == TileState.Blocked)
        {
            return "X"; // Blocked tiles
        }
        else if (tileData.Power == TilePower.None)
        {
            if (tileData.State == TileState.Empty)
            {
                return ",";
            }
            else if (tileData.State == TileState.Destroyable)
            {
                return "D";
            }
                return ".";
        }
        else if (tileData.Power == TilePower.RowClearer)
        {
            return "<>";
        }
        else if (tileData.Power == TilePower.ColumnClearer)
        {
            return "|";
        }
        else if (tileData.Power == TilePower.Bomb)
        {
            return "B";
        }
        else if (tileData.Power == TilePower.Rainbow)
        {
            return "R";
        }
        Debug.Log($"Unknown TilePower: {tileData.Power}, returning default value.");
        return ".";
    }
}
