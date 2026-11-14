using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Edit Mode coverage for lose-screen continue rules: one continue per attempt, reward-gated ad grant,
/// and in-flight ad continues rejecting duplicate taps.
/// </summary>
public class LevelContinueServiceTest
{
    private FakeLevelManager levelManager;
    private FakeEconomyService economy;
    private FakeAdsService ads;
    private ShopOffer offer;
    private LevelContinueService service;

    [SetUp]
    public void SetUp()
    {
        levelManager = new FakeLevelManager { IsLevelEnded = true };
        economy = new FakeEconomyService { Balance = 1000 };
        ads = new FakeAdsService();
        offer = ScriptableObject.CreateInstance<ShopOffer>();
        offer.coinCost = 300;
        offer.rewardAmount = 3;
        service = new LevelContinueService(levelManager, economy, ads, offer);
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(offer);
    }

    [Test]
    public void AdContinue_WhenRewarded_GrantsMovesOnce()
    {
        var task = service.TryContinueWithAdAsync();
        ads.Complete(RewardedAdResult.Rewarded);

        Assert.AreEqual(AdContinueResult.Continued, task.GetAwaiter().GetResult());
        Assert.AreEqual(1, levelManager.GrantCalls);
        Assert.AreEqual(3, levelManager.GrantedMoves);
        Assert.AreEqual(ContinueMethodUsed.Ad, service.ContinueMethodUsed);
    }

    [Test]
    public void AdContinue_SecondTapWhileAdShowing_IsRejectedWithoutSecondShow()
    {
        var first = service.TryContinueWithAdAsync();
        var second = service.TryContinueWithAdAsync();

        Assert.AreEqual(AdContinueResult.NotEligible, second.GetAwaiter().GetResult());
        Assert.AreEqual(1, ads.ShowCalls);

        ads.Complete(RewardedAdResult.Rewarded);
        Assert.AreEqual(AdContinueResult.Continued, first.GetAwaiter().GetResult());
        Assert.AreEqual(1, levelManager.GrantCalls);
    }

    [Test]
    public void AdContinue_WhileAdShowing_BlocksCoinContinue()
    {
        service.TryContinueWithAdAsync().Forget();

        Assert.IsFalse(service.CanContinueWithCoins);
        Assert.IsFalse(service.TryContinueWithCoins());
        Assert.AreEqual(1000, economy.Balance);
    }

    [Test]
    public void AdContinue_AfterContinueAlreadyUsed_IsNotEligible()
    {
        Assert.IsTrue(service.TryContinueWithCoins());
        levelManager.IsLevelEnded = true;

        var result = service.TryContinueWithAdAsync().GetAwaiter().GetResult();

        Assert.AreEqual(AdContinueResult.NotEligible, result);
        Assert.AreEqual(0, ads.ShowCalls);
    }

    [TestCase(RewardedAdResult.Dismissed, AdContinueResult.Dismissed)]
    [TestCase(RewardedAdResult.Unavailable, AdContinueResult.Unavailable)]
    public void AdContinue_WithoutReward_GrantsNothingAndAllowsRetry(RewardedAdResult adResult, AdContinueResult expected)
    {
        var task = service.TryContinueWithAdAsync();
        ads.Complete(adResult);

        Assert.AreEqual(expected, task.GetAwaiter().GetResult());
        Assert.AreEqual(0, levelManager.GrantCalls);
        Assert.AreEqual(ContinueMethodUsed.None, service.ContinueMethodUsed);
        Assert.IsTrue(service.CanContinueWithAd);
    }

    [Test]
    public void AdContinue_RewardArrivingAfterLevelResumed_GrantsNothing()
    {
        var task = service.TryContinueWithAdAsync();
        levelManager.IsLevelEnded = false;
        ads.Complete(RewardedAdResult.Rewarded);

        Assert.AreEqual(AdContinueResult.NotEligible, task.GetAwaiter().GetResult());
        Assert.AreEqual(0, levelManager.GrantCalls);
    }

    [Test]
    public void AdContinue_WhenAdsUnavailable_IsNotOffered()
    {
        ads.IsRewardedAdAvailable = false;

        Assert.IsFalse(service.CanContinueWithAd);
        Assert.AreEqual(AdContinueResult.NotEligible, service.TryContinueWithAdAsync().GetAwaiter().GetResult());
        Assert.AreEqual(0, ads.ShowCalls);
    }

    private sealed class FakeAdsService : IAdsService
    {
        private UniTaskCompletionSource<RewardedAdResult> pending;

        public int ShowCalls { get; private set; }
        public bool IsRewardedAdAvailable { get; set; } = true;
        public bool IsInitialized => true;
        public bool InterstitialAdReady => false;
        public bool InterstitialAdCompleted => false;
        public event Action OnInterstitialAdClosed { add { } remove { } }

        public UniTask<RewardedAdResult> ShowRewardedAdAsync(CancellationToken cancellationToken = default)
        {
            ShowCalls++;
            pending = new UniTaskCompletionSource<RewardedAdResult>();
            return pending.Task;
        }

        public void Complete(RewardedAdResult result) => pending.TrySetResult(result);

        public void ShowInterstitialAd() { }
        public void ShowBannerAd() { }
        public void HideBannerAd() { }
        public void ForceMarkAdComplete() { }
        public string Describe() => string.Empty;
        public void RecreateAds() { }
    }

    private sealed class FakeLevelManager : ILevelManager
    {
        public int GrantCalls { get; private set; }
        public int GrantedMoves { get; private set; }
        public int MovesRemaining => 0;
        public bool IsLevelEnded { get; set; }
        public MapData LocalMapData => null;
        public VictoryConditions VictoryConditions => null;
        public Action OnVictoryConditionsUpdated { get; set; }
        public Action OnLevelWon { get; set; }
        public Action OnLevelLost { get; set; }
        public Action OnLevelContinued { get; set; }

        public void GrantExtraMoves(int count)
        {
            GrantCalls++;
            GrantedMoves += count;
            IsLevelEnded = false;
        }
    }

    private sealed class FakeEconomyService : IEconomyService
    {
        public int Balance { get; set; }
        public event Action<int> OnBalanceChanged { add { } remove { } }
        public event Action<CoinsSpentEventArgs> OnCoinsSpent { add { } remove { } }

        public bool CanAfford(int amount) => Balance >= amount;

        public bool TrySpendCoins(int amount, string reason, out int newBalance)
        {
            if (!CanAfford(amount))
            {
                newBalance = Balance;
                return false;
            }

            Balance -= amount;
            newBalance = Balance;
            return true;
        }

        public void AddCoins(int amount, string source) => Balance += amount;
        public void SetBalance(int balance) => Balance = balance;
    }
}
