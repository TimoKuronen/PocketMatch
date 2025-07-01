using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwapCommand : ICommand
{
    private GridController controller;
    private Vector2Int posA, posB;
    private TileData tileA, tileB;

    public void Execute()
    {
        //controller.PerformSwap(posA, posB, tileA, tileB);
    }

    public void Undo()
    {
        //controller.PerformSwap(posB, posA, tileB, tileA);
    }

    public SwapCommand(GridController controller, Vector2Int posA, Vector2Int posB)
    {
        this.controller = controller;
        this.posA = posA;
        this.posB = posB;

        //tileA = controller.GetTileData(posA);
        //tileB = controller.GetTileData(posB);
    }
}