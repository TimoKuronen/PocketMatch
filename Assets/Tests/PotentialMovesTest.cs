using NUnit.Framework;
using UnityEngine;

public class PotentialMovesTest
{
    [Test]
    public void HasPotentialMoves_WithPowerTile_ReturnsTrue()
    {
        // Arrange
        int width = 5;
        int height = 5;
        TileData[,] grid = CreateEmptyGrid(width, height);
        
        // Add a power tile
        grid[2, 2] = new TileData(TileType.Red, new Vector2Int(2, 2), TilePower.Bomb, TileState.Normal);

        // Act
        bool result = GridHelperMethods.HasPotentialMoves(grid, width, height);

        // Assert
        Assert.IsTrue(result, "Board with power tile should have potential moves");
    }

    [Test]
    public void HasPotentialMoves_WithNoMoves_ReturnsFalse()
    {
        // Arrange - Create a locked board pattern (checkerboard-like with 2 colors)
        int width = 4;
        int height = 4;
        TileData[,] grid = CreateLockedGrid(width, height);

        // Act
        bool result = GridHelperMethods.HasPotentialMoves(grid, width, height);

        // Assert
        Assert.IsFalse(result, "Locked board should have no potential moves");
    }

    [Test]
    public void HasPotentialMoves_WithHorizontalPattern_ReturnsTrue()
    {
        // Arrange - Create pattern: [A][A][?] where A can be moved into ?
        // Pattern: Red, Red, Blue, Blue
        //          Blue, Blue, Red, Red
        int width = 4;
        int height = 4;
        TileData[,] grid = CreateGridWithHorizontalPattern(width, height);

        // Act
        bool result = GridHelperMethods.HasPotentialMoves(grid, width, height);

        // Assert
        Assert.IsTrue(result, "Board with horizontal match pattern should have potential moves");
    }

    [Test]
    public void HasPotentialMoves_WithVerticalPattern_ReturnsTrue()
    {
        // Arrange - Create pattern: [A][A][?] vertically where A can be moved into ?
        int width = 4;
        int height = 4;
        TileData[,] grid = CreateGridWithVerticalPattern(width, height);

        // Act
        bool result = GridHelperMethods.HasPotentialMoves(grid, width, height);

        // Assert
        Assert.IsTrue(result, "Board with vertical match pattern should have potential moves");
    }

    [Test]
    public void CountPotentialMoves_WithStubController_WorksCorrectly()
    {
        // Arrange
        int width = 5;
        int height = 5;
        TileData[,] grid = CreateTestGridWithPotentialMove(width, height);
        TileView[,] views = new TileView[width, height]; // Empty views array
        MatchFinder matchFinder = new MatchFinder(width, height);
        StubGridController stubController = new StubGridController(grid, matchFinder);

        BoardStateEvaluator evaluator = new BoardStateEvaluator(grid, views, width, height, stubController);

        // Act
        var result = evaluator.CountPotentialMoves();

        // Assert
        Assert.Greater(result.TotalMoves, 0, "Board should have at least one potential move");
    }

    private TileData[,] CreateEmptyGrid(int width, int height)
    {
        TileData[,] grid = new TileData[width, height];
        return grid;
    }

    private TileData[,] CreateLockedGrid(int width, int height)
    {
        // Create a checkerboard pattern that has no possible matches
        // This is a known "locked" pattern
        TileData[,] grid = new TileData[width, height];
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                TileType type = ((x + y) % 2 == 0) ? TileType.Red : TileType.Blue;
                grid[x, y] = new TileData(type, new Vector2Int(x, y), TilePower.None, TileState.Normal);
            }
        }
        
        return grid;
    }

    private TileData[,] CreateGridWithHorizontalPattern(int width, int height)
    {
        // Create: [Red][Red][Blue][Blue] pattern where swapping Blue above/below Red-Red creates match
        TileData[,] grid = new TileData[width, height];
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                TileType type;
                if (x < 2)
                    type = TileType.Red;
                else
                    type = TileType.Blue;
                
                grid[x, y] = new TileData(type, new Vector2Int(x, y), TilePower.None, TileState.Normal);
            }
        }
        
        // Add a Red above position (2,0) so swapping creates [Red][Red][Red]
        grid[2, 1] = new TileData(TileType.Red, new Vector2Int(2, 1), TilePower.None, TileState.Normal);
        
        return grid;
    }

    private TileData[,] CreateGridWithVerticalPattern(int width, int height)
    {
        // Create vertical pattern where swapping creates a match
        TileData[,] grid = new TileData[width, height];
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                TileType type;
                if (y < 2)
                    type = TileType.Red;
                else
                    type = TileType.Blue;
                
                grid[x, y] = new TileData(type, new Vector2Int(x, y), TilePower.None, TileState.Normal);
            }
        }
        
        // Add a Red to the right of position (0,2) so swapping creates vertical match
        grid[1, 2] = new TileData(TileType.Red, new Vector2Int(1, 2), TilePower.None, TileState.Normal);
        
        return grid;
    }

    private TileData[,] CreateTestGridWithPotentialMove(int width, int height)
    {
        // Create a simple grid with a clear potential move
        TileData[,] grid = new TileData[width, height];
        
        // Fill with alternating colors
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                TileType type = ((x + y) % 3 == 0) ? TileType.Red : TileType.Blue;
                grid[x, y] = new TileData(type, new Vector2Int(x, y), TilePower.None, TileState.Normal);
            }
        }
        
        // Create a pattern: [Red][Red][Blue] horizontally
        grid[0, 0] = new TileData(TileType.Red, new Vector2Int(0, 0), TilePower.None, TileState.Normal);
        grid[1, 0] = new TileData(TileType.Red, new Vector2Int(1, 0), TilePower.None, TileState.Normal);
        grid[2, 0] = new TileData(TileType.Blue, new Vector2Int(2, 0), TilePower.None, TileState.Normal);
        // Add Red above (2,0) so swapping creates match
        grid[2, 1] = new TileData(TileType.Red, new Vector2Int(2, 1), TilePower.None, TileState.Normal);
        
        return grid;
    }
}

/// <summary>
/// Stub implementation of IGridController for testing CountPotentialMoves.
/// Only implements SwapTilesInData and MatchFinder - the two things CountPotentialMoves needs.
/// </summary>
public class StubGridController : IGridController
{
    private TileData[,] gridData;
    public MatchFinder MatchFinder { get; }

    public StubGridController(TileData[,] gridData, MatchFinder matchFinder)
    {
        this.gridData = gridData;
        this.MatchFinder = matchFinder;
    }

    public void SwapTilesInData(Vector2Int origin, Vector2Int target, TileData tileA, TileData tileB)
    {
        gridData[origin.x, origin.y] = tileB;
        gridData[target.x, target.y] = tileA;
        
        tileA.GridPosition = target;
        tileB.GridPosition = origin;
    }

    // Unused interface members - not needed for CountPotentialMoves test
    public bool IsBoardInitialized => false;
    public bool IsProcessingTiles => false;
    public GridContext GridContext => null;
    public BoardStateEvaluator BoardEvaluator => null;
    public event System.Action ActionTaken;
    public event System.Action TileMoved;
    public event System.Action TileSwapped;
    public event System.Action TileSwapError;
    public event System.Action<TileData> TileDestroyed;
    public event System.Action<TileData[,]> BoardUpdated;
    public event System.Action<TileData> PowerTileCreated;
    public event System.Action OnBoardShuffle;
    public void TrySwapTiles(Vector2Int origin, Vector2Int dir) { }
    public void AttemptPowerTrigger(TileView tileView) { }
    public Cysharp.Threading.Tasks.UniTask MatchCycleAsync() => Cysharp.Threading.Tasks.UniTask.CompletedTask;
    public Vector2 GridToUIPos(Vector2Int gridPos) => Vector2.zero;
    public void DestroyTargetTile(Vector2Int origin) { }
}
