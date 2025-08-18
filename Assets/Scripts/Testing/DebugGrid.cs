using System.Collections;
using UnityEngine;

public class DebugGrid : MonoBehaviour
{
    [SerializeField] private GameObject debugTilePrefab;

    private DebugTile[,] tileTypeBoard;
    private DebugTile[,] tileStateBoard;

    private TileData[,] gridData;

    private int width = 6;
    private int height = 8;

    [SerializeField] private float tileSize = 1f;
    [SerializeField] private Vector3 gridOffset;

    private IEnumerator Start()
    {
        yield return new WaitUntil(() => Services.Get<IGameSessionService>().IsLevelDataLoaded);

        GridController.Instance.BoardUpdated += OnBoardUpdated;
        CreateDebugBoards();
    }

    private void OnBoardUpdated(TileData[,] board)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var data = board[x, y];
                var debugTile = tileTypeBoard[x, y];

                debugTile.text.text = GetTileLetter(data);
            }
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var data = board[x, y];
                var debugTile = tileStateBoard[x, y];

                debugTile.text.text = GetTileView(data);
            }
        }
    }

    void CreateDebugBoards()
    {
        tileTypeBoard = new DebugTile[width, height];
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
                tileTypeBoard[x, y] = new DebugTile { tileObject = tileGO, text = text };
            }
        }

        tileStateBoard = new DebugTile[width, height];
        gridOffset = new Vector3(width + 1, tileSize / 4f, 0f);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var position = new Vector2Int(x, y);
                var tileGO = Instantiate(debugTilePrefab, transform);
                tileGO.name = $"DebugTile_{x}_{y}";
                tileGO.transform.position = GridToWorldPos(position);

                var text = tileGO.GetComponentInChildren<TextMesh>();
                tileStateBoard[x, y] = new DebugTile { tileObject = tileGO, text = text };
            }
        }
    }

    private Vector3 GridToWorldPos(Vector2Int gridPosition)
    {
        return new Vector3(gridPosition.x * tileSize, gridPosition.y * tileSize, 0f) + gridOffset;
    }

    private string GetTileView(TileData tileData)
    {
        if (tileData == null)
        {
            Debug.Log("TileData is null, returning default value.");
            return "?";
        }

        switch (tileData.State)
        {
            case TileState.Blocked:
                return "X";
            case TileState.Empty:
                return ",";
            case TileState.Destroyable:
                return "D";
            case TileState.Normal:
                return ".";
        }

        return "?";
    }

    private string GetTileLetter(TileData tileData)
    {
        if (tileData == null)
        {
            Debug.Log("TileData is null, returning default value.");
            return ".";
        }

        switch (tileData.Power)
        {
            case TilePower.RowClearer:
                return "<>";
            case TilePower.ColumnClearer:
                return "|";
            case TilePower.Bomb:
                return "B";
            case TilePower.Rainbow:
                return "R";
            case TilePower.None:
                break;
            default:
                Debug.Log($"Unknown TilePower: {tileData.Power}, returning default value.");
                return ".";
        }

        switch (tileData.State)
        {
            case TileState.Blocked:
                return "X";
            case TileState.Empty:
                return ",";
            case TileState.Destroyable:
                return "D";
        }

        switch (tileData.Type)
        {
            case TileType.Red:
                return "1";
            case TileType.Blue:
                return "2";
            case TileType.Green:
                return "3";
            case TileType.Yellow:
                return "4";
            case TileType.Purple:
                return "5";
            default:
                break;
        }

        return ".";
    }
}
