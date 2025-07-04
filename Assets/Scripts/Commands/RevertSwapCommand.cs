using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RevertSwapCommand : ICommand
{
    private TileView viewA, viewB;
    private Vector3 origPosA, origPosB;

    public RevertSwapCommand(TileView a, TileView b, Vector3 posA, Vector3 posB)
    {
        viewA = a;
        viewB = b;
        origPosA = posA;
        origPosB = posB;
    }

    public IEnumerator Execute()
    {
        viewA.transform.DOKill();
        viewB.transform.DOKill();
        yield return DOTween.Sequence()
            .Join(viewA.transform.DOMove(origPosA, 0.25f).SetEase(Ease.OutBack))
            .Join(viewB.transform.DOMove(origPosB, 0.25f).SetEase(Ease.OutBack))
            .WaitForCompletion();
    }
}
