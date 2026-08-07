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
        builder.Register<ShopOffer>(resolver => CreateContinueOffer(), Lifetime.Scoped);
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

    private static ShopOffer CreateContinueOffer()
    {
        var loaded = Resources.Load<ShopOffer>("ContinueExtraMoves");
        if (loaded != null)
            return loaded;

        var offer = ScriptableObject.CreateInstance<ShopOffer>();
        offer.offerId = "continue_extra_moves";
        offer.coinCost = 300;
        offer.rewardType = OfferRewardType.ExtraMoves;
        offer.rewardAmount = 3;
        offer.allowedPayments = new[] { OfferPaymentMethod.Coins, OfferPaymentMethod.RewardedAd };
        return offer;
    }
}