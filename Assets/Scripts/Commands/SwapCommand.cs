using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwapCommand : ICommand
{
    private TileView viewA, viewB;
    private Vector3 targetPosA, targetPosB;

    public SwapCommand(TileView a, TileView b, Vector3 posA, Vector3 posB)
    {
        viewA = a;
        viewB = b;
        targetPosA = posA;
        targetPosB = posB;
    }

    public IEnumerator Execute()
    {
        viewA.transform.DOMove(targetPosB, 0.15f);
        viewB.transform.DOMove(targetPosA, 0.15f);
        yield return new WaitForSeconds(0.2f);
    }
}