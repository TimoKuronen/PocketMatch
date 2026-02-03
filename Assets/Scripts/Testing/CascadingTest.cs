using System.Collections;
using UnityEngine;

/// <summary>
/// Test script to simulate cascading after 3 tiles are destroyed vertically.
/// Right-click component → "Test Cascading" to run in Editor without Play mode.
/// </summary>
public class CascadingTest : MonoBehaviour
{
    [ContextMenu("Test Cascading")]
    public void TestCascading()
    {
        StartCoroutine(RunTest());
    }

    private IEnumerator RunTest()
    {
        Debug.Log("═══════════════════════════════════════════════════════════");
        Debug.Log("=== CASCADING TEST: 3 Tiles Destroyed Vertically ===");
        Debug.Log("═══════════════════════════════════════════════════════════");
        Debug.Log("Scenario: Column 2 has 3 tiles destroyed at y=5, y=4, y=3");
        Debug.Log("Expected: Tiles above should cascade down, filling gaps");
        Debug.Log("");
        
        // Create a 6x8 grid (matching your game)
        int width = 6;
        int height = 8;
        TileData[,] gridData = new TileData[width, height];
        TileView[,] gridViews = new TileView[width, height];
        
        // Initialize all as empty
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                gridData[x, y] = new TileData(TileType.Red, new Vector2Int(x, y), TilePower.None, TileState.Empty);
                gridViews[x, y] = null;
            }
        }
        
        // Set up scenario: Column 2 has tiles at y=7,6,5,4,3,2,1,0
        // But y=5,4,3 are destroyed (empty) - simulating a match
        CreateTile(gridData, gridViews, 2, 7, TileType.Red);
        CreateTile(gridData, gridViews, 2, 6, TileType.Blue);
        // y=5 destroyed (empty) - this was in the match
        // y=4 destroyed (empty) - this was in the match
        // y=3 destroyed (empty) - this was in the match
        CreateTile(gridData, gridViews, 2, 2, TileType.Green);
        CreateTile(gridData, gridViews, 2, 1, TileType.Yellow);
        CreateTile(gridData, gridViews, 2, 0, TileType.Purple);
        
        // Add tiles in adjacent columns to test cascading behavior
        CreateTile(gridData, gridViews, 1, 6, TileType.Red);   // Column 1, above the gap
        CreateTile(gridData, gridViews, 1, 5, TileType.Blue);  // Column 1, at gap level
        CreateTile(gridData, gridViews, 3, 6, TileType.Green); // Column 3, above the gap
        CreateTile(gridData, gridViews, 3, 5, TileType.Yellow); // Column 3, at gap level
        
        Debug.Log("=== GRID STATE BEFORE DROP ===");
        PrintGrid(gridData, 2); // Print column 2 and adjacent columns
        
        // Create DropCommand WITH LOGGING ENABLED (only for testing)
        var dropCommand = new DropCommand(gridData, gridViews, width, height, GridToUIPos);
        
        Debug.Log("");
        Debug.Log("=== EXECUTING DROP COMMAND (with detailed logging) ===");
        Debug.Log("");
        yield return dropCommand.Execute();
        
        Debug.Log("");
        Debug.Log("=== GRID STATE AFTER DROP ===");
        PrintGrid(gridData, 2);
        
        Debug.Log("");
        Debug.Log("═══════════════════════════════════════════════════════════");
        Debug.Log("=== CASCADING TEST COMPLETE ===");
        Debug.Log("═══════════════════════════════════════════════════════════");
    }
    
    private void CreateTile(TileData[,] gridData, TileView[,] gridViews, int x, int y, TileType type)
    {
        var data = new TileData(type, new Vector2Int(x, y), TilePower.None, TileState.Normal);
        gridData[x, y] = data;
        
        // Create minimal TileView for testing (required by DropCommand)
        GameObject go = new GameObject($"Tile_{x}_{y}");
        RectTransform rect = go.AddComponent<RectTransform>();
        UnityEngine.UI.Image img = go.AddComponent<UnityEngine.UI.Image>();
        TileView view = go.AddComponent<TileView>();
        view.Init(data);
        gridViews[x, y] = view;
    }
    
    private Vector2 GridToUIPos(Vector2Int gridPos)
    {
        // Simple mock implementation for testing
        return new Vector2(gridPos.x * 100f, gridPos.y * 100f);
    }
    
    private void PrintGrid(TileData[,] grid, int centerColumn)
    {
        int height = grid.GetLength(1);
        int width = grid.GetLength(0);
        
        // Print columns centerColumn-1, centerColumn, centerColumn+1
        for (int col = Mathf.Max(0, centerColumn - 1); col <= Mathf.Min(width - 1, centerColumn + 1); col++)
        {
            Debug.Log($"\nColumn {col}:");
            for (int y = height - 1; y >= 0; y--)
            {
                var tile = grid[col, y];
                string tileStr;
                if (tile == null)
                    tileStr = "NULL";
                else if (tile.State == TileState.Empty)
                    tileStr = "EMPTY";
                else if (tile.State == TileState.Normal)
                    tileStr = $"{tile.Type}";
                else
                    tileStr = tile.State.ToString();
                
                Debug.Log($"  y={y}: {tileStr}");
            }
        }
    }
}
