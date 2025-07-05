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

    public CreatePowerTileCommand(
        List<List<Vector2Int>> matchGroups,
        TileData[,] gridData,
        TileView[,] gridViews,
        Func<List<Vector2Int>, TileData[,], MatchShape> determineShape,
        Func<Vector2Int, TileType, TilePower, TileData> createData)
    {
        this.matchGroups = matchGroups;
        this.gridData = gridData;
        this.gridViews = gridViews;
        this.determineShape = determineShape;
        this.createData = createData;
    }

    public IEnumerator Execute()
    {
        foreach (var group in matchGroups)
        {
            MatchShape shape = determineShape(group, gridData);

            if (shape == MatchShape.None)
                continue;

            Vector2Int origin = group[0]; // You may want to use last moved tile
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
                gridViews[origin.x, origin.y].Init(newData, GridController.Instance.Sprite);
                gridViews[origin.x, origin.y].transform.DOPunchScale(Vector3.one * 0.25f, 0.2f);
            }
        }

        yield return null;
    }
}