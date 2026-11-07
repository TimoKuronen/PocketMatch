using System.Collections.Generic;
using Cysharp.Threading.Tasks;

/// <summary>
/// Delivers analytics events to a backend. <see cref="AnalyticsService"/> owns queuing and retries.
/// </summary>
public interface IAnalyticsTransport
{
    /// <summary>False when no backend is configured; events are then neither queued nor stored.</summary>
    bool IsEnabled { get; }

    /// <summary>Returns true once the backend can accept events.</summary>
    UniTask<bool> InitializeAsync();

    void Send(string eventName, IReadOnlyDictionary<string, object> parameters);
}
