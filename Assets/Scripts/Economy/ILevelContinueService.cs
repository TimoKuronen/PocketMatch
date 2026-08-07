public enum ContinueMethodUsed
{
    None,
    Coins,
    Ad
}

public interface ILevelContinueService
{
    ContinueMethodUsed ContinueMethodUsed { get; }
    bool CanContinueWithCoins { get; }
    bool CanContinueWithAd { get; }
    bool TryContinueWithCoins();
}
