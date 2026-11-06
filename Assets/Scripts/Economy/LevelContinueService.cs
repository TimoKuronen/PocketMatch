using System.Threading;
using Cysharp.Threading.Tasks;
using VContainer;

public class LevelContinueService : ILevelContinueService
{
    private readonly ILevelManager levelManager;
    private readonly IEconomyService economyService;
    private readonly IAdsService adsService;
    private readonly ShopOffer continueOffer;
    private bool isAdContinueInProgress;

    public ContinueMethodUsed ContinueMethodUsed { get; private set; } = ContinueMethodUsed.None;

    public bool CanContinueWithCoins =>
        IsContinueEligible
        && continueOffer.Allows(OfferPaymentMethod.Coins)
        && economyService.CanAfford(continueOffer.coinCost);

    public bool CanContinueWithAd =>
        IsContinueEligible
        && continueOffer.Allows(OfferPaymentMethod.RewardedAd)
        && adsService.IsRewardedAdAvailable;

    private bool IsContinueEligible =>
        levelManager.IsLevelEnded
        && ContinueMethodUsed == ContinueMethodUsed.None
        && !isAdContinueInProgress;

    [Inject]
    public LevelContinueService(
        ILevelManager levelManager,
        IEconomyService economyService,
        IAdsService adsService,
        ShopOffer continueOffer)
    {
        this.levelManager = levelManager;
        this.economyService = economyService;
        this.adsService = adsService;
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

    public async UniTask<AdContinueResult> TryContinueWithAdAsync(CancellationToken cancellationToken = default)
    {
        if (!CanContinueWithAd)
            return AdContinueResult.NotEligible;

        isAdContinueInProgress = true;
        RewardedAdResult adResult;
        try
        {
            adResult = await adsService.ShowRewardedAdAsync(cancellationToken);
        }
        finally
        {
            isAdContinueInProgress = false;
        }

        switch (adResult)
        {
            case RewardedAdResult.Rewarded:
                // The attempt may have ended another way while the ad was up (restart, scene exit).
                if (!levelManager.IsLevelEnded || ContinueMethodUsed != ContinueMethodUsed.None)
                    return AdContinueResult.NotEligible;

                levelManager.GrantExtraMoves(continueOffer.rewardAmount);
                ContinueMethodUsed = ContinueMethodUsed.Ad;
                return AdContinueResult.Continued;
            case RewardedAdResult.Dismissed:
                return AdContinueResult.Dismissed;
            default:
                return AdContinueResult.Unavailable;
        }
    }
}
