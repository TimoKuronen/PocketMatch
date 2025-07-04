using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MatchFinder
{
    private readonly int width;
    private readonly int height;

    public MatchFinder(int width, int height)
    {
        this.width = width;
        this.height = height;
    }

    public List<Vector2Int> GetMatches(TileData[,] gridData)
    {
        var matched = new HashSet<Vector2Int>();

        // Horizontal matches
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width - 2;)
            {
                var start = gridData[x, y];
                if (start == null)
                {
                    x++;
                    continue;
                }

                var matchType = start.Type;
                int matchLen = 1;

                for (int i = x + 1; i < width && gridData[i, y] != null && gridData[i, y].Type == matchType; i++)
                    matchLen++;

                if (matchLen >= 3)
                {
                    for (int i = x; i < x + matchLen; i++)
                        matched.Add(new Vector2Int(i, y));

                    x += matchLen;
                }
                else
                    x++;
            }
        }

        // Vertical matches
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height - 2;)
            {
                var start = gridData[x, y];
                if (start == null)
                {
                    y++;
                    continue;
                }

                var matchType = start.Type;
                int matchLen = 1;

                for (int i = y + 1; i < height && gridData[x, i] != null && gridData[x, i].Type == matchType; i++)
                    matchLen++;

                if (matchLen >= 3)
                {
                    for (int i = y; i < y + matchLen; i++)
                        matched.Add(new Vector2Int(x, i));

                    y += matchLen;
                }
                else
                    y++;
            }
        }

        return matched.ToList();
    }

    public MatchShape DetermineMatchShape(List<Vector2Int> match, TileData[,] gridData)
    {
        var types = match.Select(pos => gridData[pos.x, pos.y]?.Type).Distinct().ToList();
        if (types.Count != 1 || types[0] == null) return MatchShape.None;

        var xs = match.Select(pos => pos.x).Distinct().Count();
        var ys = match.Select(pos => pos.y).Distinct().Count();

        if (xs > 1 && ys > 1 && match.Count >= 5)
            return MatchShape.TOrL;
        if (match.Count >= 5)
            return MatchShape.FiveLine;
        if (match.Count == 4)
            return MatchShape.FourLine;
        if (match.Count == 3)
            return MatchShape.ThreeLine;

        return MatchShape.None;
    }
}