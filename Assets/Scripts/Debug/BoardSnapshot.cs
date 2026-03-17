using System.Text;

/// <summary>
/// Helpers to serialize the current board state into a compact, human-readable string.
/// This version works directly with the TileData[,] and TileState you already use
/// in BoardDataViewValidator / GridController.
/// </summary>
public static class BoardSnapshot
{
    public static string Serialize(TileData[,] data, int width, int height)
    {
        if (data == null)
            return "<null board>";

        var sb = new StringBuilder(width * height + height);

        // Print highest row first for easier visual comparison with the game.
        for (int y = height - 1; y >= 0; y--)
        {
            for (int x = 0; x < width; x++)
            {
                var cell = data[x, y];

                if (cell == null || cell.State == TileState.Empty)
                {
                    sb.Append('.'); // truly empty
                }
                else
                {
                    // Encode different states / powers so we can distinguish blockers, destroyables, power tiles, etc.
                    char c;
                    switch (cell.State)
                    {
                        case TileState.Blocked:
                            c = 'B'; // blocker
                            break;
                        case TileState.Destroyable:
                            c = 'D'; // destroyable
                            break;
                        case TileState.Normal:
                        default:
                            // Normal tiles: encode by power first, then by type
                            if (cell.Power != TilePower.None)
                            {
                                // Different letters for different powers
                                switch (cell.Power)
                                {
                                    case TilePower.RowClearer: c = 'R'; break;
                                    case TilePower.ColumnClearer: c = 'C'; break;
                                    case TilePower.Bomb: c = 'O'; break; // 'O' for bOmb to avoid clash
                                    case TilePower.Rainbow: c = '*'; break;
                                    default: c = 'P'; break;
                                }
                            }
                            else
                            {
                                // Simple color encoding for non-power normals
                                switch (cell.Type)
                                {
                                    case TileType.Red: c = 'r'; break;
                                    case TileType.Blue: c = 'b'; break;
                                    case TileType.Green: c = 'g'; break;
                                    case TileType.Yellow: c = 'y'; break;
                                    case TileType.Purple: c = 'p'; break;
                                    case TileType.Special: c = 's'; break;
                                    default: c = '#'; break;
                                }
                            }
                            break;
                    }

                    sb.Append(c);
                }
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }
}

