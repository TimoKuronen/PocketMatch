using System;
using UnityEngine;

/// <summary>Stores the consent answer in PlayerPrefs so it stays local to the device.</summary>
public sealed class AnalyticsConsent : IAnalyticsConsent
{
    public const string PrefKey = "analytics_consent";

    public AnalyticsConsentState State { get; private set; }

    public event Action StateChanged;

    public AnalyticsConsent()
    {
        int stored = PlayerPrefs.GetInt(PrefKey, (int)AnalyticsConsentState.Unknown);
        State = Enum.IsDefined(typeof(AnalyticsConsentState), stored)
            ? (AnalyticsConsentState)stored
            : AnalyticsConsentState.Unknown;
    }

    public void SetGranted(bool granted)
    {
        var next = granted ? AnalyticsConsentState.Granted : AnalyticsConsentState.Declined;
        if (next == State)
            return;

        State = next;
        PlayerPrefs.SetInt(PrefKey, (int)next);
        PlayerPrefs.Save();
        StateChanged?.Invoke();
    }
}
