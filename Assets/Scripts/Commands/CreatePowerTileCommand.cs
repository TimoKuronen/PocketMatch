using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatePowerTileCommand : ICommand
{
    private readonly List<List<Vector2Int>> matchGroups;
    private readonly TileData[,] gridData;
    private readonly TileView[,] gridViews;
    private readonly Func<List<Vector2Int>, TileData[,], MatchShape> determineShape;
    private readonly Func<Vector2Int, TileType, TilePower, TileData> createData;
    private readonly Vector2Int? lastMovedTilePosition;

    public CreatePowerTileCommand(
        List<List<Vector2Int>> matchGroups,
        TileData[,] gridData,
        TileView[,] gridViews,
        Func<List<Vector2Int>, TileData[,], MatchShape> determineShape,
        Func<Vector2Int, TileType, TilePower, TileData> createData,
        Vector2Int? lastMovedTilePosition = null)
    {
        this.matchGroups = matchGroups;
        this.gridData = gridData;
        this.gridViews = gridViews;
        this.determineShape = determineShape;
        this.createData = createData;
        this.lastMovedTilePosition = lastMovedTilePosition;
    }

    public IEnumerator Execute()
    {
        foreach (var group in matchGroups)
        {
            MatchShape shape = determineShape(group, gridData);

            if (shape == MatchShape.None)
                continue;

            Vector2Int origin = DeterminePowerTilePosition(group);
            TileData baseData = gridData[origin.x, origin.y];

            if (baseData == null)
                continue;

            TilePower power = shape switch
            {
                MatchShape.FourHorizontal => TilePower.RowClearer,
                MatchShape.FourVertical => TilePower.ColumnClearer,
                MatchShape.TOrL => TilePower.Bomb,
                MatchShape.FiveLine => TilePower.Rainbow,
                _ => TilePower.None
            };

            if (power == TilePower.None)
                continue;

            TileData newData = createData(origin, baseData.Type, power);
            gridData[origin.x, origin.y] = newData;

            if (gridViews[origin.x, origin.y] != null)
            {
                gridViews[origin.x, origin.y].Init(newData);
                gridViews[origin.x, origin.y].transform.DOPunchScale(Vector3.one * 0.25f, 0.2f);
            }
        }

        yield return null;
    }

    private Vector2Int DeterminePowerTilePosition(List<Vector2Int> group)
    {
        // If player moved a tile and it's part of this match group, use that position
        if (lastMovedTilePosition.HasValue && group.Contains(lastMovedTilePosition.Value))
        {
            return lastMovedTilePosition.Value;
        }

        int count = group.Count;
        int middleIndex;

        if (count == 5)
        {
            middleIndex = 2;
        }
        else
        {
            middleIndex = count / 2;
        }

        return group[middleIndex];
    }
}