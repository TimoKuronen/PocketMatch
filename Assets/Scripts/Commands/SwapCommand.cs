using DG.Tweening;
using System.Collections;
using UnityEngine;

public class SwapCommand : ICommand
{
    private readonly TileView viewA;
    private readonly TileView viewB;
    private readonly Vector2 targetPosA;
    private readonly Vector2 targetPosB;
    private readonly float duration;
    private readonly Ease ease;

    public SwapCommand(TileView a, TileView b, Vector2 targetPosA, Vector2 targetPosB, float duration = 0.15f, Ease ease = Ease.InOutQuad)
    {
        viewA = a;
        viewB = b;
        this.targetPosA = targetPosA;
        this.targetPosB = targetPosB;
        this.duration = duration;
        this.ease = ease;
    }

    public IEnumerator Execute()
    {
        if (viewA == null || viewB == null)
            yield break;

        var rectA = (RectTransform)viewA.transform;
        var rectB = (RectTransform)viewB.transform;

        rectA.DOKill();
        rectB.DOKill();

        Sequence seq = DOTween.Sequence();
        seq.Join(rectA.DOAnchorPos(targetPosB, duration).SetEase(ease));
        seq.Join(rectB.DOAnchorPos(targetPosA, duration).SetEase(ease));

        yield return seq.WaitForCompletion();
    }
}