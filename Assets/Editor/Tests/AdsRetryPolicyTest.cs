using NUnit.Framework;

/// <summary>Edit Mode coverage for ad load error classification and retry timing.</summary>
public class AdsRetryPolicyTest
{
    [TestCase("Error 509: No fill", AdsRetryPolicy.FailureKind.NoFill)]
    [TestCase("no fill for this request", AdsRetryPolicy.FailureKind.NoFill)]
    [TestCase("Error 626: Invalid ad unit id", AdsRetryPolicy.FailureKind.InvalidAdUnit)]
    [TestCase("invalid ad unit id", AdsRetryPolicy.FailureKind.InvalidAdUnit)]
    [TestCase("network timeout", AdsRetryPolicy.FailureKind.Other)]
    [TestCase("", AdsRetryPolicy.FailureKind.Other)]
    [TestCase(null, AdsRetryPolicy.FailureKind.Other)]
    public void Classify_MapsErrorText(string error, AdsRetryPolicy.FailureKind expected)
    {
        Assert.AreEqual(expected, AdsRetryPolicy.Classify(error));
    }

    [Test]
    public void NoFill_RetriesWithFixedDelayThenStops()
    {
        var policy = new AdsRetryPolicy();

        for (int i = 0; i < 3; i++)
        {
            Assert.IsTrue(policy.TryGetRetryDelay(AdsRetryPolicy.FailureKind.NoFill, out float delay));
            Assert.AreEqual(15f, delay);
        }

        Assert.IsFalse(policy.TryGetRetryDelay(AdsRetryPolicy.FailureKind.NoFill, out _));
    }

    [Test]
    public void OtherFailures_BackOffLinearlyThenStop()
    {
        var policy = new AdsRetryPolicy();

        Assert.IsTrue(policy.TryGetRetryDelay(AdsRetryPolicy.FailureKind.Other, out float first));
        Assert.IsTrue(policy.TryGetRetryDelay(AdsRetryPolicy.FailureKind.Other, out float second));
        Assert.IsTrue(policy.TryGetRetryDelay(AdsRetryPolicy.FailureKind.Other, out float third));
        Assert.AreEqual(3f, first);
        Assert.AreEqual(6f, second);
        Assert.AreEqual(9f, third);
        Assert.IsFalse(policy.TryGetRetryDelay(AdsRetryPolicy.FailureKind.Other, out _));
    }

    [Test]
    public void InvalidAdUnit_NeverRetries()
    {
        var policy = new AdsRetryPolicy();

        Assert.IsFalse(policy.TryGetRetryDelay(AdsRetryPolicy.FailureKind.InvalidAdUnit, out _));
    }

    [Test]
    public void Reset_RestoresRetryBudget()
    {
        var policy = new AdsRetryPolicy(maxNoFillRetries: 1, maxOtherRetries: 1);

        Assert.IsTrue(policy.TryGetRetryDelay(AdsRetryPolicy.FailureKind.NoFill, out _));
        Assert.IsFalse(policy.TryGetRetryDelay(AdsRetryPolicy.FailureKind.NoFill, out _));
        Assert.IsTrue(policy.TryGetRetryDelay(AdsRetryPolicy.FailureKind.Other, out _));
        Assert.IsFalse(policy.TryGetRetryDelay(AdsRetryPolicy.FailureKind.Other, out _));

        policy.Reset();

        Assert.IsTrue(policy.TryGetRetryDelay(AdsRetryPolicy.FailureKind.NoFill, out _));
        Assert.IsTrue(policy.TryGetRetryDelay(AdsRetryPolicy.FailureKind.Other, out _));
    }
}
