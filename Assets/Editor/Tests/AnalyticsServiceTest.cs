using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Edit Mode coverage for analytics delivery: nothing is stored without a backend, and queued events
/// reach the transport once it is ready.
/// </summary>
public class AnalyticsServiceTest
{
    private string cachePath;

    [SetUp]
    public void SetUp()
    {
        cachePath = Path.Combine(Application.persistentDataPath, "analytics_cache.json");
        File.Delete(cachePath);
    }

    [TearDown]
    public void TearDown()
    {
        File.Delete(cachePath);
    }

    [Test]
    public void DisabledTransport_StoresAndSendsNothing()
    {
        var transport = new FakeTransport { IsEnabled = false };
        var service = CreateService(transport);

        service.LogEvent(AnalyticsEvents.LevelStarted);

        Assert.AreEqual(0, transport.Sent.Count);
        Assert.IsFalse(File.Exists(cachePath));
        service.Dispose();
    }

    [Test]
    public void EventLoggedBeforeReady_IsSentOnceTransportIsReady()
    {
        var transport = new FakeTransport();
        var service = CreateService(transport);

        service.LogEvent(AnalyticsEvents.LevelStarted);
        Assert.AreEqual(0, transport.Sent.Count);

        transport.CompleteInitialization(true);

        CollectionAssert.AreEqual(new[] { AnalyticsEvents.LevelStarted }, transport.Sent);
        service.Dispose();
    }

    [Test]
    public void EventLoggedAfterReady_IsSentImmediately()
    {
        var transport = new FakeTransport();
        var service = CreateService(transport);
        transport.CompleteInitialization(true);

        service.LogEvent(AnalyticsEvents.LevelFailed);

        CollectionAssert.AreEqual(new[] { AnalyticsEvents.LevelFailed }, transport.Sent);
        service.Dispose();
    }

    private static AnalyticsService CreateService(IAnalyticsTransport transport)
    {
        var service = new AnalyticsService();
        service.Construct(null, new FakeEconomyService(), transport);
        return service;
    }

    private sealed class FakeTransport : IAnalyticsTransport
    {
        private readonly UniTaskCompletionSource<bool> initialization = new();

        public List<string> Sent { get; } = new();
        public bool IsEnabled { get; set; } = true;

        public UniTask<bool> InitializeAsync() => initialization.Task;

        public void CompleteInitialization(bool ready) => initialization.TrySetResult(ready);

        public void Send(string eventName, IReadOnlyDictionary<string, object> parameters)
        {
            if (eventName != AnalyticsEvents.SessionEnded)
                Sent.Add(eventName);
        }
    }

    private sealed class FakeEconomyService : IEconomyService
    {
        public int Balance => 0;
        public event Action<int> OnBalanceChanged { add { } remove { } }
        public event Action<CoinsSpentEventArgs> OnCoinsSpent { add { } remove { } }

        public bool CanAfford(int amount) => false;

        public bool TrySpendCoins(int amount, string reason, out int newBalance)
        {
            newBalance = 0;
            return false;
        }

        public void AddCoins(int amount, string source) { }
        public void SetBalance(int balance) { }
    }
}
