using System;

public interface IEconomyService
{
    int Balance { get; }

    bool CanAfford(int amount);
    bool TrySpendCoins(int amount, string reason, out int newBalance);
    void AddCoins(int amount, string source);
    void SetBalance(int balance);

    event Action<int> OnBalanceChanged;
    event Action<CoinsSpentEventArgs> OnCoinsSpent;
}

public class CoinsSpentEventArgs : EventArgs
{
    public int Amount { get; }
    public string Reason { get; }

    public CoinsSpentEventArgs(int amount, string reason)
    {
        Amount = amount;
        Reason = reason;
    }
}
