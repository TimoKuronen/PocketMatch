using System;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Edit Mode coverage for <see cref="EconomyService"/>: spending rules, balance events, saves, and the level-win grant.
/// </summary>
public class EconomyServiceTest
{
    private FakeSaveService save;
    private EconomyService economy;
    private int balanceEvents;
    private int lastBalance;

    [SetUp]
    public void SetUp()
    {
        balanceEvents = 0;
        lastBalance = 0;
        save = new FakeSaveService();
        economy = new EconomyService(save);
        economy.OnBalanceChanged += balance =>
        {
            balanceEvents++;
            lastBalance = balance;
        };
        economy.Start();
    }

    [TearDown]
    public void TearDown()
    {
        economy.Dispose();
    }

    [Test]
    public void AddCoins_IncreasesBalanceSavesAndRaisesEvent()
    {
        economy.AddCoins(50, "test");

        Assert.AreEqual(50, economy.Balance);
        Assert.AreEqual(1, save.SaveCalls);
        Assert.AreEqual(1, balanceEvents);
        Assert.AreEqual(50, lastBalance);
    }

    [Test]
    public void AddCoins_NonPositiveAmount_IsIgnored()
    {
        economy.AddCoins(0, "test");
        economy.AddCoins(-5, "test");

        Assert.AreEqual(0, economy.Balance);
        Assert.AreEqual(0, save.SaveCalls);
        Assert.AreEqual(0, balanceEvents);
    }

    [Test]
    public void TrySpend_WithEnoughCoins_DeductsSavesAndReportsTheSpend()
    {
        save.PlayerData.coins = 300;
        CoinsSpentEventArgs spent = null;
        economy.OnCoinsSpent += args => spent = args;

        bool result = economy.TrySpendCoins(120, "continue", out int newBalance);

        Assert.IsTrue(result);
        Assert.AreEqual(180, newBalance);
        Assert.AreEqual(180, economy.Balance);
        Assert.AreEqual(1, save.SaveCalls);
        Assert.AreEqual(120, spent.Amount);
        Assert.AreEqual("continue", spent.Reason);
    }

    [Test]
    public void TrySpend_WithTooFewCoins_ChangesNothing()
    {
        save.PlayerData.coins = 100;

        bool result = economy.TrySpendCoins(101, "continue", out int newBalance);

        Assert.IsFalse(result);
        Assert.AreEqual(100, newBalance);
        Assert.AreEqual(100, economy.Balance);
        Assert.AreEqual(0, save.SaveCalls);
        Assert.AreEqual(0, balanceEvents);
    }

    [Test]
    public void TrySpend_NonPositiveAmount_IsRejected()
    {
        save.PlayerData.coins = 100;
        UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, "[EconomyService] Spend amount must be positive.");

        bool result = economy.TrySpendCoins(0, "continue", out _);

        Assert.IsFalse(result);
        Assert.AreEqual(100, economy.Balance);
    }

    [Test]
    public void SetBalance_NeverGoesNegative()
    {
        economy.SetBalance(-40);

        Assert.AreEqual(0, economy.Balance);
    }

    [Test]
    public void LevelCompleted_GrantsTheLevelScoreAsCoins()
    {
        RaiseCompleted(totalScore: 120, capReached: false);

        Assert.AreEqual(120, economy.Balance);
    }

    [Test]
    public void LevelCompleted_AtLevelCap_StillGrantsCoins()
    {
        RaiseCompleted(totalScore: 120, capReached: true);

        Assert.AreEqual(120, economy.Balance);
    }

    [Test]
    public void LevelCompleted_WithNoScore_GrantsNothing()
    {
        RaiseCompleted(totalScore: 0, capReached: false);

        Assert.AreEqual(0, economy.Balance);
        Assert.AreEqual(0, save.SaveCalls);
    }

    [Test]
    public void PlayerDataReplaced_RaisesBalanceChanged()
    {
        save.PlayerData.coins = 321;

        save.RaisePlayerDataReplaced();

        Assert.AreEqual(1, balanceEvents);
        Assert.AreEqual(321, lastBalance);
    }

    [Test]
    public void AfterDispose_LevelCompletedNoLongerGrantsCoins()
    {
        economy.Dispose();

        RaiseCompleted(totalScore: 120, capReached: false);

        Assert.AreEqual(0, economy.Balance);
    }

    private static void RaiseCompleted(int totalScore, bool capReached)
    {
        LevelEvents.RaiseLevelCompleted(new LevelCompletedEventArgs("Level", 5, 5, totalScore, 30, capReached, 0));
    }

    private sealed class FakeSaveService : ISaveService
    {
        public PlayerData PlayerData { get; } = new PlayerData();
        public CloudSyncStatus CloudSyncStatus => CloudSyncStatus.Disabled;
        public int SaveCalls { get; private set; }

        public event Action CloudSyncStatusChanged { add { } remove { } }
        public event Action PlayerDataReplaced;

        public void RaisePlayerDataReplaced() => PlayerDataReplaced?.Invoke();

        public void Load() { }
        public void Save() => SaveCalls++;
        public void ResetToDefaults() { }
        public Task InitializeCloudAsync() => Task.CompletedTask;
    }
}
