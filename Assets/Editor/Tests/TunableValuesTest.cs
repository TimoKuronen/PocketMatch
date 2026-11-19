using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// Edit Mode coverage for remote tunables parsing: defaults when nothing is fetched, per-key fallback for
/// missing, wrongly typed, or out-of-range values, and carrying the previous values forward.
/// </summary>
public class TunableValuesTest
{
    private static TunableValues Read(Dictionary<string, object> payload, TunableValues fallback = null)
    {
        return TunableValues.Read(key => payload.TryGetValue(key, out var value) ? value : null, fallback ?? new TunableValues());
    }

    [Test]
    public void EmptyPayload_KeepsDefaults()
    {
        var values = Read(new Dictionary<string, object>());

        Assert.AreEqual(300, values.ContinueCoinCost);
        Assert.AreEqual(3, values.ContinueExtraMoves);
        Assert.IsTrue(values.InterstitialsEnabled);
        Assert.AreEqual(25, values.Level1MoveLimit);
    }

    [Test]
    public void FullPayload_AppliesEveryKey()
    {
        var values = Read(new Dictionary<string, object>
        {
            [TunableValues.ContinueCoinCostKey] = 150L,
            [TunableValues.ContinueExtraMovesKey] = 5L,
            [TunableValues.InterstitialsEnabledKey] = false,
            [TunableValues.Level1MoveLimitKey] = 30L
        });

        Assert.AreEqual(150, values.ContinueCoinCost);
        Assert.AreEqual(5, values.ContinueExtraMoves);
        Assert.IsFalse(values.InterstitialsEnabled);
        Assert.AreEqual(30, values.Level1MoveLimit);
    }

    [Test]
    public void PartialPayload_AppliesPresentKeysAndKeepsTheRest()
    {
        var values = Read(new Dictionary<string, object> { [TunableValues.ContinueCoinCostKey] = 100L });

        Assert.AreEqual(100, values.ContinueCoinCost);
        Assert.AreEqual(3, values.ContinueExtraMoves);
        Assert.AreEqual(25, values.Level1MoveLimit);
    }

    [Test]
    public void TypeMismatch_KeepsThatKeyOnly()
    {
        var values = Read(new Dictionary<string, object>
        {
            [TunableValues.ContinueCoinCostKey] = "free",
            [TunableValues.InterstitialsEnabledKey] = 0L,
            [TunableValues.Level1MoveLimitKey] = 18L
        });

        Assert.AreEqual(300, values.ContinueCoinCost);
        Assert.IsTrue(values.InterstitialsEnabled);
        Assert.AreEqual(18, values.Level1MoveLimit);
    }

    [TestCase(-1L)]
    [TestCase(100001L)]
    public void CoinCostOutOfRange_KeepsFallback(long cost)
    {
        var values = Read(new Dictionary<string, object> { [TunableValues.ContinueCoinCostKey] = cost });

        Assert.AreEqual(300, values.ContinueCoinCost);
    }

    [TestCase(0L)]
    [TestCase(51L)]
    public void ExtraMovesOutOfRange_KeepsFallback(long moves)
    {
        var values = Read(new Dictionary<string, object> { [TunableValues.ContinueExtraMovesKey] = moves });

        Assert.AreEqual(3, values.ContinueExtraMoves);
    }

    [Test]
    public void LevelOneMovesBeyondIntRange_KeepsFallback()
    {
        var values = Read(new Dictionary<string, object> { [TunableValues.Level1MoveLimitKey] = long.MaxValue });

        Assert.AreEqual(25, values.Level1MoveLimit);
    }

    [Test]
    public void MissingKey_KeepsPreviouslyFetchedValue()
    {
        var previous = new TunableValues { continueCoinCost = 120, interstitialsEnabled = false };

        var values = Read(new Dictionary<string, object> { [TunableValues.Level1MoveLimitKey] = 22L }, previous);

        Assert.AreEqual(120, values.ContinueCoinCost);
        Assert.IsFalse(values.InterstitialsEnabled);
        Assert.AreEqual(22, values.Level1MoveLimit);
    }
}
