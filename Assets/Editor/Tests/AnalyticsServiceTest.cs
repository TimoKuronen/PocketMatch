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
    public void EventLoggedAfterReady_IsSentImmediately_AndNotStored()
    {
        var transport = new FakeTransport();
        var service = CreateService(transport);
        transport.CompleteInitialization(true);

        service.LogEvent(AnalyticsEvents.LevelFailed);

        CollectionAssert.AreEqual(new[] { AnalyticsEvents.LevelFailed }, transport.Sent);
        Assert.IsFalse(File.Exists(cachePath) && File.ReadAllText(cachePath).Contains(AnalyticsEvents.LevelFailed));
        service.Dispose();
    }

    [Test]
    public void QueueBeforeReady_KeepsOnlyTheNewestEvents()
    {
        var transport = new FakeTransport();
        var service = CreateService(transport);

        for (int i = 0; i < 250; i++)
            service.LogEvent(AnalyticsEvents.LevelStarted, new Dictionary<string, object> { { "level_index", i } });

        transport.CompleteInitialization(true);

        Assert.AreEqual(200, transport.Sent.Count);
        Assert.AreEqual(249, transport.LastParameters["level_index"]);
        service.Dispose();
    }

    [Test]
    public void DisablingTransport_DiscardsQueuedEventsAndStopsStoring()
    {
        var transport = new FakeTransport();
        var service = CreateService(transport);
        service.LogEvent(AnalyticsEvents.LevelStarted);
        Assert.IsTrue(File.Exists(cachePath));

        transport.SetEnabled(false);
        service.LogEvent(AnalyticsEvents.LevelFailed);

        Assert.IsFalse(File.Exists(cachePath));
        transport.CompleteInitialization(true);
        Assert.AreEqual(0, transport.Sent.Count);
        service.Dispose();
    }

    [Test]
    public void ReEnablingTransport_InitializesAgainAndSendsNewEvents()
    {
        var transport = new FakeTransport();
        var service = CreateService(transport);
        transport.SetEnabled(false);

        transport.SetEnabled(true);
        transport.CompleteInitialization(true);
        service.LogEvent(AnalyticsEvents.LevelCompleted);

        CollectionAssert.AreEqual(new[] { AnalyticsEvents.LevelCompleted }, transport.Sent);
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
        private UniTaskCompletionSource<bool> initialization = new();

        public List<string> Sent { get; } = new();
        public IReadOnlyDictionary<string, object> LastParameters { get; private set; }
        public bool IsEnabled { get; set; } = true;

        public event Action EnabledChanged;

        public UniTask<bool> InitializeAsync()
        {
            initialization = new UniTaskCompletionSource<bool>();
            return initialization.Task;
        }

        public void CompleteInitialization(bool ready) => initialization.TrySetResult(ready);

        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;
            EnabledChanged?.Invoke();
        }

        public void Send(string eventName, IReadOnlyDictionary<string, object> parameters)
        {
            Sent.Add(eventName);
            LastParameters = parameters;
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
