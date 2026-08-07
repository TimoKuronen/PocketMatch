using VContainer;

public class LevelContinueService : ILevelContinueService
{
    private readonly ILevelManager levelManager;
    private readonly IEconomyService economyService;
    private readonly ShopOffer continueOffer;

    public ContinueMethodUsed ContinueMethodUsed { get; private set; } = ContinueMethodUsed.None;

    public bool CanContinueWithCoins =>
        levelManager.IsLevelEnded
        && ContinueMethodUsed == ContinueMethodUsed.None
        && economyService.CanAfford(continueOffer.coinCost);

    public bool CanContinueWithAd =>
        levelManager.IsLevelEnded
        && ContinueMethodUsed == ContinueMethodUsed.None;

    [Inject]
    public LevelContinueService(
        ILevelManager levelManager,
        IEconomyService economyService,
        ShopOffer continueOffer)
    {
        this.levelManager = levelManager;
        this.economyService = economyService;
        this.continueOffer = continueOffer;
    }

    public bool TryContinueWithCoins()
    {
        if (!CanContinueWithCoins)
            return false;

        if (!economyService.TrySpendCoins(continueOffer.coinCost, continueOffer.offerId, out _))
            return false;

        levelManager.GrantExtraMoves(continueOffer.rewardAmount);
        ContinueMethodUsed = ContinueMethodUsed.Coins;
        return true;
    }
}
