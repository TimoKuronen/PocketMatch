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

    public List<List<Vector2Int>> GetMatchGroups(TileData[,] grid)
    {
        var matches = new List<MatchGroup>();
        var visited = new HashSet<Vector2Int>();

        // Horizontal
        for (int y = 0; y < height; y++)
        {
            int x = 0;
            while (x < width - 2)
            {
                if (!IsMatchableTile(grid[x, y]))
                {
                    x++;
                    continue;
                }

                TileType type = grid[x, y].Type;
                int matchLen = 1;

                for (int i = x + 1; i < width && IsMatchableTile(grid[i, y]) && grid[i, y].Type == type; i++)
                {
                    matchLen++;
                }

                if (matchLen >= 3)
                {
                    var group = new MatchGroup(type);
                    for (int i = x; i < x + matchLen; i++)
                    {
                        Vector2Int pos = new(i, y);
                        if (visited.Add(pos))
                            group.Positions.Add(pos);
                    }
                    if (group.Positions.Count > 0)
                        matches.Add(group);
                    x += matchLen;
                }
                else
                {
                    x++;
                }
            }
        }

        // Vertical
        for (int x = 0; x < width; x++)
        {
            int y = 0;
            while (y < height - 2)
            {
                if (!IsMatchableTile(grid[x, y]))
                {
                    y++;
                    continue;
                }

                TileType type = grid[x, y].Type;
                int matchLen = 1;

                for (int i = y + 1; i < height && IsMatchableTile(grid[x, i]) && grid[x, i].Type == type; i++)
                {
                    matchLen++;
                }

                if (matchLen >= 3)
                {
                    var group = new MatchGroup(type);
                    for (int i = y; i < y + matchLen; i++)
                    {
                        Vector2Int pos = new(x, i);
                        if (visited.Add(pos))
                            group.Positions.Add(pos);
                    }
                    if (group.Positions.Count > 0)
                        matches.Add(group);
                    y += matchLen;
                }
                else
                {
                    y++;
                }
            }
        }

        return MergeIntersectingGroups(matches);
    }

    private List<List<Vector2Int>> MergeIntersectingGroups(List<MatchGroup> groups)
    {
        var merged = new List<MatchGroup>();

        foreach (var group in groups)
        {
            bool mergedIntoExisting = false;
            foreach (var existing in merged)
            {
                if (group.Type == existing.Type &&
                    (group.Positions.Any(pos => existing.Positions.Contains(pos)) ||
                     IsAdjacent(group.Positions, existing.Positions)))
                {
                    existing.Positions.AddRange(group.Positions.Where(p => !existing.Positions.Contains(p)));
                    mergedIntoExisting = true;
                    break;
                }
            }
            if (!mergedIntoExisting)
                merged.Add(new MatchGroup(group.Type) { Positions = new List<Vector2Int>(group.Positions) });
        }

        return merged.Select(g => g.Positions).ToList();
    }

    private bool IsAdjacent(List<Vector2Int> group, List<Vector2Int> existing)
    {
        foreach (var pos in group)
        {
            foreach (var existingPos in existing)
            {
                if ((Mathf.Abs(pos.x - existingPos.x) == 1 && pos.y == existingPos.y) ||
                    (Mathf.Abs(pos.y - existingPos.y) == 1 && pos.x == existingPos.x))
                {
                    return true;
                }
            }
        }
        return false;
    }

    public MatchShape DetermineMatchShape(List<Vector2Int> match, TileData[,] gridData)
    {
        if (match.Count >= 5)
        {
            bool sameY = match.All(p => p.y == match[0].y);
            bool sameX = match.All(p => p.x == match[0].x);

            if (sameY || sameX)
                return MatchShape.FiveLine;
            else
                return MatchShape.TOrL;
        }
        if (match.Count == 4)
        {
            bool sameY = match.All(p => p.y == match[0].y);
            bool sameX = match.All(p => p.x == match[0].x);

            if (sameX) 
                return MatchShape.FourHorizontal;
            if (sameY) 
                return MatchShape.FourVertical;
        }
        if (match.Count == 3)
        {
            return MatchShape.ThreeLine;
        }

        Debug.Log("Match shape determination failed: no valid match shape found." + match.Count);
        return MatchShape.None;
    }

    private bool IsMatchableTile(TileData tileData)
    {
        return tileData != null && tileData.State == TileState.Normal;
    }
}