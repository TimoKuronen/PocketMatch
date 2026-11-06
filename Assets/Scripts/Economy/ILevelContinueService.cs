using System.Threading;
using Cysharp.Threading.Tasks;

public enum ContinueMethodUsed
{
    None,
    Coins,
    Ad
}

public enum AdContinueResult
{
    Continued,
    NotEligible,
    Dismissed,
    Unavailable
}

public interface ILevelContinueService
{
    ContinueMethodUsed ContinueMethodUsed { get; }
    bool CanContinueWithCoins { get; }
    bool CanContinueWithAd { get; }
    bool TryContinueWithCoins();

    /// <summary>
    /// Shows a rewarded ad and grants the continue only on a confirmed reward. Calls while an ad
    /// continue is in flight, or after a continue was already used this attempt, return NotEligible.
    /// </summary>
    UniTask<AdContinueResult> TryContinueWithAdAsync(CancellationToken cancellationToken = default);
}
