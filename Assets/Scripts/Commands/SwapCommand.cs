using DG.Tweening;
using System.Collections;
using UnityEngine;

public class SwapCommand : ICommand
{
    private readonly TileView viewA, viewB;
    private readonly Vector2 targetPosA, targetPosB;

    public SwapCommand(TileView a, TileView b, Vector2 posA, Vector2 posB)
    {
        viewA = a;
        viewB = b;
        targetPosA = posA;
        targetPosB = posB;
    }

    public IEnumerator Execute()
    {
        var rectA = (RectTransform)viewA.transform;
        var rectB = (RectTransform)viewB.transform;

        rectA.DOKill();
        rectB.DOKill();

        Sequence seq = DOTween.Sequence();
        seq.Join(rectA.DOAnchorPos(targetPosB, 0.15f).SetEase(Ease.InOutQuad));
        seq.Join(rectB.DOAnchorPos(targetPosA, 0.15f).SetEase(Ease.InOutQuad));

        yield return seq.WaitForCompletion();
    }
}
