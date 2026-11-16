using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Ugs = Unity.Services.Analytics;

/// <summary>
/// Sends events through Unity Analytics. Nothing is recorded until the player has granted consent.
/// The <see cref="AnalyticsEvents.AdImpression"/> event maps onto the SDK standard ad impression event;
/// every other event is sent as a custom event.
/// </summary>
public sealed class UgsAnalyticsTransport : IAnalyticsTransport, IDisposable
{
    private readonly IUgsInitializer ugs;
    private readonly IAnalyticsConsent consent;
    private bool enabled;
    private bool collecting;

    public UgsAnalyticsTransport(IUgsInitializer ugs, IAnalyticsConsent consent)
    {
        this.ugs = ugs;
        this.consent = consent;
        enabled = IsEnabled;
        consent.StateChanged += OnConsentChanged;
    }

    public bool IsEnabled => consent.State != AnalyticsConsentState.Declined;

    public event Action EnabledChanged;

    public async UniTask<bool> InitializeAsync()
    {
        if (!await ugs.InitializeAsync())
            return false;

        await UniTask.WaitUntil(() => consent.State != AnalyticsConsentState.Unknown);
        if (consent.State != AnalyticsConsentState.Granted)
            return false;

        Ugs.AnalyticsService.Instance.StartDataCollection();
        collecting = true;
        return true;
    }

    public void Send(string eventName, IReadOnlyDictionary<string, object> parameters)
    {
        Ugs.Event analyticsEvent;
        try
        {
            analyticsEvent = eventName == AnalyticsEvents.AdImpression
                ? BuildAdImpression(parameters)
                : BuildCustomEvent(eventName, parameters);
        }
        catch (ArgumentException e)
        {
            Debug.LogWarning($"[Analytics] Dropped {eventName}: {e.Message}");
            return;
        }

        Ugs.AnalyticsService.Instance.RecordEvent(analyticsEvent);
    }

    public void Dispose()
    {
        consent.StateChanged -= OnConsentChanged;
    }

    private static Ugs.CustomEvent BuildCustomEvent(string eventName, IReadOnlyDictionary<string, object> parameters)
    {
        var customEvent = new Ugs.CustomEvent(eventName);
        if (parameters == null)
            return customEvent;

        foreach (var parameter in parameters)
        {
            if (parameter.Value != null)
                customEvent.Add(parameter.Key, Normalize(parameter.Value));
        }

        return customEvent;
    }

    private static Ugs.AdImpressionEvent BuildAdImpression(IReadOnlyDictionary<string, object> parameters)
    {
        string format = Read(parameters, "ad_format");
        string placement = Read(parameters, "placement");
        string result = Read(parameters, "result");

        return new Ugs.AdImpressionEvent
        {
            AdCompletionStatus = result.Contains("dismissed")
                ? Ugs.AdCompletionStatus.Partial
                : Ugs.AdCompletionStatus.Completed,
            AdProvider = Ugs.AdProvider.IrnSource,
            PlacementId = placement,
            PlacementName = placement,
            PlacementType = format == "rewarded"
                ? Ugs.AdPlacementType.REWARDED
                : Ugs.AdPlacementType.INTERSTITIAL
        };
    }

    private static string Read(IReadOnlyDictionary<string, object> parameters, string key)
    {
        return parameters != null && parameters.TryGetValue(key, out var value) && value != null
            ? value.ToString()
            : string.Empty;
    }

    // Queued events come back from the JSON cache with integers widened to long.
    private static object Normalize(object value)
    {
        if (value is long number && number >= int.MinValue && number <= int.MaxValue)
            return (int)number;

        return value;
    }

    private void OnConsentChanged()
    {
        if (consent.State == AnalyticsConsentState.Declined && collecting)
        {
            Ugs.AnalyticsService.Instance.StopDataCollection();
            collecting = false;
        }

        bool now = IsEnabled;
        if (now == enabled)
            return;

        enabled = now;
        EnabledChanged?.Invoke();
    }
}
