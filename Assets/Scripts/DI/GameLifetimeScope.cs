using UnityEngine;
using VContainer;
using VContainer.Unity;

public class GameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<IGameSessionService, GameSessionService>(Lifetime.Scoped);
        builder.Register<ILevelManager, LevelManager>(Lifetime.Scoped).As<IStartable>();
        builder.Register<ILevelEarningsService, LevelEarningsService>(Lifetime.Scoped).As<IStartable>();
        builder.Register<ILevelContinueService, LevelContinueService>(Lifetime.Scoped);
        builder.Register<ShopOffer>(resolver => CreateContinueOffer(resolver.Resolve<ITunables>()), Lifetime.Scoped);
        builder.Register<IEffectService, EffectService>(Lifetime.Singleton).As<IStartable>();
        builder.Register<MenuStackManager>(Lifetime.Scoped);

        builder.RegisterComponentInHierarchy<UIGameHUD>()
               .As<IGameHudView>();
        builder.RegisterComponentInHierarchy<PauseSettingsPanel>()
               .As<IPauseSettingsView>();
        builder.RegisterComponentInHierarchy<WinPanel>()
               .As<IWinView>();
        builder.RegisterComponentInHierarchy<LosePanel>()
               .As<ILoseView>();
        builder.RegisterComponentInHierarchy<ConfirmationDialog>();

        builder.RegisterComponentInHierarchy<GridController>().As<IGridController>();
        builder.RegisterComponentInHierarchy<GridAudioPlayer>();
        
        builder.Register<GameHudPresenter>(Lifetime.Scoped).As<IStartable>();
        builder.Register<PauseSettingsPresenter>(Lifetime.Scoped).As<IStartable>();
        builder.Register<WinPresenter>(Lifetime.Scoped).As<IStartable>();
        builder.Register<LosePresenter>(Lifetime.Scoped).As<IStartable>();

        builder.RegisterBuildCallback(container =>
        {
            var menuStackManager = container.Resolve<MenuStackManager>();
            var gridController = container.Resolve<IGridController>();
            menuStackManager.SetGridController(gridController);
        });
    }

    // The offer asset is shared, so tunables are applied to a copy.
    private static ShopOffer CreateContinueOffer(ITunables tunables)
    {
        var loaded = Resources.Load<ShopOffer>("ContinueExtraMoves");
        ShopOffer offer;
        if (loaded != null)
        {
            offer = Object.Instantiate(loaded);
        }
        else
        {
            offer = ScriptableObject.CreateInstance<ShopOffer>();
            offer.offerId = "continue_extra_moves";
            offer.rewardType = OfferRewardType.ExtraMoves;
            offer.allowedPayments = new[] { OfferPaymentMethod.Coins, OfferPaymentMethod.RewardedAd };
        }

        offer.coinCost = tunables.ContinueCoinCost;
        offer.rewardAmount = tunables.ContinueExtraMoves;
        return offer;
    }
}