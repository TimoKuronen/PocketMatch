using DG.Tweening;
using System.Threading.Tasks;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class SwapCommand : ICommand
{
    private readonly TileView viewA;
    private readonly TileView viewB;
    private readonly Vector2 targetPosA;
    private readonly Vector2 targetPosB;
    private readonly float duration = 0.15f;
    private readonly Ease ease;

    public SwapCommand(TileView a, TileView b, Vector2 targetPosA, Vector2 targetPosB, Ease ease = Ease.InOutQuad)
    {
        viewA = a;
        viewB = b;
        this.targetPosA = targetPosA;
        this.targetPosB = targetPosB;
        this.ease = ease;
    }

    public async UniTask ExecuteAsync()
    {
        if (viewA == null || viewB == null)
            return;

        var rectA = viewA.RectTransform;
        var rectB = viewB.RectTransform;

        rectA.DOKill();
        rectB.DOKill();

        Sequence seq = DOTween.Sequence();
        seq.Join(rectA.DOAnchorPos(targetPosB, duration).SetEase(ease));
        seq.Join(rectB.DOAnchorPos(targetPosA, duration).SetEase(ease));

        await seq.AsyncWaitForCompletion();
    }
}