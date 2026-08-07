using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class EconomyService : IEconomyService, IStartable, IDisposable
{
    private readonly ISaveService saveService;

    public int Balance => saveService.PlayerData.coins;

    public event Action<int> OnBalanceChanged;
    public event Action<CoinsSpentEventArgs> OnCoinsSpent;

    [Inject]
    public EconomyService(ISaveService saveService)
    {
        this.saveService = saveService;
    }

    public void Start()
    {
        LevelEvents.OnLevelCompleted += OnLevelCompleted;
    }

    private void OnLevelCompleted(object sender, LevelCompletedEventArgs e)
    {
        if (e.IsLevelCapReached || e.TotalScore <= 0)
            return;

        AddCoins(e.TotalScore, "level_complete");
    }

    public bool CanAfford(int amount) => amount >= 0 && Balance >= amount;

    public bool TrySpendCoins(int amount, string reason, out int newBalance)
    {
        newBalance = Balance;

        if (amount <= 0)
        {
            Debug.LogWarning("[EconomyService] Spend amount must be positive.");
            return false;
        }

        if (!CanAfford(amount))
            return false;

        saveService.PlayerData.coins -= amount;
        newBalance = saveService.PlayerData.coins;
        saveService.Save();

        OnCoinsSpent?.Invoke(new CoinsSpentEventArgs(amount, reason));
        OnBalanceChanged?.Invoke(newBalance);
        return true;
    }

    public void AddCoins(int amount, string source)
    {
        if (amount <= 0)
            return;

        saveService.PlayerData.coins += amount;
        saveService.Save();
        OnBalanceChanged?.Invoke(saveService.PlayerData.coins);
    }

    public void Dispose()
    {
        LevelEvents.OnLevelCompleted -= OnLevelCompleted;
    }
}
