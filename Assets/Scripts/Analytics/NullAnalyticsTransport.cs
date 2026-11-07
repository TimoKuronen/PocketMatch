using System.Collections.Generic;
using Cysharp.Threading.Tasks;

/// <summary>
/// Used when no analytics backend is configured, so no event data leaves the device or is stored on it.
/// </summary>
public sealed class NullAnalyticsTransport : IAnalyticsTransport
{
    public bool IsEnabled => false;

    public UniTask<bool> InitializeAsync() => UniTask.FromResult(false);

    public void Send(string eventName, IReadOnlyDictionary<string, object> parameters) { }
}
