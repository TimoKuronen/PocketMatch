public class PotentialMovesResult
{
    public int SwapMoveCount { get; set; }     
    public int PowerTileMoveCount { get; set; } 
    public int TotalMoves => SwapMoveCount + PowerTileMoveCount;

    public PotentialMovesResult(int swapMoves, int powerMoves)
    {
        SwapMoveCount = swapMoves;
        PowerTileMoveCount = powerMoves;
    }
}